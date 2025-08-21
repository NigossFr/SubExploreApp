using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Circuit breaker implementation to prevent cascading failures
    /// </summary>
    public class CircuitBreakerService : ICircuitBreakerService
    {
        private readonly ILogger<CircuitBreakerService> _logger;
        private readonly object _lock = new object();

        // Configuration
        private readonly int _failureThreshold;
        private readonly TimeSpan _timeout;
        private readonly int _successThreshold;

        // State
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount = 0;
        private int _successCount = 0;
        private DateTime? _lastFailureTime;
        private DateTime? _lastSuccessTime;
        private int _halfOpenSuccessCount = 0;

        public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
        {
            _logger = logger;
            
            // Default configuration - can be made configurable later
            _failureThreshold = 5;      // Open circuit after 5 consecutive failures
            _timeout = TimeSpan.FromMinutes(1); // Wait 1 minute before trying half-open
            _successThreshold = 3;      // Close circuit after 3 consecutive successes in half-open
        }

        /// <summary>
        /// Constructor with custom configuration
        /// </summary>
        public CircuitBreakerService(
            ILogger<CircuitBreakerService> logger,
            int failureThreshold = 5,
            TimeSpan? timeout = null,
            int successThreshold = 3)
        {
            _logger = logger;
            _failureThreshold = failureThreshold;
            _timeout = timeout ?? TimeSpan.FromMinutes(1);
            _successThreshold = successThreshold;
        }

        public CircuitBreakerState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        public int FailureCount
        {
            get
            {
                lock (_lock)
                {
                    return _failureCount;
                }
            }
        }

        public DateTime? LastFailureTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastFailureTime;
                }
            }
        }

        /// <summary>
        /// Executes an operation through the circuit breaker
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Check circuit state and determine if we should execute
            var currentState = GetCurrentState();
            
            switch (currentState)
            {
                case CircuitBreakerState.Open:
                    var timeUntilRetry = GetTimeUntilRetry();
                    throw new CircuitBreakerOpenException(
                        $"Circuit breaker is OPEN. {_failureCount} consecutive failures. Retry in {timeUntilRetry.TotalSeconds:F0} seconds.",
                        _failureCount,
                        timeUntilRetry);

                case CircuitBreakerState.HalfOpen:
                    _logger.LogInformation("🟡 Circuit breaker is HALF-OPEN - testing recovery");
                    break;

                case CircuitBreakerState.Closed:
                    // Normal operation
                    break;
            }

            try
            {
                var result = await operation(cancellationToken);
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation through the circuit breaker (void return)
        /// </summary>
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async (ct) =>
            {
                await operation(ct);
                return true; // Return dummy value for generic method
            }, cancellationToken);
        }

        /// <summary>
        /// Manually resets the circuit breaker to closed state
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _halfOpenSuccessCount = 0;
                _lastFailureTime = null;
                _lastSuccessTime = DateTime.UtcNow;
                
                _logger.LogInformation("🔄 Circuit breaker manually reset to CLOSED state");
            }
        }

        /// <summary>
        /// Gets the current health status of the circuit breaker
        /// </summary>
        public CircuitBreakerHealthStatus GetHealthStatus()
        {
            lock (_lock)
            {
                var timeUntilRetry = _state == CircuitBreakerState.Open ? GetTimeUntilRetry() : (TimeSpan?)null;
                
                return new CircuitBreakerHealthStatus
                {
                    State = _state,
                    FailureCount = _failureCount,
                    SuccessCount = _successCount,
                    LastFailureTime = _lastFailureTime,
                    LastSuccessTime = _lastSuccessTime,
                    TimeUntilRetry = timeUntilRetry
                };
            }
        }

        #region Private Methods

        /// <summary>
        /// Gets the current state, potentially transitioning from Open to HalfOpen
        /// </summary>
        private CircuitBreakerState GetCurrentState()
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    // Check if enough time has passed to transition to half-open
                    if (_lastFailureTime.HasValue && DateTime.UtcNow >= _lastFailureTime.Value + _timeout)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _halfOpenSuccessCount = 0;
                        _logger.LogInformation("🟡 Circuit breaker transitioning from OPEN to HALF-OPEN");
                    }
                }

                return _state;
            }
        }

        /// <summary>
        /// Handles successful operation execution
        /// </summary>
        private void OnSuccess()
        {
            lock (_lock)
            {
                _successCount++;
                _lastSuccessTime = DateTime.UtcNow;

                switch (_state)
                {
                    case CircuitBreakerState.HalfOpen:
                        _halfOpenSuccessCount++;
                        if (_halfOpenSuccessCount >= _successThreshold)
                        {
                            // Close the circuit - service has recovered
                            _state = CircuitBreakerState.Closed;
                            _failureCount = 0;
                            _halfOpenSuccessCount = 0;
                            _lastFailureTime = null;
                            
                            _logger.LogInformation(
                                "✅ Circuit breaker CLOSED - service recovered after {SuccessCount} successful operations",
                                _halfOpenSuccessCount);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "🟡 Circuit breaker HALF-OPEN - {CurrentSuccess}/{RequiredSuccess} successes",
                                _halfOpenSuccessCount, _successThreshold);
                        }
                        break;

                    case CircuitBreakerState.Closed:
                        // Reset failure count on success
                        if (_failureCount > 0)
                        {
                            _logger.LogDebug("✅ Circuit breaker success - resetting failure count from {FailureCount}", _failureCount);
                            _failureCount = 0;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Handles failed operation execution
        /// </summary>
        private void OnFailure(Exception exception)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                switch (_state)
                {
                    case CircuitBreakerState.Closed:
                        if (_failureCount >= _failureThreshold)
                        {
                            // Open the circuit
                            _state = CircuitBreakerState.Open;
                            _halfOpenSuccessCount = 0;
                            
                            _logger.LogWarning(exception,
                                "❌ Circuit breaker OPENED - {FailureCount} consecutive failures. Will retry in {TimeoutMinutes} minutes",
                                _failureCount, _timeout.TotalMinutes);
                        }
                        else
                        {
                            _logger.LogWarning(exception,
                                "⚠️ Circuit breaker failure {FailureCount}/{FailureThreshold}. Error: {Error}",
                                _failureCount, _failureThreshold, exception.Message);
                        }
                        break;

                    case CircuitBreakerState.HalfOpen:
                        // Return to open state
                        _state = CircuitBreakerState.Open;
                        _halfOpenSuccessCount = 0;
                        
                        _logger.LogWarning(exception,
                            "❌ Circuit breaker returned to OPEN - failure during half-open test. Error: {Error}",
                            exception.Message);
                        break;

                    case CircuitBreakerState.Open:
                        // Already open, just log
                        _logger.LogDebug(exception,
                            "❌ Circuit breaker failure while OPEN. Error: {Error}",
                            exception.Message);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the time remaining until the circuit can transition to half-open
        /// </summary>
        private TimeSpan GetTimeUntilRetry()
        {
            if (!_lastFailureTime.HasValue)
                return TimeSpan.Zero;

            var retryTime = _lastFailureTime.Value + _timeout;
            var timeUntilRetry = retryTime - DateTime.UtcNow;
            
            return timeUntilRetry > TimeSpan.Zero ? timeUntilRetry : TimeSpan.Zero;
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown when circuit breaker is in open state
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public int FailureCount { get; }
        public TimeSpan RetryAfter { get; }

        public CircuitBreakerOpenException(string message, int failureCount, TimeSpan retryAfter) 
            : base(message)
        {
            FailureCount = failureCount;
            RetryAfter = retryAfter;
        }

        public CircuitBreakerOpenException(string message, int failureCount, TimeSpan retryAfter, Exception innerException) 
            : base(message, innerException)
        {
            FailureCount = failureCount;
            RetryAfter = retryAfter;
        }
    }
}