using SubExplore.Services.Implementations;
using SubExplore.Services.Interfaces;

namespace SubExplore.Extensions
{
    /// <summary>
    /// Extension methods for service registration and health checking
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register all SubExplore services with health checking
        /// </summary>
        public static IServiceCollection AddSubExploreServices(this IServiceCollection services)
        {
            // Register new services
            services.AddSingleton<IShellIconService, ShellIconService>();
            services.AddSingleton<IShellRouteRegistry, ShellRouteRegistry>();
            services.AddTransient<IServiceHealthChecker, ServiceHealthChecker>();

            // Initialize route registry after registration
            services.AddSingleton<IServiceInitializer>(provider =>
            {
                var routeRegistry = provider.GetRequiredService<IShellRouteRegistry>();
                return new RouteRegistryInitializer(routeRegistry);
            });

            return services;
        }

        /// <summary>
        /// Perform startup health checks and initialize services
        /// </summary>
        public static async Task<bool> ValidateAndInitializeServicesAsync(this IServiceProvider serviceProvider)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ServiceExtensions] Starting service validation and initialization...");

                // Initialize route registry first
                var initializers = serviceProvider.GetServices<IServiceInitializer>();
                foreach (var initializer in initializers)
                {
                    await initializer.InitializeAsync();
                }

                // Perform health checks
                var healthChecker = serviceProvider.GetService<IServiceHealthChecker>();
                if (healthChecker != null)
                {
                    var isHealthy = await healthChecker.ValidateStartupServicesAsync();
                    if (!isHealthy)
                    {
                        System.Diagnostics.Debug.WriteLine("[ServiceExtensions] ❌ Service health validation failed");
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ServiceExtensions] ⚠️ ServiceHealthChecker not available, skipping health checks");
                }

                System.Diagnostics.Debug.WriteLine("[ServiceExtensions] ✅ Service validation and initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ServiceExtensions] ❌ Service initialization failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Interface for service initialization
    /// </summary>
    public interface IServiceInitializer
    {
        Task InitializeAsync();
    }

    /// <summary>
    /// Route registry initializer
    /// </summary>
    public class RouteRegistryInitializer : IServiceInitializer
    {
        private readonly IShellRouteRegistry _routeRegistry;

        public RouteRegistryInitializer(IShellRouteRegistry routeRegistry)
        {
            _routeRegistry = routeRegistry;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine("[RouteRegistryInitializer] Discovering and registering routes...");
                _routeRegistry.DiscoverAndRegisterRoutes();
            });
        }
    }
}