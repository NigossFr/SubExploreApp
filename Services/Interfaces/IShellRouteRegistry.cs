using SubExplore.Navigation;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for managing Shell route registrations and navigation
    /// </summary>
    public interface IShellRouteRegistry
    {
        /// <summary>
        /// Discover and register all routes from ViewModels with ShellRoute attributes
        /// </summary>
        void DiscoverAndRegisterRoutes();

        /// <summary>
        /// Get route for a specific ViewModel type
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel type</typeparam>
        /// <returns>Shell route string, or null if not found</returns>
        string? GetRouteForViewModel<TViewModel>();

        /// <summary>
        /// Get route for a specific ViewModel type by Type
        /// </summary>
        /// <param name="viewModelType">ViewModel type</param>
        /// <returns>Shell route string, or null if not found</returns>
        string? GetRouteForViewModel(Type viewModelType);

        /// <summary>
        /// Get friendly name for a route
        /// </summary>
        /// <param name="route">Route string</param>
        /// <returns>Friendly name, or the route if not found</returns>
        string GetFriendlyName(string route);

        /// <summary>
        /// Get View type for a ViewModel type
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel type</typeparam>
        /// <returns>View type, or null if not found</returns>
        Type? GetViewTypeForViewModel<TViewModel>();

        /// <summary>
        /// Get all registered routes
        /// </summary>
        /// <returns>Collection of route information</returns>
        IReadOnlyCollection<ShellRouteInfo> GetAllRoutes();

        /// <summary>
        /// Get Shell route from ViewModel name for backward compatibility
        /// </summary>
        /// <param name="viewModelName">ViewModel class name</param>
        /// <returns>Shell route without prefix</returns>
        string GetShellRouteFromViewModel(string viewModelName);
    }
}