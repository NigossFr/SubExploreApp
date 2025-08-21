using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// States of the circuit breaker
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>Circuit is closed - operations flow normally</summary>
        Closed,
        
        /// <summary>Circuit is open - operations fail fast</summary>
        Open,
        
        /// <summary>Circuit is half-open - testing if service recovered</summary>
        HalfOpen
    }

    /// <summary>
    /// Circuit breaker service for preventing cascading failures
    /// </summary>
    public interface ICircuitBreakerService
    {
        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Number of consecutive failures recorded
        /// </summary>
        int FailureCount { get; }

        /// <summary>
        /// Time when the circuit was opened (if currently open)
        /// </summary>
        DateTime? LastFailureTime { get; }

        /// <summary>
        /// Executes an operation through the circuit breaker
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation through the circuit breaker (void return)
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually resets the circuit breaker to closed state
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the current health status of the circuit breaker
        /// </summary>
        /// <returns>Health status information</returns>
        CircuitBreakerHealthStatus GetHealthStatus();
    }

    /// <summary>
    /// Health status information for circuit breaker
    /// </summary>
    public class CircuitBreakerHealthStatus
    {
        /// <summary>Current state of the circuit breaker</summary>
        public CircuitBreakerState State { get; set; }

        /// <summary>Number of consecutive failures</summary>
        public int FailureCount { get; set; }

        /// <summary>Total number of successful operations</summary>
        public int SuccessCount { get; set; }

        /// <summary>Time when the circuit was last opened</summary>
        public DateTime? LastFailureTime { get; set; }

        /// <summary>Time when the circuit was last reset or succeeded</summary>
        public DateTime? LastSuccessTime { get; set; }

        /// <summary>Time until the circuit can transition to half-open (if currently open)</summary>
        public TimeSpan? TimeUntilRetry { get; set; }

        /// <summary>Whether the circuit breaker is healthy</summary>
        public bool IsHealthy => State == CircuitBreakerState.Closed;

        /// <summary>Human-readable status description</summary>
        public string StatusDescription => State switch
        {
            CircuitBreakerState.Closed => $"✅ Healthy - {SuccessCount} successes, {FailureCount} failures",
            CircuitBreakerState.Open => $"❌ Circuit Open - {FailureCount} consecutive failures, retry in {TimeUntilRetry?.TotalSeconds:F0}s",
            CircuitBreakerState.HalfOpen => "🟡 Testing Recovery - Circuit Half-Open",
            _ => "❓ Unknown State"
        };
    }
}