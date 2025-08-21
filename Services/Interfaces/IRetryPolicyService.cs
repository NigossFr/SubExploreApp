using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for implementing retry policies with exponential backoff
    /// </summary>
    public interface IRetryPolicyService
    {
        /// <summary>
        /// Executes an operation with exponential backoff retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
        /// <param name="baseDelay">Base delay in milliseconds (default: 1000)</param>
        /// <param name="maxDelay">Maximum delay in milliseconds (default: 30000)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            int maxRetries = 3,
            int baseDelay = 1000,
            int maxDelay = 30000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with exponential backoff retry logic (void return)
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
        /// <param name="baseDelay">Base delay in milliseconds (default: 1000)</param>
        /// <param name="maxDelay">Maximum delay in milliseconds (default: 30000)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteWithRetryAsync(
            Func<CancellationToken, Task> operation,
            int maxRetries = 3,
            int baseDelay = 1000,
            int maxDelay = 30000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if an exception should be retried
        /// </summary>
        /// <param name="exception">Exception to evaluate</param>
        /// <returns>True if the operation should be retried</returns>
        bool ShouldRetry(Exception exception);

        /// <summary>
        /// Calculates the delay for a specific retry attempt
        /// </summary>
        /// <param name="attempt">Current attempt number (0-based)</param>
        /// <param name="baseDelay">Base delay in milliseconds</param>
        /// <param name="maxDelay">Maximum delay in milliseconds</param>
        /// <returns>Delay in milliseconds</returns>
        int CalculateDelay(int attempt, int baseDelay, int maxDelay);
    }
}