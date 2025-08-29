using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for monitoring and reporting network health status
    /// </summary>
    public interface INetworkHealthService
    {
        /// <summary>
        /// Current network health status
        /// </summary>
        NetworkHealthStatus CurrentStatus { get; }

        /// <summary>
        /// Event fired when network health status changes
        /// </summary>
        event EventHandler<NetworkHealthChangedEventArgs> HealthStatusChanged;

        /// <summary>
        /// Start continuous network health monitoring
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop network health monitoring
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Perform immediate network health check
        /// </summary>
        Task<NetworkHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get network quality metrics
        /// </summary>
        Task<NetworkQualityMetrics> GetQualityMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Register a custom endpoint for health checking
        /// </summary>
        void RegisterHealthEndpoint(string name, string url);

        /// <summary>
        /// Get recommendations based on current network state
        /// </summary>
        NetworkRecommendations GetNetworkRecommendations();
    }

    /// <summary>
    /// Network health status levels
    /// </summary>
    public enum NetworkHealthLevel
    {
        Excellent,  // ≤50ms latency, 0% packet loss
        Good,       // ≤100ms latency, <1% packet loss  
        Fair,       // ≤200ms latency, <3% packet loss
        Poor,       // ≤500ms latency, <10% packet loss
        Critical,   // >500ms latency or >10% packet loss
        Offline     // No connectivity
    }

    /// <summary>
    /// Comprehensive network health status
    /// </summary>
    public class NetworkHealthStatus
    {
        public NetworkHealthLevel Level { get; set; }
        public bool IsConnected { get; set; }
        public bool IsOnWiFi { get; set; }
        public bool IsOnCellular { get; set; }
        public bool IsRoaming { get; set; }
        public double LatencyMs { get; set; }
        public double PacketLossPercent { get; set; }
        public double BandwidthKbps { get; set; }
        public string ConnectionType { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string? Issue { get; set; }
        public string? Recommendation { get; set; }
    }

    /// <summary>
    /// Network quality metrics over time
    /// </summary>
    public class NetworkQualityMetrics
    {
        public double AverageLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double PacketLossRate { get; set; }
        public int TotalRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan MonitoringDuration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastMeasurement { get; set; }
    }

    /// <summary>
    /// Network-based recommendations for the user
    /// </summary>
    public class NetworkRecommendations
    {
        public bool ShouldUseOfflineMode { get; set; }
        public bool ShouldReduceImageQuality { get; set; }
        public bool ShouldDelayNonCriticalOperations { get; set; }
        public bool ShouldShowConnectivityWarning { get; set; }
        public string? UserMessage { get; set; }
        public TimeSpan? RecommendedRetryDelay { get; set; }
    }

    /// <summary>
    /// Event arguments for network health changes
    /// </summary>
    public class NetworkHealthChangedEventArgs : EventArgs
    {
        public NetworkHealthStatus PreviousStatus { get; set; }
        public NetworkHealthStatus CurrentStatus { get; set; }
        public string? ChangeReason { get; set; }

        public NetworkHealthChangedEventArgs(
            NetworkHealthStatus previousStatus, 
            NetworkHealthStatus currentStatus, 
            string? changeReason = null)
        {
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
            ChangeReason = changeReason;
        }
    }
}