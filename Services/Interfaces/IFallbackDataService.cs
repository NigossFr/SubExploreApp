using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Data access methods available for fallback
    /// </summary>
    public enum DataAccessMethod
    {
        /// <summary>Use Supabase API for data access</summary>
        SupabaseApi,
        
        /// <summary>Use direct database connection</summary>
        DirectDatabase,
        
        /// <summary>Method is currently unknown or not determined</summary>
        Unknown
    }

    /// <summary>
    /// Fallback decision result
    /// </summary>
    public class FallbackDecision
    {
        /// <summary>Chosen data access method</summary>
        public DataAccessMethod Method { get; }
        
        /// <summary>Reason for choosing this method</summary>
        public string Reason { get; }
        
        /// <summary>Whether this is a fallback from the preferred method</summary>
        public bool IsFallback { get; }
        
        /// <summary>Timestamp of the decision</summary>
        public DateTime Timestamp { get; }

        public FallbackDecision(DataAccessMethod method, string reason, bool isFallback = false)
        {
            Method = method;
            Reason = reason;
            IsFallback = isFallback;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Service providing intelligent fallback between API and direct database access
    /// </summary>
    public interface IFallbackDataService
    {
        /// <summary>
        /// Current preferred data access method
        /// </summary>
        DataAccessMethod PreferredMethod { get; }

        /// <summary>
        /// Current active data access method
        /// </summary>
        DataAccessMethod ActiveMethod { get; }

        /// <summary>
        /// Whether fallback mode is currently active
        /// </summary>
        bool IsFallbackActive { get; }

        /// <summary>
        /// Event fired when data access method changes
        /// </summary>
        event EventHandler<DataAccessMethodChangedEventArgs> DataAccessMethodChanged;

        /// <summary>
        /// Determines the best data access method for current conditions
        /// </summary>
        /// <returns>Fallback decision with chosen method and reasoning</returns>
        Task<FallbackDecision> DetermineDataAccessMethodAsync();

        /// <summary>
        /// Executes an operation with automatic fallback between API and database
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="apiOperation">Operation using Supabase API</param>
        /// <param name="databaseOperation">Operation using direct database access</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <returns>Result from successful operation</returns>
        Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> apiOperation,
            Func<Task<T>> databaseOperation,
            string operationName);

        /// <summary>
        /// Executes an operation with automatic fallback (void return)
        /// </summary>
        /// <param name="apiOperation">Operation using Supabase API</param>
        /// <param name="databaseOperation">Operation using direct database access</param>
        /// <param name="operationName">Name of the operation for logging</param>
        Task ExecuteWithFallbackAsync(
            Func<Task> apiOperation,
            Func<Task> databaseOperation,
            string operationName);

        /// <summary>
        /// Forces the service to use a specific data access method
        /// </summary>
        /// <param name="method">Method to force</param>
        /// <param name="reason">Reason for forcing this method</param>
        void ForceDataAccessMethod(DataAccessMethod method, string reason);

        /// <summary>
        /// Resets to automatic method selection
        /// </summary>
        void ResetToAutomatic();

        /// <summary>
        /// Reports a failure for the current method to trigger fallback evaluation
        /// </summary>
        /// <param name="method">Method that failed</param>
        /// <param name="exception">Exception that occurred</param>
        Task ReportMethodFailureAsync(DataAccessMethod method, Exception exception);

        /// <summary>
        /// Reports a success for a method to improve its reliability score
        /// </summary>
        /// <param name="method">Method that succeeded</param>
        Task ReportMethodSuccessAsync(DataAccessMethod method);

        /// <summary>
        /// Gets current fallback statistics and status
        /// </summary>
        /// <returns>Status information</returns>
        FallbackStatus GetStatus();
    }

    /// <summary>
    /// Event arguments for data access method changes
    /// </summary>
    public class DataAccessMethodChangedEventArgs : EventArgs
    {
        public DataAccessMethod PreviousMethod { get; }
        public DataAccessMethod CurrentMethod { get; }
        public string Reason { get; }
        public bool IsFallback { get; }
        public DateTime Timestamp { get; }

        public DataAccessMethodChangedEventArgs(
            DataAccessMethod previousMethod,
            DataAccessMethod currentMethod,
            string reason,
            bool isFallback = false)
        {
            PreviousMethod = previousMethod;
            CurrentMethod = currentMethod;
            Reason = reason;
            IsFallback = isFallback;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Fallback status information
    /// </summary>
    public class FallbackStatus
    {
        /// <summary>Current preferred method</summary>
        public DataAccessMethod PreferredMethod { get; }

        /// <summary>Current active method</summary>
        public DataAccessMethod ActiveMethod { get; }

        /// <summary>Whether fallback is active</summary>
        public bool IsFallbackActive { get; }

        /// <summary>API success rate (0-1)</summary>
        public double ApiSuccessRate { get; }

        /// <summary>Database success rate (0-1)</summary>
        public double DatabaseSuccessRate { get; }

        /// <summary>Number of API operations attempted</summary>
        public int ApiOperationsAttempted { get; }

        /// <summary>Number of database operations attempted</summary>
        public int DatabaseOperationsAttempted { get; }

        /// <summary>Number of successful fallbacks</summary>
        public int SuccessfulFallbacks { get; }

        /// <summary>Number of failed operations (both methods failed)</summary>
        public int TotalFailures { get; }

        /// <summary>Time when current method was last changed</summary>
        public DateTime? LastMethodChange { get; }

        /// <summary>Reason for current method selection</summary>
        public string? CurrentMethodReason { get; }

        public FallbackStatus(
            DataAccessMethod preferredMethod,
            DataAccessMethod activeMethod,
            bool isFallbackActive,
            double apiSuccessRate,
            double databaseSuccessRate,
            int apiOperationsAttempted,
            int databaseOperationsAttempted,
            int successfulFallbacks,
            int totalFailures,
            DateTime? lastMethodChange,
            string? currentMethodReason)
        {
            PreferredMethod = preferredMethod;
            ActiveMethod = activeMethod;
            IsFallbackActive = isFallbackActive;
            ApiSuccessRate = apiSuccessRate;
            DatabaseSuccessRate = databaseSuccessRate;
            ApiOperationsAttempted = apiOperationsAttempted;
            DatabaseOperationsAttempted = databaseOperationsAttempted;
            SuccessfulFallbacks = successfulFallbacks;
            TotalFailures = totalFailures;
            LastMethodChange = lastMethodChange;
            CurrentMethodReason = currentMethodReason;
        }

        /// <summary>
        /// Gets a summary string of the fallback status
        /// </summary>
        public string GetSummary()
        {
            var activeIcon = ActiveMethod switch
            {
                DataAccessMethod.SupabaseApi => "🌐",
                DataAccessMethod.DirectDatabase => "💾",
                _ => "❓"
            };

            var fallbackStatus = IsFallbackActive ? " (FALLBACK)" : "";
            
            return $"{activeIcon} Active: {ActiveMethod}{fallbackStatus} | API: {ApiSuccessRate:P1} ({ApiOperationsAttempted} ops) | DB: {DatabaseSuccessRate:P1} ({DatabaseOperationsAttempted} ops) | Fallbacks: {SuccessfulFallbacks} | Failures: {TotalFailures}";
        }
    }
}