using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Health status levels
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>Service is healthy and operational</summary>
        Healthy,
        
        /// <summary>Service is operational but showing warning signs</summary>
        Warning,
        
        /// <summary>Service is degraded but partially functional</summary>
        Degraded,
        
        /// <summary>Service is unhealthy and not functioning</summary>
        Unhealthy,
        
        /// <summary>Health status is unknown</summary>
        Unknown
    }

    /// <summary>
    /// Connection health monitoring service
    /// </summary>
    public interface IConnectionHealthService
    {
        /// <summary>
        /// Current overall health status
        /// </summary>
        HealthStatus CurrentStatus { get; }

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        DateTime? LastHealthCheck { get; }

        /// <summary>
        /// Event fired when health status changes
        /// </summary>
        event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        /// <summary>
        /// Starts continuous health monitoring
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops health monitoring
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Performs an immediate health check
        /// </summary>
        /// <returns>Comprehensive health report</returns>
        Task<HealthReport> CheckHealthAsync();

        /// <summary>
        /// Gets the current health report
        /// </summary>
        /// <returns>Current health status</returns>
        HealthReport GetCurrentHealth();

        /// <summary>
        /// Registers a custom health check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="healthCheck">Health check function</param>
        void RegisterHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> healthCheck);

        /// <summary>
        /// Unregisters a custom health check
        /// </summary>
        /// <param name="name">Name of the health check to remove</param>
        void UnregisterHealthCheck(string name);
    }

    /// <summary>
    /// Health status change event arguments
    /// </summary>
    public class HealthStatusChangedEventArgs : EventArgs
    {
        public HealthStatus PreviousStatus { get; }
        public HealthStatus CurrentStatus { get; }
        public DateTime Timestamp { get; }
        public string? Reason { get; }

        public HealthStatusChangedEventArgs(
            HealthStatus previousStatus, 
            HealthStatus currentStatus, 
            string? reason = null)
        {
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
            Timestamp = DateTime.UtcNow;
            Reason = reason;
        }
    }

    /// <summary>
    /// Individual health check result
    /// </summary>
    public class HealthCheckResult
    {
        public string Name { get; }
        public HealthStatus Status { get; }
        public string? Description { get; }
        public TimeSpan Duration { get; }
        public Exception? Exception { get; }
        public DateTime Timestamp { get; }

        public HealthCheckResult(
            string name,
            HealthStatus status,
            TimeSpan duration,
            string? description = null,
            Exception? exception = null)
        {
            Name = name;
            Status = status;
            Duration = duration;
            Description = description;
            Exception = exception;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Comprehensive health report
    /// </summary>
    public class HealthReport
    {
        public HealthStatus OverallStatus { get; }
        public DateTime Timestamp { get; }
        public TimeSpan TotalDuration { get; }
        public IDictionary<string, HealthCheckResult> Results { get; }

        public HealthReport(
            HealthStatus overallStatus,
            TimeSpan totalDuration,
            IDictionary<string, HealthCheckResult> results)
        {
            OverallStatus = overallStatus;
            Timestamp = DateTime.UtcNow;
            TotalDuration = totalDuration;
            Results = results ?? new Dictionary<string, HealthCheckResult>();
        }

        /// <summary>
        /// Gets a summary string of the health report
        /// </summary>
        public string GetSummary()
        {
            var statusIcon = OverallStatus switch
            {
                HealthStatus.Healthy => "✅",
                HealthStatus.Warning => "⚠️",
                HealthStatus.Degraded => "🟡",
                HealthStatus.Unhealthy => "❌",
                _ => "❓"
            };

            var healthyCount = Results.Values.Count(r => r.Status == HealthStatus.Healthy);
            var totalCount = Results.Count;

            return $"{statusIcon} Overall: {OverallStatus} | {healthyCount}/{totalCount} checks passed | Duration: {TotalDuration.TotalMilliseconds:F0}ms";
        }
    }
}