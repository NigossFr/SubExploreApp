using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Automatic reconnection service for managing connection recovery
    /// </summary>
    public class AutoReconnectService : IAutoReconnectService, IDisposable
    {
        private readonly ILogger<AutoReconnectService> _logger;
        private readonly IRetryPolicyService? _retryPolicyService;
        private readonly IErrorCategorizationService? _errorCategorizationService;

        private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<bool>>> _connectionTests;
        private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<bool>>> _reconnectFunctions;

        private ConnectionState _currentState = ConnectionState.Unknown;
        private int _reconnectAttempts = 0;
        private DateTime? _lastConnectedTime;
        private DateTime? _lastFailureTime;
        private string? _lastFailureReason;
        private bool _isAutoReconnectEnabled = false;
        private bool _disposed = false;

        private CancellationTokenSource? _monitoringCts;
        private Task? _monitoringTask;

        // Configuration
        private readonly TimeSpan _connectionTestInterval = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(30);
        private readonly int _maxReconnectAttempts = 10;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        public AutoReconnectService(
            ILogger<AutoReconnectService> logger,
            IRetryPolicyService? retryPolicyService = null,
            IErrorCategorizationService? errorCategorizationService = null)
        {
            _logger = logger;
            _retryPolicyService = retryPolicyService;
            _errorCategorizationService = errorCategorizationService;

            _connectionTests = new ConcurrentDictionary<string, Func<CancellationToken, Task<bool>>>();
            _reconnectFunctions = new ConcurrentDictionary<string, Func<CancellationToken, Task<bool>>>();
        }

        public ConnectionState CurrentState => _currentState;
        public int ReconnectAttempts => _reconnectAttempts;
        public DateTime? LastConnectedTime => _lastConnectedTime;
        public bool IsAutoReconnectEnabled => _isAutoReconnectEnabled;

        /// <summary>
        /// Starts automatic reconnection monitoring
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isAutoReconnectEnabled)
            {
                _logger.LogWarning("⚠️ Auto-reconnect service is already running");
                return;
            }

            _logger.LogInformation("🚀 Starting auto-reconnect service with {Interval}s test interval", 
                _connectionTestInterval.TotalSeconds);

            _isAutoReconnectEnabled = true;
            _monitoringCts = new CancellationTokenSource();

            // Perform initial connection test
            await TestConnectionsAsync();

            // Start background monitoring
            _monitoringTask = Task.Run(async () => await MonitoringLoopAsync(_monitoringCts.Token), _monitoringCts.Token);
        }

        /// <summary>
        /// Stops automatic reconnection monitoring
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isAutoReconnectEnabled)
            {
                return;
            }

            _logger.LogInformation("⏹️ Stopping auto-reconnect service");
            
            _isAutoReconnectEnabled = false;
            
            // Cancel monitoring
            _monitoringCts?.Cancel();
            
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
            }

            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
        }

        /// <summary>
        /// Manually triggers a reconnection attempt
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            _logger.LogInformation("🔧 Manual reconnection attempt initiated");
            
            var success = await PerformReconnectionAsync();
            
            if (success)
            {
                await ReportConnectionSuccessAsync();
            }
            else
            {
                _reconnectAttempts++;
            }

            return success;
        }

        /// <summary>
        /// Reports a connection loss to trigger reconnection
        /// </summary>
        public async Task ReportConnectionLossAsync(string? reason = null)
        {
            _logger.LogWarning("📢 Connection loss reported: {Reason}", reason ?? "Unknown reason");

            _lastFailureTime = DateTime.UtcNow;
            _lastFailureReason = reason;

            await ChangeStateAsync(ConnectionState.Disconnected, reason);

            // Trigger immediate reconnection attempt if enabled
            if (_isAutoReconnectEnabled)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_reconnectDelay);
                    await TryReconnectAsync();
                });
            }
        }

        /// <summary>
        /// Reports a successful connection to update state
        /// </summary>
        public async Task ReportConnectionSuccessAsync()
        {
            _logger.LogInformation("📢 Connection success reported");

            _lastConnectedTime = DateTime.UtcNow;
            _reconnectAttempts = 0;
            
            await ChangeStateAsync(ConnectionState.Connected, "Connection restored");
        }

        /// <summary>
        /// Registers a connection test function
        /// </summary>
        public void RegisterConnectionTest(string name, Func<CancellationToken, Task<bool>> testFunction)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Test name cannot be null or empty", nameof(name));

            if (testFunction == null)
                throw new ArgumentNullException(nameof(testFunction));

            _connectionTests.AddOrUpdate(name, testFunction, (_, __) => testFunction);
            _logger.LogDebug("➕ Registered connection test: {Name}", name);
        }

        /// <summary>
        /// Registers a reconnection function
        /// </summary>
        public void RegisterReconnectFunction(string name, Func<CancellationToken, Task<bool>> reconnectFunction)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Function name cannot be null or empty", nameof(name));

            if (reconnectFunction == null)
                throw new ArgumentNullException(nameof(reconnectFunction));

            _reconnectFunctions.AddOrUpdate(name, reconnectFunction, (_, __) => reconnectFunction);
            _logger.LogDebug("➕ Registered reconnect function: {Name}", name);
        }

        /// <summary>
        /// Unregisters a connection test
        /// </summary>
        public void UnregisterConnectionTest(string name)
        {
            if (_connectionTests.TryRemove(name, out _))
            {
                _logger.LogDebug("➖ Unregistered connection test: {Name}", name);
            }
        }

        /// <summary>
        /// Unregisters a reconnection function
        /// </summary>
        public void UnregisterReconnectFunction(string name)
        {
            if (_reconnectFunctions.TryRemove(name, out _))
            {
                _logger.LogDebug("➖ Unregistered reconnect function: {Name}", name);
            }
        }

        /// <summary>
        /// Gets the current reconnection status
        /// </summary>
        public ReconnectStatus GetStatus()
        {
            return new ReconnectStatus(
                _currentState,
                _reconnectAttempts,
                _lastConnectedTime,
                _lastFailureTime,
                _lastFailureReason,
                _isAutoReconnectEnabled,
                _connectionTests.Count,
                _reconnectFunctions.Count);
        }

        #region Private Methods

        /// <summary>
        /// Background monitoring loop
        /// </summary>
        private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔄 Auto-reconnect monitoring loop started");

            while (_isAutoReconnectEnabled && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_connectionTestInterval, cancellationToken);
                    
                    if (_isAutoReconnectEnabled && !cancellationToken.IsCancellationRequested)
                    {
                        await TestConnectionsAsync();
                        
                        // If disconnected, attempt reconnection
                        if (_currentState == ConnectionState.Disconnected)
                        {
                            await TryReconnectAsync();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in auto-reconnect monitoring loop");
                    
                    // Continue monitoring despite errors
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }

            _logger.LogInformation("⏹️ Auto-reconnect monitoring loop stopped");
        }

        /// <summary>
        /// Tests all registered connection tests
        /// </summary>
        private async Task TestConnectionsAsync()
        {
            if (!_connectionTests.Any())
            {
                _logger.LogDebug("🔍 No connection tests registered");
                return;
            }

            _logger.LogDebug("🔍 Testing {Count} connection(s)", _connectionTests.Count);

            var results = await Task.WhenAll(
                _connectionTests.Select(kvp => TestSingleConnectionAsync(kvp.Key, kvp.Value)));

            var allConnected = results.All(r => r);
            
            if (allConnected && _currentState != ConnectionState.Connected)
            {
                await ReportConnectionSuccessAsync();
            }
            else if (!allConnected && _currentState == ConnectionState.Connected)
            {
                await ReportConnectionLossAsync("Connection test failure detected");
            }
        }

        /// <summary>
        /// Tests a single connection
        /// </summary>
        private async Task<bool> TestSingleConnectionAsync(string name, Func<CancellationToken, Task<bool>> testFunction)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var result = await testFunction(cts.Token);
                
                _logger.LogDebug("🔍 Connection test '{Name}': {Result}", name, result ? "✅ OK" : "❌ FAIL");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Connection test '{Name}' failed with exception", name);
                return false;
            }
        }

        /// <summary>
        /// Attempts reconnection if not at maximum attempts
        /// </summary>
        private async Task TryReconnectAsync()
        {
            if (_reconnectAttempts >= _maxReconnectAttempts)
            {
                _logger.LogWarning("⚠️ Maximum reconnection attempts ({MaxAttempts}) reached", _maxReconnectAttempts);
                return;
            }

            await ChangeStateAsync(ConnectionState.Reconnecting, $"Attempt {_reconnectAttempts + 1}/{_maxReconnectAttempts}");
            
            var success = await PerformReconnectionAsync();
            
            if (success)
            {
                await ReportConnectionSuccessAsync();
            }
            else
            {
                _reconnectAttempts++;
                await ChangeStateAsync(ConnectionState.Disconnected, $"Reconnection failed - attempt {_reconnectAttempts}");
            }
        }

        /// <summary>
        /// Performs the actual reconnection using registered functions
        /// </summary>
        private async Task<bool> PerformReconnectionAsync()
        {
            if (!_reconnectFunctions.Any())
            {
                _logger.LogWarning("⚠️ No reconnection functions registered");
                return false;
            }

            _logger.LogInformation("🔄 Attempting reconnection using {Count} function(s)", _reconnectFunctions.Count);

            // Try each reconnection function
            foreach (var kvp in _reconnectFunctions)
            {
                try
                {
                    var success = await ExecuteReconnectFunctionAsync(kvp.Key, kvp.Value);
                    if (success)
                    {
                        _logger.LogInformation("✅ Reconnection successful using function: {Name}", kvp.Key);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "❌ Reconnection function '{Name}' failed", kvp.Key);
                }
            }

            _logger.LogWarning("❌ All reconnection functions failed");
            return false;
        }

        /// <summary>
        /// Executes a single reconnection function with resilience
        /// </summary>
        private async Task<bool> ExecuteReconnectFunctionAsync(string name, Func<CancellationToken, Task<bool>> function)
        {
            if (_retryPolicyService != null)
            {
                return await _retryPolicyService.ExecuteWithRetryAsync(
                    async (ct) => await function(ct),
                    maxRetries: 2,
                    baseDelay: 2000,
                    maxDelay: 10000);
            }
            else
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                return await function(cts.Token);
            }
        }

        /// <summary>
        /// Changes the connection state and notifies listeners
        /// </summary>
        private async Task ChangeStateAsync(ConnectionState newState, string? reason = null)
        {
            if (_currentState == newState)
                return;

            var previousState = _currentState;
            _currentState = newState;

            _logger.LogInformation("🔄 Connection state changed: {PreviousState} → {CurrentState}. Reason: {Reason}", 
                previousState, newState, reason ?? "Not specified");

            // Fire event asynchronously to avoid blocking
            _ = Task.Run(() =>
            {
                try
                {
                    ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(
                        previousState, newState, reason));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error firing ConnectionStateChanged event");
                }
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _ = Task.Run(async () => await StopAsync());
                _monitoringCts?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}