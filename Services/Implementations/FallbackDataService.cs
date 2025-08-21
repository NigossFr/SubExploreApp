using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Intelligent fallback service between Supabase API and direct database access
    /// </summary>
    public class FallbackDataService : IFallbackDataService
    {
        private readonly ILogger<FallbackDataService> _logger;
        private readonly ICircuitBreakerService? _apiCircuitBreaker;
        private readonly ICircuitBreakerService? _databaseCircuitBreaker;
        private readonly IErrorCategorizationService? _errorCategorizationService;
        private readonly IConnectionHealthService? _connectionHealthService;

        // State tracking
        private DataAccessMethod _preferredMethod = DataAccessMethod.SupabaseApi;
        private DataAccessMethod _activeMethod = DataAccessMethod.SupabaseApi;
        private bool _isForced = false;
        private string? _forcedReason;
        private DateTime? _lastMethodChange;
        private string? _currentMethodReason;

        // Success tracking for intelligent fallback decisions
        private int _apiSuccesses = 0;
        private int _apiFailures = 0;
        private int _databaseSuccesses = 0;
        private int _databaseFailures = 0;
        private int _successfulFallbacks = 0;
        private int _totalFailures = 0;

        // Configuration
        private readonly double _fallbackThreshold = 0.3; // Switch if success rate drops below 30%
        private readonly int _minimumOperationsForDecision = 5;

        public event EventHandler<DataAccessMethodChangedEventArgs>? DataAccessMethodChanged;

        public FallbackDataService(
            ILogger<FallbackDataService> logger,
            ICircuitBreakerService? apiCircuitBreaker = null,
            ICircuitBreakerService? databaseCircuitBreaker = null,
            IErrorCategorizationService? errorCategorizationService = null,
            IConnectionHealthService? connectionHealthService = null)
        {
            _logger = logger;
            _apiCircuitBreaker = apiCircuitBreaker;
            _databaseCircuitBreaker = databaseCircuitBreaker;
            _errorCategorizationService = errorCategorizationService;
            _connectionHealthService = connectionHealthService;

            _currentMethodReason = "Default startup configuration";
            _lastMethodChange = DateTime.UtcNow;
        }

        public DataAccessMethod PreferredMethod => _preferredMethod;
        public DataAccessMethod ActiveMethod => _activeMethod;
        public bool IsFallbackActive => _activeMethod != _preferredMethod;

        /// <summary>
        /// Determines the best data access method based on current conditions
        /// </summary>
        public async Task<FallbackDecision> DetermineDataAccessMethodAsync()
        {
            // If method is forced, return the forced method
            if (_isForced)
            {
                return new FallbackDecision(_activeMethod, _forcedReason ?? "Method manually forced", false);
            }

            // Check circuit breaker states
            var apiCircuitOpen = _apiCircuitBreaker?.State == CircuitBreakerState.Open;
            var databaseCircuitOpen = _databaseCircuitBreaker?.State == CircuitBreakerState.Open;

            // If API circuit is open, use database
            if (apiCircuitOpen && !databaseCircuitOpen)
            {
                var decision = new FallbackDecision(
                    DataAccessMethod.DirectDatabase, 
                    "API circuit breaker is open", 
                    true);
                
                await ChangeActiveMethodAsync(decision.Method, decision.Reason, decision.IsFallback);
                return decision;
            }

            // If database circuit is open, use API
            if (databaseCircuitOpen && !apiCircuitOpen)
            {
                var decision = new FallbackDecision(
                    DataAccessMethod.SupabaseApi, 
                    "Database circuit breaker is open", 
                    _preferredMethod != DataAccessMethod.SupabaseApi);
                
                await ChangeActiveMethodAsync(decision.Method, decision.Reason, decision.IsFallback);
                return decision;
            }

            // If both circuits are open, choose based on success rates
            if (apiCircuitOpen && databaseCircuitOpen)
            {
                var betterMethod = GetApiSuccessRate() >= GetDatabaseSuccessRate() 
                    ? DataAccessMethod.SupabaseApi 
                    : DataAccessMethod.DirectDatabase;
                
                var decision = new FallbackDecision(
                    betterMethod, 
                    "Both circuit breakers open - choosing based on success rates", 
                    true);
                
                await ChangeActiveMethodAsync(decision.Method, decision.Reason, decision.IsFallback);
                return decision;
            }

            // Check connection health if available
            if (_connectionHealthService != null)
            {
                var healthReport = _connectionHealthService.GetCurrentHealth();
                
                if (healthReport.OverallStatus == HealthStatus.Unhealthy)
                {
                    // If overall health is poor, prefer database connection
                    var decision = new FallbackDecision(
                        DataAccessMethod.DirectDatabase, 
                        "Overall connection health is poor", 
                        _preferredMethod != DataAccessMethod.DirectDatabase);
                    
                    await ChangeActiveMethodAsync(decision.Method, decision.Reason, decision.IsFallback);
                    return decision;
                }
            }

            // Make intelligent decision based on success rates
            var apiSuccessRate = GetApiSuccessRate();
            var databaseSuccessRate = GetDatabaseSuccessRate();

            // If we have enough data to make an informed decision
            if ((_apiSuccesses + _apiFailures) >= _minimumOperationsForDecision ||
                (_databaseSuccesses + _databaseFailures) >= _minimumOperationsForDecision)
            {
                // Switch if current method is performing poorly
                if (_activeMethod == DataAccessMethod.SupabaseApi && apiSuccessRate < _fallbackThreshold && databaseSuccessRate > apiSuccessRate)
                {
                    var decision = new FallbackDecision(
                        DataAccessMethod.DirectDatabase, 
                        $"API success rate ({apiSuccessRate:P1}) below threshold ({_fallbackThreshold:P1})", 
                        true);
                    
                    await ChangeActiveMethodAsync(decision.Method, decision.Reason, decision.IsFallback);
                    return decision;
                }
                
                if (_activeMethod == DataAccessMethod.DirectDatabase && databaseSuccessRate < _fallbackThreshold && apiSuccessRate > databaseSuccessRate)
                {
                    var decision = new FallbackDecision(
                        DataAccessMethod.SupabaseApi, 
                        $"Database success rate ({databaseSuccessRate:P1}) below threshold ({_fallbackThreshold:P1})", 
                        _preferredMethod != DataAccessMethod.SupabaseApi);
                    
                    await ChangeActiveMethodAsync(decision.Method, decision.Reason, decision.IsFallback);
                    return decision;
                }
            }

            // Default: stick with current active method
            return new FallbackDecision(_activeMethod, "Current method performing adequately", IsFallbackActive);
        }

        /// <summary>
        /// Executes an operation with automatic fallback between API and database
        /// </summary>
        public async Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> apiOperation,
            Func<Task<T>> databaseOperation,
            string operationName)
        {
            if (apiOperation == null)
                throw new ArgumentNullException(nameof(apiOperation));
            if (databaseOperation == null)
                throw new ArgumentNullException(nameof(databaseOperation));

            _logger.LogDebug("🔄 Executing {OperationName} with fallback", operationName);

            // Determine best method
            var decision = await DetermineDataAccessMethodAsync();
            
            // Try primary method
            try
            {
                T result;
                if (decision.Method == DataAccessMethod.SupabaseApi)
                {
                    _logger.LogDebug("🌐 Executing {OperationName} via Supabase API", operationName);
                    result = await apiOperation();
                    await ReportMethodSuccessAsync(DataAccessMethod.SupabaseApi);
                }
                else
                {
                    _logger.LogDebug("💾 Executing {OperationName} via Direct Database", operationName);
                    result = await databaseOperation();
                    await ReportMethodSuccessAsync(DataAccessMethod.DirectDatabase);
                }

                _logger.LogDebug("✅ {OperationName} completed successfully via {Method}", 
                    operationName, decision.Method);
                
                return result;
            }
            catch (Exception primaryException)
            {
                await ReportMethodFailureAsync(decision.Method, primaryException);
                
                _logger.LogWarning(primaryException, 
                    "⚠️ {OperationName} failed via {PrimaryMethod}, attempting fallback", 
                    operationName, decision.Method);

                // Determine if we should retry with the other method
                var shouldFallback = ShouldAttemptFallback(primaryException);
                
                if (!shouldFallback)
                {
                    _logger.LogError("❌ {OperationName} failed with non-retryable error via {Method}", 
                        operationName, decision.Method);
                    throw;
                }

                // Try fallback method
                try
                {
                    T result;
                    var fallbackMethod = decision.Method == DataAccessMethod.SupabaseApi 
                        ? DataAccessMethod.DirectDatabase 
                        : DataAccessMethod.SupabaseApi;

                    if (fallbackMethod == DataAccessMethod.SupabaseApi)
                    {
                        _logger.LogInformation("🔄 Fallback: Executing {OperationName} via Supabase API", operationName);
                        result = await apiOperation();
                        await ReportMethodSuccessAsync(DataAccessMethod.SupabaseApi);
                    }
                    else
                    {
                        _logger.LogInformation("🔄 Fallback: Executing {OperationName} via Direct Database", operationName);
                        result = await databaseOperation();
                        await ReportMethodSuccessAsync(DataAccessMethod.DirectDatabase);
                    }

                    _successfulFallbacks++;
                    
                    _logger.LogInformation("✅ {OperationName} completed successfully via fallback {Method}", 
                        operationName, fallbackMethod);
                    
                    return result;
                }
                catch (Exception fallbackException)
                {
                    var fallbackMethod = decision.Method == DataAccessMethod.SupabaseApi 
                        ? DataAccessMethod.DirectDatabase 
                        : DataAccessMethod.SupabaseApi;
                    
                    await ReportMethodFailureAsync(fallbackMethod, fallbackException);
                    _totalFailures++;
                    
                    _logger.LogError(fallbackException,
                        "❌ {OperationName} failed via both {PrimaryMethod} and {FallbackMethod}", 
                        operationName, decision.Method, fallbackMethod);
                    
                    // Throw the more informative exception
                    var betterException = GetMoreInformativeException(primaryException, fallbackException);
                    throw betterException;
                }
            }
        }

        /// <summary>
        /// Executes an operation with automatic fallback (void return)
        /// </summary>
        public async Task ExecuteWithFallbackAsync(
            Func<Task> apiOperation,
            Func<Task> databaseOperation,
            string operationName)
        {
            await ExecuteWithFallbackAsync(
                async () =>
                {
                    await apiOperation();
                    return true; // Dummy return value
                },
                async () =>
                {
                    await databaseOperation();
                    return true; // Dummy return value
                },
                operationName);
        }

        /// <summary>
        /// Forces the service to use a specific data access method
        /// </summary>
        public void ForceDataAccessMethod(DataAccessMethod method, string reason)
        {
            _logger.LogInformation("🔧 Forcing data access method to {Method}. Reason: {Reason}", method, reason);
            
            _isForced = true;
            _forcedReason = reason;
            
            var previousMethod = _activeMethod;
            _activeMethod = method;
            _currentMethodReason = $"Forced: {reason}";
            _lastMethodChange = DateTime.UtcNow;

            FireDataAccessMethodChangedEvent(previousMethod, method, reason, false);
        }

        /// <summary>
        /// Resets to automatic method selection
        /// </summary>
        public void ResetToAutomatic()
        {
            _logger.LogInformation("🔄 Resetting to automatic data access method selection");
            
            _isForced = false;
            _forcedReason = null;
            _currentMethodReason = "Automatic selection";
            _lastMethodChange = DateTime.UtcNow;

            // Trigger immediate method evaluation
            _ = Task.Run(async () => await DetermineDataAccessMethodAsync());
        }

        /// <summary>
        /// Reports a failure for the specified method
        /// </summary>
        public async Task ReportMethodFailureAsync(DataAccessMethod method, Exception exception)
        {
            if (method == DataAccessMethod.SupabaseApi)
            {
                _apiFailures++;
            }
            else if (method == DataAccessMethod.DirectDatabase)
            {
                _databaseFailures++;
            }

            _logger.LogDebug("📊 Method failure reported: {Method}. API: {ApiSuccessRate:P1} ({ApiTotal}), DB: {DbSuccessRate:P1} ({DbTotal})",
                method, GetApiSuccessRate(), _apiSuccesses + _apiFailures, 
                GetDatabaseSuccessRate(), _databaseSuccesses + _databaseFailures);

            // Trigger method re-evaluation
            await DetermineDataAccessMethodAsync();
        }

        /// <summary>
        /// Reports a success for the specified method
        /// </summary>
        public async Task ReportMethodSuccessAsync(DataAccessMethod method)
        {
            if (method == DataAccessMethod.SupabaseApi)
            {
                _apiSuccesses++;
            }
            else if (method == DataAccessMethod.DirectDatabase)
            {
                _databaseSuccesses++;
            }

            _logger.LogDebug("📊 Method success reported: {Method}. API: {ApiSuccessRate:P1} ({ApiTotal}), DB: {DbSuccessRate:P1} ({DbTotal})",
                method, GetApiSuccessRate(), _apiSuccesses + _apiFailures, 
                GetDatabaseSuccessRate(), _databaseSuccesses + _databaseFailures);
        }

        /// <summary>
        /// Gets current fallback statistics and status
        /// </summary>
        public FallbackStatus GetStatus()
        {
            return new FallbackStatus(
                _preferredMethod,
                _activeMethod,
                IsFallbackActive,
                GetApiSuccessRate(),
                GetDatabaseSuccessRate(),
                _apiSuccesses + _apiFailures,
                _databaseSuccesses + _databaseFailures,
                _successfulFallbacks,
                _totalFailures,
                _lastMethodChange,
                _currentMethodReason);
        }

        #region Private Helper Methods

        /// <summary>
        /// Changes the active method and fires events
        /// </summary>
        private async Task ChangeActiveMethodAsync(DataAccessMethod newMethod, string reason, bool isFallback)
        {
            if (_activeMethod == newMethod)
                return;

            var previousMethod = _activeMethod;
            _activeMethod = newMethod;
            _currentMethodReason = reason;
            _lastMethodChange = DateTime.UtcNow;

            _logger.LogInformation("🔄 Data access method changed: {PreviousMethod} → {CurrentMethod}. Reason: {Reason}{FallbackIndicator}", 
                previousMethod, newMethod, reason, isFallback ? " (FALLBACK)" : "");

            FireDataAccessMethodChangedEvent(previousMethod, newMethod, reason, isFallback);
        }

        /// <summary>
        /// Fires the data access method changed event
        /// </summary>
        private void FireDataAccessMethodChangedEvent(DataAccessMethod previousMethod, DataAccessMethod currentMethod, string reason, bool isFallback)
        {
            try
            {
                DataAccessMethodChanged?.Invoke(this, new DataAccessMethodChangedEventArgs(
                    previousMethod, currentMethod, reason, isFallback));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error firing DataAccessMethodChanged event");
            }
        }

        /// <summary>
        /// Calculates API success rate
        /// </summary>
        private double GetApiSuccessRate()
        {
            var total = _apiSuccesses + _apiFailures;
            return total == 0 ? 1.0 : (double)_apiSuccesses / total;
        }

        /// <summary>
        /// Calculates database success rate
        /// </summary>
        private double GetDatabaseSuccessRate()
        {
            var total = _databaseSuccesses + _databaseFailures;
            return total == 0 ? 1.0 : (double)_databaseSuccesses / total;
        }

        /// <summary>
        /// Determines if fallback should be attempted based on the exception
        /// </summary>
        private bool ShouldAttemptFallback(Exception exception)
        {
            if (_errorCategorizationService != null)
            {
                var classification = _errorCategorizationService.ClassifyError(exception);
                
                // Don't fallback for validation, security, or configuration errors
                return classification.Category switch
                {
                    ErrorCategory.Validation => false,
                    ErrorCategory.Security => false,
                    ErrorCategory.Configuration => false,
                    _ => true
                };
            }

            // Default: attempt fallback for most errors
            return exception switch
            {
                ArgumentNullException => false,
                ArgumentException => false,
                UnauthorizedAccessException => false,
                _ => true
            };
        }

        /// <summary>
        /// Returns the more informative of two exceptions
        /// </summary>
        private Exception GetMoreInformativeException(Exception primary, Exception fallback)
        {
            // Prefer the exception with more specific information
            if (primary.InnerException != null && fallback.InnerException == null)
                return primary;
            
            if (fallback.InnerException != null && primary.InnerException == null)
                return fallback;
            
            // Prefer the exception with a longer message (usually more detailed)
            if (primary.Message.Length > fallback.Message.Length)
                return primary;
            
            return fallback;
        }

        #endregion
    }
}