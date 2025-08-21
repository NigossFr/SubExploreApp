using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Connection states for auto-reconnect service
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>Connection is established and working</summary>
        Connected,
        
        /// <summary>Connection is lost and attempting reconnection</summary>
        Reconnecting,
        
        /// <summary>Connection is permanently disconnected</summary>
        Disconnected,
        
        /// <summary>Connection state is unknown</summary>
        Unknown
    }

    /// <summary>
    /// Connection state change event arguments
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState PreviousState { get; }
        public ConnectionState CurrentState { get; }
        public string? Reason { get; }
        public DateTime Timestamp { get; }

        public ConnectionStateChangedEventArgs(
            ConnectionState previousState,
            ConnectionState currentState,
            string? reason = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Auto-reconnect service for managing connection recovery
    /// </summary>
    public interface IAutoReconnectService
    {
        /// <summary>
        /// Current connection state
        /// </summary>
        ConnectionState CurrentState { get; }

        /// <summary>
        /// Number of reconnection attempts made
        /// </summary>
        int ReconnectAttempts { get; }

        /// <summary>
        /// Time of the last successful connection
        /// </summary>
        DateTime? LastConnectedTime { get; }

        /// <summary>
        /// Whether auto-reconnect is currently enabled
        /// </summary>
        bool IsAutoReconnectEnabled { get; }

        /// <summary>
        /// Event fired when connection state changes
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Starts automatic reconnection monitoring
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops automatic reconnection monitoring
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Manually triggers a reconnection attempt
        /// </summary>
        /// <returns>True if reconnection was successful</returns>
        Task<bool> ReconnectAsync();

        /// <summary>
        /// Reports a connection loss to trigger reconnection
        /// </summary>
        /// <param name="reason">Reason for the connection loss</param>
        Task ReportConnectionLossAsync(string? reason = null);

        /// <summary>
        /// Reports a successful connection to update state
        /// </summary>
        Task ReportConnectionSuccessAsync();

        /// <summary>
        /// Registers a connection test function
        /// </summary>
        /// <param name="name">Name of the connection test</param>
        /// <param name="testFunction">Function to test connection health</param>
        void RegisterConnectionTest(string name, Func<CancellationToken, Task<bool>> testFunction);

        /// <summary>
        /// Registers a reconnection function
        /// </summary>
        /// <param name="name">Name of the reconnection function</param>
        /// <param name="reconnectFunction">Function to perform reconnection</param>
        void RegisterReconnectFunction(string name, Func<CancellationToken, Task<bool>> reconnectFunction);

        /// <summary>
        /// Unregisters a connection test
        /// </summary>
        /// <param name="name">Name of the test to remove</param>
        void UnregisterConnectionTest(string name);

        /// <summary>
        /// Unregisters a reconnection function
        /// </summary>
        /// <param name="name">Name of the function to remove</param>
        void UnregisterReconnectFunction(string name);

        /// <summary>
        /// Gets the current reconnection status
        /// </summary>
        /// <returns>Status information</returns>
        ReconnectStatus GetStatus();
    }

    /// <summary>
    /// Reconnection status information
    /// </summary>
    public class ReconnectStatus
    {
        /// <summary>Current connection state</summary>
        public ConnectionState State { get; }

        /// <summary>Number of reconnection attempts</summary>
        public int AttemptCount { get; }

        /// <summary>Time of last connection success</summary>
        public DateTime? LastConnected { get; }

        /// <summary>Time of last connection failure</summary>
        public DateTime? LastFailure { get; }

        /// <summary>Reason for last failure</summary>
        public string? LastFailureReason { get; }

        /// <summary>Whether auto-reconnect is active</summary>
        public bool IsActive { get; }

        /// <summary>Number of registered connection tests</summary>
        public int RegisteredTests { get; }

        /// <summary>Number of registered reconnection functions</summary>
        public int RegisteredReconnectFunctions { get; }

        public ReconnectStatus(
            ConnectionState state,
            int attemptCount,
            DateTime? lastConnected,
            DateTime? lastFailure,
            string? lastFailureReason,
            bool isActive,
            int registeredTests,
            int registeredReconnectFunctions)
        {
            State = state;
            AttemptCount = attemptCount;
            LastConnected = lastConnected;
            LastFailure = lastFailure;
            LastFailureReason = lastFailureReason;
            IsActive = isActive;
            RegisteredTests = registeredTests;
            RegisteredReconnectFunctions = registeredReconnectFunctions;
        }

        /// <summary>
        /// Gets a summary string of the reconnection status
        /// </summary>
        public string GetSummary()
        {
            var stateIcon = State switch
            {
                ConnectionState.Connected => "✅",
                ConnectionState.Reconnecting => "🔄",
                ConnectionState.Disconnected => "❌",
                _ => "❓"
            };

            var activeStatus = IsActive ? "Active" : "Inactive";
            
            return $"{stateIcon} {State} | {AttemptCount} attempts | {activeStatus} | {RegisteredTests} tests | {RegisteredReconnectFunctions} reconnect functions";
        }
    }
}