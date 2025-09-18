using SubExplore.Navigation;
using SubExplore.Services.Interfaces;
using System.Diagnostics;
using System.Reflection;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Centralized registry for Shell routes, eliminating hardcoded route mappings
    /// </summary>
    public class ShellRouteRegistry : IShellRouteRegistry
    {
        private readonly Dictionary<Type, ShellRouteInfo> _viewModelRoutes = new();
        private readonly Dictionary<string, ShellRouteInfo> _routeMap = new();

        public void DiscoverAndRegisterRoutes()
        {
            try
            {
                Debug.WriteLine("[ShellRouteRegistry] Discovering routes from ViewModels...");

                var assembly = Assembly.GetExecutingAssembly();
                var viewModelTypes = assembly.GetTypes()
                    .Where(t => t.Name.EndsWith("ViewModel") && 
                               t.GetCustomAttribute<ShellRouteAttribute>() != null)
                    .ToList();

                Debug.WriteLine($"[ShellRouteRegistry] Found {viewModelTypes.Count} ViewModels with routes");

                foreach (var viewModelType in viewModelTypes)
                {
                    var routeAttr = viewModelType.GetCustomAttribute<ShellRouteAttribute>();
                    if (routeAttr != null)
                    {
                        RegisterRoute(viewModelType, routeAttr);
                    }
                }

                // Register fallback routes for ViewModels without attributes (backward compatibility)
                RegisterFallbackRoutes();

                Debug.WriteLine($"[ShellRouteRegistry] Total registered routes: {_viewModelRoutes.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellRouteRegistry] Error discovering routes: {ex.Message}");
                throw;
            }
        }

        public string? GetRouteForViewModel<TViewModel>()
        {
            return GetRouteForViewModel(typeof(TViewModel));
        }

        public string? GetRouteForViewModel(Type viewModelType)
        {
            if (_viewModelRoutes.TryGetValue(viewModelType, out var routeInfo))
            {
                return $"///{routeInfo.Route}";
            }

            Debug.WriteLine($"[ShellRouteRegistry] No route found for {viewModelType.Name}");
            return null;
        }

        public string GetFriendlyName(string route)
        {
            // Remove route prefixes for lookup
            var cleanRoute = route.Replace("///", "").Replace("//", "").Replace("/", "");
            
            var routeInfo = _routeMap.Values.FirstOrDefault(r => r.Route == cleanRoute);
            return routeInfo?.FriendlyName ?? route;
        }

        public Type? GetViewTypeForViewModel<TViewModel>()
        {
            if (_viewModelRoutes.TryGetValue(typeof(TViewModel), out var routeInfo))
            {
                return routeInfo.ViewType;
            }
            return null;
        }

        public IReadOnlyCollection<ShellRouteInfo> GetAllRoutes()
        {
            return _viewModelRoutes.Values.ToList().AsReadOnly();
        }

        public string GetShellRouteFromViewModel(string viewModelName)
        {
            var routeInfo = _viewModelRoutes.Values
                .FirstOrDefault(r => r.ViewModelType.Name == viewModelName);
            
            return routeInfo?.Route ?? ConvertViewModelNameToRoute(viewModelName);
        }

        private void RegisterRoute(Type viewModelType, ShellRouteAttribute routeAttr)
        {
            try
            {
                var viewType = FindCorrespondingViewType(viewModelType);
                if (viewType == null)
                {
                    Debug.WriteLine($"[ShellRouteRegistry] ⚠️ No corresponding view found for {viewModelType.Name}");
                    return;
                }

                var routeInfo = new ShellRouteInfo
                {
                    ViewModelType = viewModelType,
                    ViewType = viewType,
                    Route = routeAttr.Route,
                    FriendlyName = routeAttr.FriendlyName ?? GetDefaultFriendlyName(viewModelType),
                    Icon = routeAttr.Icon,
                    IsVisible = routeAttr.IsVisible
                };

                _viewModelRoutes[viewModelType] = routeInfo;
                _routeMap[routeAttr.Route] = routeInfo;

                Debug.WriteLine($"[ShellRouteRegistry] ✓ Registered route '{routeAttr.Route}' for {viewModelType.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShellRouteRegistry] Error registering route for {viewModelType.Name}: {ex.Message}");
            }
        }

        private void RegisterFallbackRoutes()
        {
            // Register common ViewModels that might not have attributes yet
            var fallbackMappings = new Dictionary<string, (string route, string friendlyName, string? icon)>
            {
                { "MapViewModel", ("map", "🗺️ Carte", "dotnet_bot.png") },
                { "FavoriteSpotsViewModel", ("favorites", "⭐ Favoris", null) },
                { "AddSpotViewModel", ("addspot", "➕ Ajouter un Spot", null) },
                { "SpotDetailsViewModel", ("spotdetails", "📖 Détails du Spot", null) },
                { "MySpotsViewModel", ("myspots", "📍 Mes Spots", null) },
                { "UserProfileViewModel", ("userprofile", "👤 Profil", null) },
                { "UserStatsViewModel", ("userstats", "📊 Statistiques", null) },
                { "SpotValidationViewModel", ("spotvalidation", "⚖️ Validation", null) },
                { "DatabaseTestViewModel", ("databasetest", "🛠️ Test BD", null) },
                { "AboutViewModel", ("about", "ℹ️ À Propos", null) }
            };

            var assembly = Assembly.GetExecutingAssembly();

            foreach (var (viewModelName, (route, friendlyName, icon)) in fallbackMappings)
            {
                var viewModelType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == viewModelName);

                if (viewModelType != null && !_viewModelRoutes.ContainsKey(viewModelType))
                {
                    var viewType = FindCorrespondingViewType(viewModelType);
                    if (viewType != null)
                    {
                        var routeInfo = new ShellRouteInfo
                        {
                            ViewModelType = viewModelType,
                            ViewType = viewType,
                            Route = route,
                            FriendlyName = friendlyName,
                            Icon = icon,
                            IsVisible = true
                        };

                        _viewModelRoutes[viewModelType] = routeInfo;
                        _routeMap[route] = routeInfo;

                        Debug.WriteLine($"[ShellRouteRegistry] ✓ Registered fallback route '{route}' for {viewModelName}");
                    }
                }
            }
        }

        private Type? FindCorrespondingViewType(Type viewModelType)
        {
            var viewModelName = viewModelType.Name;
            var viewName = viewModelName.Replace("ViewModel", "Page");

            // Special case mappings for ViewModels that don't follow standard naming
            var specialMappings = new Dictionary<string, string>
            {
                { "RefactoredAddSpotViewModel", "AddSpotPage" },
                { "SpotTypeSelectionViewModel", "SpotTypeSelectionPage" }
            };

            if (specialMappings.TryGetValue(viewModelName, out var specialViewName))
            {
                viewName = specialViewName;
            }

            // Try different namespace patterns
            var assembly = Assembly.GetExecutingAssembly();
            
            // First try specific namespace mappings
            var namespaceMappings = new Dictionary<string, string>
            {
                { "MapPage", "SubExplore.Views.Map" },
                { "FavoriteSpotsPage", "SubExplore.Views.Favorites" },
                { "FavoritesPage", "SubExplore.Views.Favorites" },
                { "AddSpotPage", "SubExplore.Views.Spots" },
                { "SpotTypeSelectionPage", "SubExplore.Views.Spots" },
                { "SpotDetailsPage", "SubExplore.Views.Spots" },
                { "MySpotsPage", "SubExplore.Views.Spots" },
                { "UserProfilePage", "SubExplore.Views.Profile" },
                { "UserStatsPage", "SubExplore.Views.Profile" },
                { "SpotValidationPage", "SubExplore.Views.Admin" },
                { "DatabaseTestPage", "SubExplore.Views.Settings" },
                { "AboutPage", "SubExplore.Views.Settings" },
                { "LoginPage", "SubExplore.Views.Auth" },
                { "RegistrationPage", "SubExplore.Views.Auth" },
                { "CompleteLoginPage", "SubExplore.Views.Auth" },
                { "OrganizationDetailsPage", "SubExplore.Views.Organizations" },
                { "BusinessDetailsPage", "SubExplore.Views.Businesses" }
            };

            if (namespaceMappings.TryGetValue(viewName, out var specificNamespace))
            {
                var fullTypeName = $"{specificNamespace}.{viewName}";
                var viewType = assembly.GetType(fullTypeName);
                if (viewType != null) return viewType;
            }

            // Fallback: search all types
            return assembly.GetTypes()
                .FirstOrDefault(t => t.Name == viewName && t.IsSubclassOf(typeof(Microsoft.Maui.Controls.Page)));
        }

        private string GetDefaultFriendlyName(Type viewModelType)
        {
            var name = viewModelType.Name.Replace("ViewModel", "");
            
            // Add spaces before capital letters
            var friendlyName = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();
            
            return friendlyName;
        }

        private string ConvertViewModelNameToRoute(string viewModelName)
        {
            return viewModelName
                .Replace("ViewModel", "")
                .ToLower();
        }
    }
}