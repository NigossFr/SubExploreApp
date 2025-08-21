using System;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service implementing exponential backoff retry logic for resilient operations
    /// </summary>
    public class RetryPolicyService : IRetryPolicyService
    {
        private readonly ILogger<RetryPolicyService> _logger;
        private readonly Random _random;

        public RetryPolicyService(ILogger<RetryPolicyService> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        /// <summary>
        /// Executes an operation with exponential backoff retry logic
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            int maxRetries = 3,
            int baseDelay = 1000,
            int maxDelay = 30000,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be >= 0");

            Exception lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("🔄 Retry attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    }

                    var result = await operation(cancellationToken);
                    
                    if (attempt > 0)
                    {
                        _logger.LogInformation("✅ Operation succeeded after {Attempt} retries", attempt);
                    }

                    return result;
                }
                catch (Exception ex) when (attempt < maxRetries && ShouldRetry(ex))
                {
                    lastException = ex;
                    var delay = CalculateDelay(attempt, baseDelay, maxDelay);
                    
                    _logger.LogWarning(ex, 
                        "⚠️ Operation failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms. Error: {Error}",
                        attempt + 1, maxRetries + 1, delay, ex.Message);

                    if (delay > 0)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Non-retryable exception or max retries reached
                    _logger.LogError(ex, 
                        "❌ Operation failed permanently on attempt {Attempt}/{MaxRetries}. Error: {Error}",
                        attempt + 1, maxRetries + 1, ex.Message);
                    throw;
                }
            }

            // Should never reach here, but just in case
            throw lastException ?? new InvalidOperationException("Operation failed after all retry attempts");
        }

        /// <summary>
        /// Executes an operation with exponential backoff retry logic (void return)
        /// </summary>
        public async Task ExecuteWithRetryAsync(
            Func<CancellationToken, Task> operation,
            int maxRetries = 3,
            int baseDelay = 1000,
            int maxDelay = 30000,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync(async (ct) =>
            {
                await operation(ct);
                return true; // Return dummy value for generic method
            }, maxRetries, baseDelay, maxDelay, cancellationToken);
        }

        /// <summary>
        /// Determines if an exception should be retried based on type and characteristics
        /// </summary>
        public bool ShouldRetry(Exception exception)
        {
            if (exception == null)
                return false;

            // Check for transient exceptions that should be retried
            switch (exception)
            {
                // Network-related exceptions
                case HttpRequestException httpEx:
                    return IsTransientHttpException(httpEx);

                case TaskCanceledException tcEx when tcEx.InnerException is TimeoutException:
                    // Timeout exceptions should be retried
                    return true;

                case TimeoutException:
                    return true;

                // Database-related exceptions
                case DbException dbEx:
                    return IsTransientDatabaseException(dbEx);

                // Supabase/Postgrest specific exceptions
                case Postgrest.Exceptions.PostgrestException pgEx:
                    return IsTransientPostgrestException(pgEx);

                // General network exceptions
                case System.Net.Sockets.SocketException:
                case WebException webEx when IsTransientWebException(webEx):
                    return true;

                // Don't retry these types
                case ArgumentNullException:
                case ArgumentException:
                case InvalidOperationException:
                case UnauthorizedAccessException:
                case NotSupportedException:
                    return false;

                default:
                    // For unknown exceptions, be conservative and don't retry
                    _logger.LogDebug("🤔 Unknown exception type {ExceptionType}, not retrying", 
                        exception.GetType().Name);
                    return false;
            }
        }

        /// <summary>
        /// Calculates exponential backoff delay with jitter
        /// </summary>
        public int CalculateDelay(int attempt, int baseDelay, int maxDelay)
        {
            if (attempt < 0)
                return 0;

            // Calculate exponential backoff: baseDelay * 2^attempt
            var exponentialDelay = baseDelay * Math.Pow(2, attempt);
            
            // Cap at maximum delay
            var cappedDelay = Math.Min(exponentialDelay, maxDelay);
            
            // Add jitter (±25% random variation)
            var jitter = _random.NextDouble() * 0.5 - 0.25; // -0.25 to +0.25
            var delayWithJitter = cappedDelay * (1 + jitter);
            
            return Math.Max(0, (int)delayWithJitter);
        }

        #region Private Helper Methods

        /// <summary>
        /// Determines if an HTTP exception is transient
        /// </summary>
        private static bool IsTransientHttpException(HttpRequestException httpEx)
        {
            var message = httpEx.Message?.ToLower() ?? string.Empty;
            
            // Check for transient HTTP conditions
            return message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("unreachable") ||
                   message.Contains("temporarily unavailable") ||
                   message.Contains("502") || // Bad Gateway
                   message.Contains("503") || // Service Unavailable
                   message.Contains("504");   // Gateway Timeout
        }

        /// <summary>
        /// Determines if a database exception is transient
        /// </summary>
        private static bool IsTransientDatabaseException(DbException dbEx)
        {
            var message = dbEx.Message?.ToLower() ?? string.Empty;
            
            // Common transient database error patterns
            return message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("temporary") ||
                   message.Contains("deadlock") ||
                   message.Contains("lock") ||
                   dbEx.ErrorCode == -2; // Timeout error code
        }

        /// <summary>
        /// Determines if a Postgrest exception is transient
        /// </summary>
        private static bool IsTransientPostgrestException(Postgrest.Exceptions.PostgrestException pgEx)
        {
            // Check for transient HTTP status codes
            return (int)pgEx.StatusCode == 408 ||  // RequestTimeout
                   (int)pgEx.StatusCode == 429 ||  // TooManyRequests
                   (int)pgEx.StatusCode == 500 ||  // InternalServerError
                   (int)pgEx.StatusCode == 502 ||  // BadGateway
                   (int)pgEx.StatusCode == 503 ||  // ServiceUnavailable
                   (int)pgEx.StatusCode == 504;    // GatewayTimeout
        }

        /// <summary>
        /// Determines if a web exception is transient
        /// </summary>
        private static bool IsTransientWebException(WebException webEx)
        {
            switch (webEx.Status)
            {
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.PipelineFailure:
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.ProxyNameResolutionFailure:
                case WebExceptionStatus.KeepAliveFailure:
                    return true;

                default:
                    return false;
            }
        }

        #endregion
    }
}