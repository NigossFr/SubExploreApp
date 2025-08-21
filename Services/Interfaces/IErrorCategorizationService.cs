using System;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Error categories for handling different types of failures
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>Temporary error that should be retried</summary>
        Transient,
        
        /// <summary>Permanent error that should not be retried</summary>
        Permanent,
        
        /// <summary>Network-related error</summary>
        Network,
        
        /// <summary>Authentication or authorization error</summary>
        Security,
        
        /// <summary>Configuration or setup error</summary>
        Configuration,
        
        /// <summary>Resource limit or capacity error</summary>
        Resource,
        
        /// <summary>Data validation or format error</summary>
        Validation,
        
        /// <summary>Unknown or unclassified error</summary>
        Unknown
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>Low severity - informational</summary>
        Low,
        
        /// <summary>Medium severity - warning</summary>
        Medium,
        
        /// <summary>High severity - error</summary>
        High,
        
        /// <summary>Critical severity - system failure</summary>
        Critical
    }

    /// <summary>
    /// Error classification result
    /// </summary>
    public class ErrorClassification
    {
        /// <summary>Error category</summary>
        public ErrorCategory Category { get; }
        
        /// <summary>Error severity</summary>
        public ErrorSeverity Severity { get; }
        
        /// <summary>Whether the error should be retried</summary>
        public bool ShouldRetry { get; }
        
        /// <summary>Recommended action for handling the error</summary>
        public string RecommendedAction { get; }
        
        /// <summary>Human-readable description of the error classification</summary>
        public string Description { get; }
        
        /// <summary>Maximum retry attempts recommended for this error type</summary>
        public int MaxRetryAttempts { get; }
        
        /// <summary>Recommended base delay between retries (milliseconds)</summary>
        public int BaseRetryDelay { get; }

        public ErrorClassification(
            ErrorCategory category,
            ErrorSeverity severity,
            bool shouldRetry,
            string recommendedAction,
            string description,
            int maxRetryAttempts = 3,
            int baseRetryDelay = 1000)
        {
            Category = category;
            Severity = severity;
            ShouldRetry = shouldRetry;
            RecommendedAction = recommendedAction;
            Description = description;
            MaxRetryAttempts = maxRetryAttempts;
            BaseRetryDelay = baseRetryDelay;
        }

        /// <summary>
        /// Gets a formatted status string
        /// </summary>
        public string GetStatusString()
        {
            var severityIcon = Severity switch
            {
                ErrorSeverity.Low => "ℹ️",
                ErrorSeverity.Medium => "⚠️",
                ErrorSeverity.High => "❌",
                ErrorSeverity.Critical => "🚨",
                _ => "❓"
            };

            var retryIcon = ShouldRetry ? "🔄" : "⏹️";

            return $"{severityIcon} {Category} | {Severity} | {retryIcon} {(ShouldRetry ? "Retryable" : "No Retry")}";
        }
    }

    /// <summary>
    /// Service for categorizing and classifying errors
    /// </summary>
    public interface IErrorCategorizationService
    {
        /// <summary>
        /// Classifies an exception into a category and severity
        /// </summary>
        /// <param name="exception">Exception to classify</param>
        /// <returns>Error classification with recommended handling</returns>
        ErrorClassification ClassifyError(Exception exception);

        /// <summary>
        /// Determines if an error should be retried based on its classification
        /// </summary>
        /// <param name="exception">Exception to evaluate</param>
        /// <returns>True if the error should be retried</returns>
        bool ShouldRetryError(Exception exception);

        /// <summary>
        /// Gets the recommended retry parameters for an error
        /// </summary>
        /// <param name="exception">Exception to evaluate</param>
        /// <returns>Retry parameters (maxAttempts, baseDelay)</returns>
        (int maxAttempts, int baseDelay) GetRetryParameters(Exception exception);

        /// <summary>
        /// Gets a user-friendly error message for an exception
        /// </summary>
        /// <param name="exception">Exception to process</param>
        /// <returns>User-friendly error message</returns>
        string GetUserFriendlyMessage(Exception exception);

        /// <summary>
        /// Gets detailed error information for debugging
        /// </summary>
        /// <param name="exception">Exception to analyze</param>
        /// <returns>Detailed error information</returns>
        string GetDetailedErrorInfo(Exception exception);
    }
}