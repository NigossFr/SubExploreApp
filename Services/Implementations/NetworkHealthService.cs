using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Advanced network health monitoring service with quality metrics
    /// </summary>
    public class NetworkHealthService : INetworkHealthService, IDisposable
    {
        private readonly ILogger<NetworkHealthService> _logger;
        private readonly IConnectivityService _connectivityService;
        private readonly HttpClient _httpClient;
        
        private readonly Dictionary<string, string> _healthEndpoints;
        private readonly List<double> _latencyHistory;
        private readonly List<bool> _requestHistory;
        
        private NetworkHealthStatus _currentStatus;
        private CancellationTokenSource? _monitoringCts;
        private Task? _monitoringTask;
        private bool _disposed = false;

        // Configuration
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(10);
        private readonly int _historySize = 20;

        public NetworkHealthStatus CurrentStatus => _currentStatus;

        public event EventHandler<NetworkHealthChangedEventArgs>? HealthStatusChanged;

        public NetworkHealthService(
            ILogger<NetworkHealthService> logger,
            IConnectivityService connectivityService,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            _httpClient.Timeout = _httpTimeout;
            
            _healthEndpoints = new Dictionary<string, string>
            {
                { "Primary", "https://httpbin.org/status/200" },
                { "Secondary", "https://www.google.com" },
                { "Tertiary", "https://www.microsoft.com" }
            };
            
            _latencyHistory = new List<double>();
            _requestHistory = new List<bool>();
            
            _currentStatus = new NetworkHealthStatus
            {
                Level = NetworkHealthLevel.Offline,
                IsConnected = false,
                LastUpdated = DateTime.UtcNow
            };

            // Subscribe to connectivity changes
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_monitoringTask?.IsCompleted == false)
            {
                _logger.LogInformation("🔍 Network health monitoring already running");
                return;
            }

            _logger.LogInformation("🔍 Starting network health monitoring");

            _monitoringCts = new CancellationTokenSource();
            _monitoringTask = MonitorNetworkHealthAsync(_monitoringCts.Token);
            
            // Perform initial health check
            await CheckHealthAsync(cancellationToken);
        }

        public async Task StopMonitoringAsync()
        {
            _logger.LogInformation("⏹️ Stopping network health monitoring");
            
            _monitoringCts?.Cancel();
            
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }

            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
        }

        public async Task<NetworkHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var previousStatus = _currentStatus;
            var newStatus = await PerformHealthCheckAsync(cancellationToken);
            
            if (HasStatusChanged(previousStatus, newStatus))
            {
                _currentStatus = newStatus;
                
                _logger.LogInformation("📊 Network health changed: {PreviousLevel} → {CurrentLevel} (Latency: {Latency}ms)",
                    previousStatus.Level, newStatus.Level, newStatus.LatencyMs);
                
                OnHealthStatusChanged(new NetworkHealthChangedEventArgs(previousStatus, newStatus, "Health check"));
            }
            else
            {
                _currentStatus = newStatus;
            }

            return _currentStatus;
        }

        public Task<NetworkQualityMetrics> GetQualityMetricsAsync(CancellationToken cancellationToken = default)
        {
            var metrics = new NetworkQualityMetrics();
            
            if (_latencyHistory.Count > 0)
            {
                double sum = 0;
                double max = 0;
                double min = double.MaxValue;
                
                foreach (var latency in _latencyHistory)
                {
                    sum += latency;
                    if (latency > max) max = latency;
                    if (latency < min) min = latency;
                }
                
                metrics.AverageLatencyMs = sum / _latencyHistory.Count;
                metrics.MaxLatencyMs = max;
                metrics.MinLatencyMs = min == double.MaxValue ? 0 : min;
            }

            if (_requestHistory.Count > 0)
            {
                int successCount = 0;
                foreach (var success in _requestHistory)
                {
                    if (success) successCount++;
                }
                
                metrics.TotalRequests = _requestHistory.Count;
                metrics.FailedRequests = _requestHistory.Count - successCount;
                metrics.SuccessRate = (double)successCount / _requestHistory.Count * 100;
                metrics.PacketLossRate = (double)metrics.FailedRequests / metrics.TotalRequests * 100;
            }

            metrics.MonitoringDuration = TimeSpan.FromMinutes(_requestHistory.Count * 0.5); // Approximate
            metrics.StartTime = DateTime.UtcNow - metrics.MonitoringDuration;
            metrics.LastMeasurement = DateTime.UtcNow;

            return Task.FromResult(metrics);
        }

        public void RegisterHealthEndpoint(string name, string url)
        {
            _healthEndpoints[name] = url;
            _logger.LogInformation("🔗 Registered health endpoint: {Name} → {Url}", name, url);
        }

        public NetworkRecommendations GetNetworkRecommendations()
        {
            var recommendations = new NetworkRecommendations();
            
            switch (_currentStatus.Level)
            {
                case NetworkHealthLevel.Offline:
                    recommendations.ShouldUseOfflineMode = true;
                    recommendations.ShouldShowConnectivityWarning = true;
                    recommendations.UserMessage = "No internet connection. Using offline mode.";
                    break;

                case NetworkHealthLevel.Critical:
                    recommendations.ShouldReduceImageQuality = true;
                    recommendations.ShouldDelayNonCriticalOperations = true;
                    recommendations.ShouldShowConnectivityWarning = true;
                    recommendations.UserMessage = "Poor connection quality. Some features may be slow.";
                    recommendations.RecommendedRetryDelay = TimeSpan.FromSeconds(10);
                    break;

                case NetworkHealthLevel.Poor:
                    recommendations.ShouldReduceImageQuality = true;
                    recommendations.ShouldDelayNonCriticalOperations = true;
                    recommendations.UserMessage = "Slow connection detected. Optimizing for better performance.";
                    recommendations.RecommendedRetryDelay = TimeSpan.FromSeconds(5);
                    break;

                case NetworkHealthLevel.Fair:
                    recommendations.ShouldReduceImageQuality = _currentStatus.IsOnCellular;
                    recommendations.RecommendedRetryDelay = TimeSpan.FromSeconds(3);
                    if (_currentStatus.IsOnCellular)
                    {
                        recommendations.UserMessage = "Using mobile data. Image quality reduced to save data.";
                    }
                    break;

                case NetworkHealthLevel.Good:
                case NetworkHealthLevel.Excellent:
                    // No special recommendations for good connections
                    recommendations.RecommendedRetryDelay = TimeSpan.FromSeconds(1);
                    break;
            }

            return recommendations;
        }

        private async Task MonitorNetworkHealthAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔍 Network health monitoring started");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await CheckHealthAsync(cancellationToken);
                    await Task.Delay(_monitoringInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🔍 Network health monitoring cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in network health monitoring");
            }
        }

        private async Task<NetworkHealthStatus> PerformHealthCheckAsync(CancellationToken cancellationToken)
        {
            var status = new NetworkHealthStatus
            {
                LastUpdated = DateTime.UtcNow,
                IsConnected = _connectivityService.IsConnected,
                IsOnCellular = _connectivityService.IsUsingCellularNetwork,
                IsOnWiFi = !_connectivityService.IsUsingCellularNetwork && _connectivityService.IsConnected,
                ConnectionType = _connectivityService.NetworkAccess.ToString()
            };

            if (!status.IsConnected)
            {
                status.Level = NetworkHealthLevel.Offline;
                status.Issue = "No internet connection";
                status.Recommendation = "Check your network settings and try again";
                return status;
            }

            // Perform latency test
            var latency = await MeasureLatencyAsync(cancellationToken);
            status.LatencyMs = latency;

            // Perform HTTP connectivity test
            var httpSuccess = await TestHttpConnectivityAsync(cancellationToken);
            
            // Update history
            UpdateHistory(latency, httpSuccess);
            
            // Calculate packet loss
            status.PacketLossPercent = CalculatePacketLoss();

            // Determine health level
            status.Level = DetermineHealthLevel(latency, status.PacketLossPercent, httpSuccess);
            
            // Set recommendations based on level
            SetStatusRecommendations(status);

            return status;
        }

        private async Task<double> MeasureLatencyAsync(CancellationToken cancellationToken)
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 5000); // Google DNS

                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Ping test failed: {Message}", ex.Message);
            }

            return double.MaxValue; // Indicates failure
        }

        private async Task<bool> TestHttpConnectivityAsync(CancellationToken cancellationToken)
        {
            foreach (var endpoint in _healthEndpoints)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(endpoint.Value, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return true; // At least one endpoint is reachable
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("🔗 Health check failed for {Name}: {Message}", endpoint.Key, ex.Message);
                }
            }

            return false; // All endpoints failed
        }

        private void UpdateHistory(double latency, bool httpSuccess)
        {
            // Update latency history (only for successful pings)
            if (latency != double.MaxValue)
            {
                _latencyHistory.Add(latency);
                if (_latencyHistory.Count > _historySize)
                {
                    _latencyHistory.RemoveAt(0);
                }
            }

            // Update request success history
            _requestHistory.Add(httpSuccess);
            if (_requestHistory.Count > _historySize)
            {
                _requestHistory.RemoveAt(0);
            }
        }

        private double CalculatePacketLoss()
        {
            if (_requestHistory.Count == 0) return 0;
            
            int failureCount = 0;
            foreach (var success in _requestHistory)
            {
                if (!success) failureCount++;
            }

            return (double)failureCount / _requestHistory.Count * 100;
        }

        private static NetworkHealthLevel DetermineHealthLevel(double latency, double packetLoss, bool httpSuccess)
        {
            if (!httpSuccess)
                return NetworkHealthLevel.Offline;

            if (latency == double.MaxValue || packetLoss > 10)
                return NetworkHealthLevel.Critical;

            if (latency > 500 || packetLoss > 3)
                return NetworkHealthLevel.Poor;

            if (latency > 200 || packetLoss > 1)
                return NetworkHealthLevel.Fair;

            if (latency > 100)
                return NetworkHealthLevel.Good;

            return NetworkHealthLevel.Excellent;
        }

        private static void SetStatusRecommendations(NetworkHealthStatus status)
        {
            switch (status.Level)
            {
                case NetworkHealthLevel.Offline:
                    status.Issue = "No internet connectivity";
                    status.Recommendation = "Check your network connection";
                    break;
                case NetworkHealthLevel.Critical:
                    status.Issue = "Very poor connection quality";
                    status.Recommendation = "Consider switching networks or try again later";
                    break;
                case NetworkHealthLevel.Poor:
                    status.Issue = "Slow connection detected";
                    status.Recommendation = "Some features may load slowly";
                    break;
                case NetworkHealthLevel.Fair:
                    status.Issue = status.IsOnCellular ? "Using mobile data" : "Moderate connection quality";
                    status.Recommendation = status.IsOnCellular ? "Consider using WiFi for better experience" : null;
                    break;
                default:
                    status.Issue = null;
                    status.Recommendation = null;
                    break;
            }
        }

        private static bool HasStatusChanged(NetworkHealthStatus previous, NetworkHealthStatus current)
        {
            return previous.Level != current.Level ||
                   previous.IsConnected != current.IsConnected ||
                   previous.IsOnCellular != current.IsOnCellular ||
                   Math.Abs(previous.LatencyMs - current.LatencyMs) > 50; // Significant latency change
        }

        private void OnConnectivityChanged(object? sender, SubExplore.Services.Interfaces.ConnectivityChangedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckHealthAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error handling connectivity change");
                }
            });
        }

        protected virtual void OnHealthStatusChanged(NetworkHealthChangedEventArgs e)
        {
            HealthStatusChanged?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await StopMonitoringAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error stopping network monitoring during disposal");
                }
            });

            _monitoringCts?.Dispose();
            _disposed = true;
        }
    }
}