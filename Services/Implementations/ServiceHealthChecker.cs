using SubExplore.Services.Interfaces;
using System.Diagnostics;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Service health checker for validating critical services during startup
    /// </summary>
    public class ServiceHealthChecker : IServiceHealthChecker
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Define critical services that must be healthy for app startup
        private readonly Type[] _criticalServices = 
        {
            typeof(INavigationService),
            typeof(IShellRouteRegistry),
            typeof(IShellIconService),
            typeof(IDatabaseService),
            typeof(IDialogService)
        };

        public ServiceHealthChecker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<IReadOnlyCollection<ServiceHealthResult>> CheckAllServicesAsync()
        {
            var results = new List<ServiceHealthResult>();

            foreach (var serviceType in _criticalServices)
            {
                try
                {
                    var result = await CheckServiceByTypeAsync(serviceType);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(new ServiceHealthResult
                    {
                        ServiceName = serviceType.Name,
                        ServiceType = serviceType,
                        Status = ServiceHealthStatus.Unhealthy,
                        Message = $"Health check failed: {ex.Message}",
                        Exception = ex
                    });
                }
            }

            return results.AsReadOnly();
        }

        public async Task<ServiceHealthResult> CheckServiceAsync<TService>() where TService : class
        {
            return await CheckServiceByTypeAsync(typeof(TService));
        }

        public async Task<bool> ValidateStartupServicesAsync()
        {
            try
            {
                Debug.WriteLine("[ServiceHealthChecker] Starting startup service validation...");

                var results = await CheckAllServicesAsync();
                var unhealthyServices = results.Where(r => r.Status == ServiceHealthStatus.Unhealthy || r.Status == ServiceHealthStatus.NotRegistered).ToList();

                if (unhealthyServices.Any())
                {
                    Debug.WriteLine($"[ServiceHealthChecker] ❌ Found {unhealthyServices.Count} unhealthy services:");
                    foreach (var service in unhealthyServices)
                    {
                        Debug.WriteLine($"[ServiceHealthChecker] - {service.ServiceName}: {service.Status} - {service.Message}");
                    }
                    return false;
                }

                Debug.WriteLine($"[ServiceHealthChecker] ✅ All {results.Count} critical services are healthy");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ServiceHealthChecker] ❌ Startup validation failed: {ex.Message}");
                return false;
            }
        }

        public async Task<(ServiceHealthStatus overallStatus, int healthyCount, int totalCount)> GetHealthSummaryAsync()
        {
            var results = await CheckAllServicesAsync();
            var healthyCount = results.Count(r => r.Status == ServiceHealthStatus.Healthy);
            var totalCount = results.Count;

            var overallStatus = ServiceHealthStatus.Healthy;
            
            if (results.Any(r => r.Status == ServiceHealthStatus.Unhealthy || r.Status == ServiceHealthStatus.NotRegistered))
            {
                overallStatus = ServiceHealthStatus.Unhealthy;
            }
            else if (results.Any(r => r.Status == ServiceHealthStatus.Degraded))
            {
                overallStatus = ServiceHealthStatus.Degraded;
            }

            return (overallStatus, healthyCount, totalCount);
        }

        private async Task<ServiceHealthResult> CheckServiceByTypeAsync(Type serviceType)
        {
            try
            {
                // Check if service is registered
                var service = _serviceProvider.GetService(serviceType);
                if (service == null)
                {
                    return new ServiceHealthResult
                    {
                        ServiceName = serviceType.Name,
                        ServiceType = serviceType,
                        Status = ServiceHealthStatus.NotRegistered,
                        Message = "Service is not registered in DI container"
                    };
                }

                // Perform basic health check
                var healthStatus = await PerformServiceSpecificHealthCheck(serviceType, service);
                
                return new ServiceHealthResult
                {
                    ServiceName = serviceType.Name,
                    ServiceType = serviceType,
                    Status = healthStatus.status,
                    Message = healthStatus.message
                };
            }
            catch (Exception ex)
            {
                return new ServiceHealthResult
                {
                    ServiceName = serviceType.Name,
                    ServiceType = serviceType,
                    Status = ServiceHealthStatus.Unhealthy,
                    Message = $"Health check exception: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<(ServiceHealthStatus status, string message)> PerformServiceSpecificHealthCheck(Type serviceType, object service)
        {
            // Service-specific health checks
            try
            {
                switch (serviceType.Name)
                {
                    case nameof(INavigationService):
                        return CheckNavigationService(service as INavigationService);
                        
                    case nameof(IShellRouteRegistry):
                        return CheckRouteRegistry(service as IShellRouteRegistry);
                        
                    case nameof(IShellIconService):
                        return CheckIconService(service as IShellIconService);
                        
                    case nameof(IDatabaseService):
                        return await CheckDatabaseService(service as IDatabaseService);
                        
                    case nameof(IDialogService):
                        return CheckDialogService(service as IDialogService);
                        
                    default:
                        return (ServiceHealthStatus.Healthy, "Service is registered and accessible");
                }
            }
            catch (Exception ex)
            {
                return (ServiceHealthStatus.Unhealthy, $"Health check failed: {ex.Message}");
            }
        }

        private (ServiceHealthStatus, string) CheckNavigationService(INavigationService? navigationService)
        {
            if (navigationService == null)
                return (ServiceHealthStatus.NotRegistered, "NavigationService is null");

            // Check if navigation history methods work
            try
            {
                var historyCount = navigationService.GetNavigationHistoryCount();
                return (ServiceHealthStatus.Healthy, $"NavigationService operational, history count: {historyCount}");
            }
            catch (Exception ex)
            {
                return (ServiceHealthStatus.Degraded, $"NavigationService partially functional: {ex.Message}");
            }
        }

        private (ServiceHealthStatus, string) CheckRouteRegistry(IShellRouteRegistry? routeRegistry)
        {
            if (routeRegistry == null)
                return (ServiceHealthStatus.NotRegistered, "RouteRegistry is null");

            try
            {
                var routes = routeRegistry.GetAllRoutes();
                return routes.Count > 0 
                    ? (ServiceHealthStatus.Healthy, $"RouteRegistry operational with {routes.Count} routes")
                    : (ServiceHealthStatus.Degraded, "RouteRegistry has no registered routes");
            }
            catch (Exception ex)
            {
                return (ServiceHealthStatus.Unhealthy, $"RouteRegistry error: {ex.Message}");
            }
        }

        private (ServiceHealthStatus, string) CheckIconService(IShellIconService? iconService)
        {
            if (iconService == null)
                return (ServiceHealthStatus.NotRegistered, "ShellIconService is null");

            try
            {
                var iconSource = iconService.GetPlatformIconSource();
                return iconSource != null
                    ? (ServiceHealthStatus.Healthy, "ShellIconService operational")
                    : (ServiceHealthStatus.Degraded, "ShellIconService cannot create icon source");
            }
            catch (Exception ex)
            {
                return (ServiceHealthStatus.Unhealthy, $"ShellIconService error: {ex.Message}");
            }
        }

        private async Task<(ServiceHealthStatus, string)> CheckDatabaseService(IDatabaseService? databaseService)
        {
            if (databaseService == null)
                return (ServiceHealthStatus.NotRegistered, "DatabaseService is null");

            try
            {
                // Basic database connectivity check
                var isHealthy = await databaseService.TestConnectionAsync();
                return isHealthy
                    ? (ServiceHealthStatus.Healthy, "DatabaseService operational")
                    : (ServiceHealthStatus.Unhealthy, "Database connection failed");
            }
            catch (Exception ex)
            {
                return (ServiceHealthStatus.Unhealthy, $"DatabaseService error: {ex.Message}");
            }
        }

        private (ServiceHealthStatus, string) CheckDialogService(IDialogService? dialogService)
        {
            if (dialogService == null)
                return (ServiceHealthStatus.NotRegistered, "DialogService is null");

            // DialogService is hard to test without showing actual dialogs
            return (ServiceHealthStatus.Healthy, "DialogService registered and accessible");
        }
    }
}