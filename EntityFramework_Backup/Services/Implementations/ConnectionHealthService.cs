using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Connection health monitoring service implementation
    /// </summary>
    public class ConnectionHealthService : IConnectionHealthService, IDisposable
    {
        private readonly ILogger<ConnectionHealthService> _logger;
        private readonly SubExploreDbContext _dbContext;
        private readonly ISupabaseApiService _supabaseApiService;
        
        private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<HealthCheckResult>>> _healthChecks;
        private readonly Timer? _monitoringTimer;
        
        private HealthStatus _currentStatus = HealthStatus.Unknown;
        private DateTime? _lastHealthCheck;
        private HealthReport? _lastHealthReport;
        private bool _isMonitoring = false;
        private bool _disposed = false;

        // Configuration
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _healthCheckTimeout = TimeSpan.FromSeconds(30);

        public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

        public ConnectionHealthService(
            ILogger<ConnectionHealthService> logger,
            SubExploreDbContext dbContext,
            ISupabaseApiService supabaseApiService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _supabaseApiService = supabaseApiService;
            _healthChecks = new ConcurrentDictionary<string, Func<CancellationToken, Task<HealthCheckResult>>>();

            // Register built-in health checks
            RegisterBuiltInHealthChecks();
        }

        public HealthStatus CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                if (_currentStatus != value)
                {
                    var previousStatus = _currentStatus;
                    _currentStatus = value;
                    
                    _logger.LogInformation("🔄 Health status changed: {PreviousStatus} → {CurrentStatus}", 
                        previousStatus, value);
                    
                    HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs(
                        previousStatus, 
                        value, 
                        "Health monitoring detected status change"));
                }
            }
        }

        public DateTime? LastHealthCheck => _lastHealthCheck;

        /// <summary>
        /// Starts continuous health monitoring
        /// </summary>
        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("⚠️ Health monitoring is already running");
                return;
            }

            _logger.LogInformation("🚀 Starting health monitoring with {Interval}s interval", 
                _monitoringInterval.TotalSeconds);

            _isMonitoring = true;

            // Perform initial health check
            await CheckHealthAsync();

            // Start background monitoring
            _ = Task.Run(async () => await MonitoringLoopAsync(cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Stops health monitoring
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
            {
                return;
            }

            _logger.LogInformation("⏹️ Stopping health monitoring");
            _isMonitoring = false;

            // Allow some time for current operations to complete
            await Task.Delay(1000);
        }

        /// <summary>
        /// Performs an immediate comprehensive health check
        /// </summary>
        public async Task<HealthReport> CheckHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new Dictionary<string, HealthCheckResult>();

            _logger.LogDebug("🔍 Starting comprehensive health check");

            // Execute all registered health checks
            var tasks = _healthChecks.Select(kvp => ExecuteHealthCheckAsync(kvp.Key, kvp.Value));
            var healthCheckResults = await Task.WhenAll(tasks);

            foreach (var result in healthCheckResults)
            {
                results[result.Name] = result;
            }

            stopwatch.Stop();

            // Determine overall status
            var overallStatus = DetermineOverallStatus(results.Values);
            var report = new HealthReport(overallStatus, stopwatch.Elapsed, results);

            // Update current status and cache report
            CurrentStatus = overallStatus;
            _lastHealthCheck = DateTime.UtcNow;
            _lastHealthReport = report;

            _logger.LogInformation("🏥 Health check completed: {Summary}", report.GetSummary());

            return report;
        }

        /// <summary>
        /// Gets the current cached health report
        /// </summary>
        public HealthReport GetCurrentHealth()
        {
            return _lastHealthReport ?? new HealthReport(
                HealthStatus.Unknown,
                TimeSpan.Zero,
                new Dictionary<string, HealthCheckResult>());
        }

        /// <summary>
        /// Registers a custom health check
        /// </summary>
        public void RegisterHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> healthCheck)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(name));

            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            _healthChecks.AddOrUpdate(name, healthCheck, (_, __) => healthCheck);
            _logger.LogDebug("➕ Registered health check: {Name}", name);
        }

        /// <summary>
        /// Unregisters a custom health check
        /// </summary>
        public void UnregisterHealthCheck(string name)
        {
            if (_healthChecks.TryRemove(name, out _))
            {
                _logger.LogDebug("➖ Unregistered health check: {Name}", name);
            }
        }

        #region Private Methods

        /// <summary>
        /// Background monitoring loop
        /// </summary>
        private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
        {
            while (_isMonitoring && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_monitoringInterval, cancellationToken);
                    
                    if (_isMonitoring && !cancellationToken.IsCancellationRequested)
                    {
                        await CheckHealthAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in health monitoring loop");
                    
                    // Continue monitoring despite errors
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }

            _logger.LogInformation("⏹️ Health monitoring loop stopped");
        }

        /// <summary>
        /// Executes a single health check with timeout
        /// </summary>
        private async Task<HealthCheckResult> ExecuteHealthCheckAsync(
            string name, 
            Func<CancellationToken, Task<HealthCheckResult>> healthCheck)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var cts = new CancellationTokenSource(_healthCheckTimeout);
                var result = await healthCheck(cts.Token);
                stopwatch.Stop();

                _logger.LogDebug("✅ Health check '{Name}' completed: {Status} ({Duration}ms)", 
                    name, result.Status, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var timeoutResult = new HealthCheckResult(
                    name,
                    HealthStatus.Unhealthy,
                    stopwatch.Elapsed,
                    $"Health check timed out after {_healthCheckTimeout.TotalSeconds} seconds");

                _logger.LogWarning("⏰ Health check '{Name}' timed out after {Timeout}s", 
                    name, _healthCheckTimeout.TotalSeconds);

                return timeoutResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var errorResult = new HealthCheckResult(
                    name,
                    HealthStatus.Unhealthy,
                    stopwatch.Elapsed,
                    $"Health check failed: {ex.Message}",
                    ex);

                _logger.LogError(ex, "❌ Health check '{Name}' failed", name);

                return errorResult;
            }
        }

        /// <summary>
        /// Determines overall status from individual health check results
        /// </summary>
        private static HealthStatus DetermineOverallStatus(IEnumerable<HealthCheckResult> results)
        {
            if (!results.Any())
                return HealthStatus.Unknown;

            var statuses = results.Select(r => r.Status).ToList();

            // If any are unhealthy, overall is unhealthy
            if (statuses.Contains(HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            // If any are degraded, overall is degraded
            if (statuses.Contains(HealthStatus.Degraded))
                return HealthStatus.Degraded;

            // If any are warning, overall is warning
            if (statuses.Contains(HealthStatus.Warning))
                return HealthStatus.Warning;

            // If all are healthy, overall is healthy
            if (statuses.All(s => s == HealthStatus.Healthy))
                return HealthStatus.Healthy;

            // Default to unknown for mixed states
            return HealthStatus.Unknown;
        }

        /// <summary>
        /// Registers built-in health checks for database and API connectivity
        /// </summary>
        private void RegisterBuiltInHealthChecks()
        {
            // Database connectivity check
            RegisterHealthCheck("database", async (cancellationToken) =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                    stopwatch.Stop();

                    if (canConnect)
                    {
                        // Additional check - try a simple query
                        var count = await _dbContext.SpotTypes.CountAsync(cancellationToken);
                        
                        return new HealthCheckResult(
                            "database",
                            HealthStatus.Healthy,
                            stopwatch.Elapsed,
                            $"Database connected successfully. {count} spot types found.");
                    }
                    else
                    {
                        return new HealthCheckResult(
                            "database",
                            HealthStatus.Unhealthy,
                            stopwatch.Elapsed,
                            "Cannot connect to database");
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    return new HealthCheckResult(
                        "database",
                        HealthStatus.Unhealthy,
                        stopwatch.Elapsed,
                        $"Database health check failed: {ex.Message}",
                        ex);
                }
            });

            // Supabase API connectivity check
            RegisterHealthCheck("supabase-api", async (cancellationToken) =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    var isHealthy = await _supabaseApiService.TestConnectionAsync();
                    stopwatch.Stop();

                    return new HealthCheckResult(
                        "supabase-api",
                        isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                        stopwatch.Elapsed,
                        isHealthy ? "Supabase API connection successful" : "Supabase API connection failed");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    return new HealthCheckResult(
                        "supabase-api",
                        HealthStatus.Unhealthy,
                        stopwatch.Elapsed,
                        $"Supabase API health check failed: {ex.Message}",
                        ex);
                }
            });

            // Memory usage check
            RegisterHealthCheck("memory", async (cancellationToken) =>
            {
                var stopwatch = Stopwatch.StartNew();
                await Task.Delay(1, cancellationToken); // Minimal async operation
                
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / (1024 * 1024);
                stopwatch.Stop();

                var status = memoryMB switch
                {
                    < 100 => HealthStatus.Healthy,
                    < 200 => HealthStatus.Warning,
                    < 500 => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };

                return new HealthCheckResult(
                    "memory",
                    status,
                    stopwatch.Elapsed,
                    $"Memory usage: {memoryMB} MB");
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _isMonitoring = false;
                _monitoringTimer?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}