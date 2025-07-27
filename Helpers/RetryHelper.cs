using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Helper class for implementing intelligent retry logic with exponential backoff
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Executes an async operation with intelligent retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
        /// <param name="baseDelay">Base delay between retries in milliseconds (default: 1000ms)</param>
        /// <param name="maxDelay">Maximum delay between retries in milliseconds (default: 10000ms)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            int maxRetries = 3,
            int baseDelay = 1000,
            int maxDelay = 10000,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var attempt = 0;
            Exception? lastException = null;

            while (attempt <= maxRetries)
            {
                try
                {
                    var result = await operation(cancellationToken).ConfigureAwait(false);
                    
                    if (attempt > 0)
                    {
                        logger?.LogInformation("Operation succeeded on attempt {Attempt}", attempt + 1);
                    }
                    
                    return result;
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt, maxRetries))
                {
                    lastException = ex;
                    attempt++;
                    
                    var delay = CalculateDelay(attempt, baseDelay, maxDelay);
                    logger?.LogWarning(ex, "Operation failed on attempt {Attempt}. Retrying in {Delay}ms...", 
                        attempt, delay);
                    
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            // All retries exhausted
            logger?.LogError(lastException, "Operation failed after {MaxRetries} retries", maxRetries);
            throw lastException ?? new InvalidOperationException("Operation failed with unknown error");
        }

        /// <summary>
        /// Executes an async operation with intelligent retry logic (void return)
        /// </summary>
        public static async Task ExecuteWithRetryAsync(
            Func<CancellationToken, Task> operation,
            int maxRetries = 3,
            int baseDelay = 1000,
            int maxDelay = 10000,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync<object?>(async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return null;
            }, maxRetries, baseDelay, maxDelay, logger, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines if an exception should trigger a retry
        /// </summary>
        private static bool ShouldRetry(Exception ex, int attempt, int maxRetries)
        {
            if (attempt >= maxRetries)
                return false;

            // Check specific exception types for retry eligibility
            switch (ex)
            {
                // Don't retry for these exceptions (most specific first)
                case ArgumentNullException:
                case ArgumentException:
                case UnauthorizedAccessException:
                case InvalidOperationException:
                    return false;
                
                // Network-related exceptions that are worth retrying
                case HttpRequestException httpEx:
                    // Check for specific HTTP status codes worth retrying
                    if (httpEx.Message.Contains("500") || // Internal Server Error
                        httpEx.Message.Contains("502") || // Bad Gateway
                        httpEx.Message.Contains("503") || // Service Unavailable
                        httpEx.Message.Contains("504") || // Gateway Timeout
                        httpEx.Message.Contains("429"))   // Too Many Requests
                    {
                        return true;
                    }
                    return true; // Retry other HTTP exceptions
                
                case TaskCanceledException when ex.InnerException is TimeoutException:
                case OperationCanceledException when ex.InnerException is TimeoutException:
                    return true;
                
                // DNS resolution errors (common in emulators)
                case Exception when ex.Message.Contains("UnknownHostException") ||
                                   ex.Message.Contains("Name or service not known") ||
                                   ex.Message.Contains("Unable to resolve host"):
                    return true;
                
                // Retry for other exceptions (with caution)
                default:
                    return true;
            }
        }

        /// <summary>
        /// Calculates delay with exponential backoff and jitter
        /// </summary>
        private static int CalculateDelay(int attempt, int baseDelay, int maxDelay)
        {
            // Exponential backoff: baseDelay * 2^(attempt-1)
            var exponentialDelay = baseDelay * Math.Pow(2, attempt - 1);
            
            // Add jitter (±20% randomization)
            var random = new Random();
            var jitter = exponentialDelay * 0.2 * (random.NextDouble() - 0.5);
            
            var totalDelay = exponentialDelay + jitter;
            
            // Cap at maxDelay
            return (int)Math.Min(totalDelay, maxDelay);
        }

        /// <summary>
        /// Specific retry helper for weather API calls
        /// </summary>
        public static async Task<T?> ExecuteWeatherApiCallAsync<T>(
            Func<CancellationToken, Task<T?>> operation,
            ILogger? logger = null,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                return await ExecuteWithRetryAsync(
                    operation,
                    maxRetries: 2, // Limited retries for weather (not critical)
                    baseDelay: 500, // Shorter delay for weather
                    maxDelay: 2000, // Max 2 seconds delay
                    logger,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Weather API call failed after all retries. Continuing without weather data.");
                return null; // Return null for weather failures (non-critical)
            }
        }

        /// <summary>
        /// Specific retry helper for database operations
        /// </summary>
        public static async Task<T> ExecuteDatabaseOperationAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(
                operation,
                maxRetries: 5, // More retries for database (critical)
                baseDelay: 200, // Shorter initial delay for database
                maxDelay: 5000, // Max 5 seconds delay
                logger,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}