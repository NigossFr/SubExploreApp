namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service health status enumeration
    /// </summary>
    public enum ServiceHealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        NotRegistered
    }

    /// <summary>
    /// Health check result for a service
    /// </summary>
    public class ServiceHealthResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public Type ServiceType { get; set; } = null!;
        public ServiceHealthStatus Status { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service for checking the health of critical application services
    /// </summary>
    public interface IServiceHealthChecker
    {
        /// <summary>
        /// Perform health checks on all critical services
        /// </summary>
        /// <returns>Health check results for all services</returns>
        Task<IReadOnlyCollection<ServiceHealthResult>> CheckAllServicesAsync();

        /// <summary>
        /// Check health of a specific service
        /// </summary>
        /// <typeparam name="TService">Service interface type</typeparam>
        /// <returns>Health check result</returns>
        Task<ServiceHealthResult> CheckServiceAsync<TService>() where TService : class;

        /// <summary>
        /// Validate that all critical services are registered and healthy
        /// </summary>
        /// <returns>True if all critical services are healthy</returns>
        Task<bool> ValidateStartupServicesAsync();

        /// <summary>
        /// Get summary of service health status
        /// </summary>
        /// <returns>Overall health status summary</returns>
        Task<(ServiceHealthStatus overallStatus, int healthyCount, int totalCount)> GetHealthSummaryAsync();
    }
}