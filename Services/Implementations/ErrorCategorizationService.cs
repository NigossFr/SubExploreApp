using System;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service for categorizing and classifying errors for appropriate handling
    /// </summary>
    public class ErrorCategorizationService : IErrorCategorizationService
    {
        private readonly ILogger<ErrorCategorizationService> _logger;

        public ErrorCategorizationService(ILogger<ErrorCategorizationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Classifies an exception into a category and severity with handling recommendations
        /// </summary>
        public ErrorClassification ClassifyError(Exception exception)
        {
            if (exception == null)
            {
                return new ErrorClassification(
                    ErrorCategory.Unknown,
                    ErrorSeverity.Low,
                    false,
                    "No exception provided",
                    "Null exception",
                    0, 0);
            }

            _logger.LogDebug("🔍 Classifying error: {ExceptionType} - {Message}", 
                exception.GetType().Name, exception.Message);

            return exception switch
            {
                // Network and connectivity errors (transient)
                HttpRequestException httpEx => ClassifyHttpException(httpEx),
                SocketException socketEx => ClassifySocketException(socketEx),
                WebException webEx => ClassifyWebException(webEx),
                TaskCanceledException tcEx when tcEx.InnerException is TimeoutException => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with exponential backoff",
                    "HTTP request timeout",
                    5, 2000),

                // Database errors
                DbException dbEx => ClassifyDatabaseException(dbEx),

                // Supabase/Postgrest errors
                Postgrest.Exceptions.PostgrestException pgEx => ClassifyPostgrestException(pgEx),

                // Authentication/Authorization errors (permanent)
                UnauthorizedAccessException => new ErrorClassification(
                    ErrorCategory.Security,
                    ErrorSeverity.High,
                    false,
                    "Check authentication credentials",
                    "Unauthorized access - check credentials",
                    0, 0),

                // Configuration errors (permanent)
                InvalidOperationException ioEx when ioEx.Message.Contains("configuration") || 
                                                   ioEx.Message.Contains("not initialized") => new ErrorClassification(
                    ErrorCategory.Configuration,
                    ErrorSeverity.High,
                    false,
                    "Fix configuration and restart",
                    "Service configuration error",
                    0, 0),

                // Validation errors (permanent)
                ArgumentNullException => new ErrorClassification(
                    ErrorCategory.Validation,
                    ErrorSeverity.Medium,
                    false,
                    "Ensure required parameters are provided",
                    "Required parameter is null",
                    0, 0),

                ArgumentException => new ErrorClassification(
                    ErrorCategory.Validation,
                    ErrorSeverity.Medium,
                    false,
                    "Fix input validation",
                    "Invalid argument or parameter",
                    0, 0),

                // Circuit breaker errors
                CircuitBreakerOpenException cbEx => new ErrorClassification(
                    ErrorCategory.Resource,
                    ErrorSeverity.High,
                    false,
                    $"Wait {cbEx.RetryAfter.TotalSeconds:F0}s before retry",
                    $"Circuit breaker open - {cbEx.FailureCount} failures",
                    0, (int)cbEx.RetryAfter.TotalMilliseconds),

                // General timeout errors (transient)
                TimeoutException => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with longer timeout",
                    "Operation timeout",
                    3, 2000),

                // Resource errors (potentially transient)
                OutOfMemoryException => new ErrorClassification(
                    ErrorCategory.Resource,
                    ErrorSeverity.Critical,
                    false,
                    "Reduce memory usage and restart",
                    "Out of memory",
                    0, 0),

                // Generic system errors
                SystemException => new ErrorClassification(
                    ErrorCategory.Unknown,
                    ErrorSeverity.High,
                    false,
                    "Check system logs and restart if necessary",
                    "System-level error",
                    1, 5000),

                // Unknown errors (conservative approach)
                _ => new ErrorClassification(
                    ErrorCategory.Unknown,
                    ErrorSeverity.Medium,
                    false,
                    "Log error details and investigate",
                    $"Unknown error type: {exception.GetType().Name}",
                    1, 3000)
            };
        }

        /// <summary>
        /// Determines if an error should be retried based on its classification
        /// </summary>
        public bool ShouldRetryError(Exception exception)
        {
            var classification = ClassifyError(exception);
            return classification.ShouldRetry;
        }

        /// <summary>
        /// Gets the recommended retry parameters for an error
        /// </summary>
        public (int maxAttempts, int baseDelay) GetRetryParameters(Exception exception)
        {
            var classification = ClassifyError(exception);
            return (classification.MaxRetryAttempts, classification.BaseRetryDelay);
        }

        /// <summary>
        /// Gets a user-friendly error message for an exception
        /// </summary>
        public string GetUserFriendlyMessage(Exception exception)
        {
            var classification = ClassifyError(exception);
            
            return classification.Category switch
            {
                ErrorCategory.Network => "Connection problem. Please check your internet connection and try again.",
                ErrorCategory.Security => "Authentication failed. Please check your credentials.",
                ErrorCategory.Configuration => "Service configuration error. Please contact support.",
                ErrorCategory.Validation => "Invalid input. Please check your data and try again.",
                ErrorCategory.Resource => "Service temporarily unavailable. Please try again later.",
                ErrorCategory.Transient => "Temporary issue. Please try again in a moment.",
                _ => "An unexpected error occurred. Please try again or contact support."
            };
        }

        /// <summary>
        /// Gets detailed error information for debugging
        /// </summary>
        public string GetDetailedErrorInfo(Exception exception)
        {
            var classification = ClassifyError(exception);
            
            return $"""
                Error Classification:
                  Category: {classification.Category}
                  Severity: {classification.Severity}
                  Should Retry: {classification.ShouldRetry}
                  Max Retry Attempts: {classification.MaxRetryAttempts}
                  Base Retry Delay: {classification.BaseRetryDelay}ms
                  Recommended Action: {classification.RecommendedAction}
                  Description: {classification.Description}
                
                Exception Details:
                  Type: {exception.GetType().FullName}
                  Message: {exception.Message}
                  Stack Trace: {exception.StackTrace}
                """;
        }

        #region Private Classification Methods

        /// <summary>
        /// Classifies HTTP exceptions
        /// </summary>
        private ErrorClassification ClassifyHttpException(HttpRequestException httpEx)
        {
            var message = httpEx.Message?.ToLower() ?? string.Empty;

            if (message.Contains("timeout"))
            {
                return new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with exponential backoff",
                    "HTTP request timeout",
                    5, 1000);
            }

            if (message.Contains("connection") || message.Contains("network"))
            {
                return new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.High,
                    true,
                    "Check network connectivity and retry",
                    "Network connection error",
                    3, 2000);
            }

            if (message.Contains("502") || message.Contains("503") || message.Contains("504"))
            {
                return new ErrorClassification(
                    ErrorCategory.Transient,
                    ErrorSeverity.Medium,
                    true,
                    "Retry after delay - server temporarily unavailable",
                    "Server temporarily unavailable",
                    5, 3000);
            }

            if (message.Contains("401") || message.Contains("403"))
            {
                return new ErrorClassification(
                    ErrorCategory.Security,
                    ErrorSeverity.High,
                    false,
                    "Check authentication credentials",
                    "Authentication or authorization failed",
                    0, 0);
            }

            return new ErrorClassification(
                ErrorCategory.Network,
                ErrorSeverity.Medium,
                true,
                "Retry with exponential backoff",
                "HTTP request error",
                3, 1000);
        }

        /// <summary>
        /// Classifies socket exceptions
        /// </summary>
        private ErrorClassification ClassifySocketException(SocketException socketEx)
        {
            return socketEx.SocketErrorCode switch
            {
                SocketError.TimedOut => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with longer timeout",
                    "Socket timeout",
                    3, 2000),

                SocketError.ConnectionRefused => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.High,
                    true,
                    "Check service availability and retry",
                    "Connection refused - service may be down",
                    3, 5000),

                SocketError.HostNotFound => new ErrorClassification(
                    ErrorCategory.Configuration,
                    ErrorSeverity.High,
                    false,
                    "Check host configuration",
                    "Host not found - check DNS/configuration",
                    0, 0),

                SocketError.NetworkUnreachable => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.High,
                    true,
                    "Check network connectivity",
                    "Network unreachable",
                    2, 10000),

                _ => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry after delay",
                    $"Socket error: {socketEx.SocketErrorCode}",
                    3, 2000)
            };
        }

        /// <summary>
        /// Classifies web exceptions
        /// </summary>
        private ErrorClassification ClassifyWebException(WebException webEx)
        {
            return webEx.Status switch
            {
                WebExceptionStatus.Timeout => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with exponential backoff",
                    "Web request timeout",
                    5, 1000),

                WebExceptionStatus.ConnectFailure => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.High,
                    true,
                    "Check connectivity and retry",
                    "Connection failure",
                    3, 3000),

                WebExceptionStatus.NameResolutionFailure => new ErrorClassification(
                    ErrorCategory.Configuration,
                    ErrorSeverity.High,
                    false,
                    "Check DNS configuration",
                    "DNS resolution failed",
                    0, 0),

                WebExceptionStatus.TrustFailure => new ErrorClassification(
                    ErrorCategory.Security,
                    ErrorSeverity.High,
                    false,
                    "Check SSL certificate configuration",
                    "SSL/TLS trust failure",
                    0, 0),

                _ => new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry after delay",
                    $"Web error: {webEx.Status}",
                    3, 2000)
            };
        }

        /// <summary>
        /// Classifies database exceptions
        /// </summary>
        private ErrorClassification ClassifyDatabaseException(DbException dbEx)
        {
            var message = dbEx.Message?.ToLower() ?? string.Empty;

            if (message.Contains("timeout") || dbEx.ErrorCode == -2)
            {
                return new ErrorClassification(
                    ErrorCategory.Transient,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with longer timeout",
                    "Database timeout",
                    3, 3000);
            }

            if (message.Contains("connection") || message.Contains("network"))
            {
                return new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.High,
                    true,
                    "Check database connectivity",
                    "Database connection error",
                    3, 5000);
            }

            if (message.Contains("deadlock") || message.Contains("lock"))
            {
                return new ErrorClassification(
                    ErrorCategory.Transient,
                    ErrorSeverity.Medium,
                    true,
                    "Retry after random delay",
                    "Database lock/deadlock",
                    3, 1000);
            }

            if (message.Contains("syntax") || message.Contains("invalid"))
            {
                return new ErrorClassification(
                    ErrorCategory.Validation,
                    ErrorSeverity.High,
                    false,
                    "Fix query syntax",
                    "SQL syntax or validation error",
                    0, 0);
            }

            return new ErrorClassification(
                ErrorCategory.Unknown,
                ErrorSeverity.Medium,
                false,
                "Check database logs",
                $"Database error: {dbEx.Message}",
                1, 3000);
        }

        /// <summary>
        /// Classifies Postgrest exceptions
        /// </summary>
        private ErrorClassification ClassifyPostgrestException(Postgrest.Exceptions.PostgrestException pgEx)
        {
            var statusCode = (int)pgEx.StatusCode;

            if (statusCode == 408) // RequestTimeout
            {
                return new ErrorClassification(
                    ErrorCategory.Network,
                    ErrorSeverity.Medium,
                    true,
                    "Retry with exponential backoff",
                    "Request timeout",
                    5, 1000);
            }

            if (statusCode == 429) // TooManyRequests
            {
                return new ErrorClassification(
                    ErrorCategory.Resource,
                    ErrorSeverity.Medium,
                    true,
                    "Retry after delay with rate limiting",
                    "Rate limit exceeded",
                    3, 5000);
            }

            if (statusCode == 500) // InternalServerError
            {
                return new ErrorClassification(
                    ErrorCategory.Transient,
                    ErrorSeverity.High,
                    true,
                    "Retry after delay",
                    "Server error",
                    2, 3000);
            }

            if (statusCode == 502 || statusCode == 503 || statusCode == 504) // BadGateway, ServiceUnavailable, GatewayTimeout
            {
                return new ErrorClassification(
                    ErrorCategory.Transient,
                    ErrorSeverity.High,
                    true,
                    "Retry after delay - server temporarily unavailable",
                    "Server temporarily unavailable",
                    3, 5000);
            }

            if (statusCode == 401) // Unauthorized
            {
                return new ErrorClassification(
                    ErrorCategory.Security,
                    ErrorSeverity.High,
                    false,
                    "Check authentication credentials",
                    "Authentication failed",
                    0, 0);
            }

            if (statusCode == 403) // Forbidden
            {
                return new ErrorClassification(
                    ErrorCategory.Security,
                    ErrorSeverity.High,
                    false,
                    "Check authorization permissions",
                    "Access forbidden",
                    0, 0);
            }

            if (statusCode == 400) // BadRequest
            {
                return new ErrorClassification(
                    ErrorCategory.Validation,
                    ErrorSeverity.Medium,
                    false,
                    "Fix request parameters",
                    "Bad request - invalid parameters",
                    0, 0);
            }

            // Default case
            return new ErrorClassification(
                ErrorCategory.Unknown,
                ErrorSeverity.Medium,
                false,
                "Check API response details",
                $"Postgrest error: {pgEx.StatusCode}",
                1, 2000);
        }

        #endregion
    }
}