using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Reflection;
using System.Web;

namespace SubExplore.Services.Implementations
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShellRouteRegistry _routeRegistry;
        private INavigationGuardService? _navigationGuard;
        private IDialogService? _dialogService;
        private readonly Stack<string> _navigationHistory = new();
        private string? _currentRoute;

        public NavigationService(IServiceProvider serviceProvider, IShellRouteRegistry routeRegistry)
        {
            _serviceProvider = serviceProvider;
            _routeRegistry = routeRegistry;
        }

        // Lazy initialization to avoid circular dependencies
        private INavigationGuardService NavigationGuard => 
            _navigationGuard ??= _serviceProvider.GetService<INavigationGuardService>();
        
        private IDialogService DialogService => 
            _dialogService ??= _serviceProvider.GetService<IDialogService>();

        public async Task NavigateToAsync<TViewModel>(object parameter = null)
        {
            try
            {
                var targetRoute = typeof(TViewModel).Name;
                
                // Add to navigation history for better back navigation
                if (!string.IsNullOrEmpty(_currentRoute) && _currentRoute != targetRoute)
                {
                    _navigationHistory.Push(_currentRoute);
                }
                
                // Check navigation permissions first
                if (NavigationGuard != null)
                {
                    var canNavigate = await NavigationGuard.CanNavigateToAsync<TViewModel>();
                    if (!canNavigate)
                    {
                        var message = NavigationGuard.GetAccessDeniedMessage(typeof(TViewModel));
                        
                        if (DialogService != null)
                        {
                            await DialogService.ShowAlertAsync("Access Denied", message, "D'accord");
                        }
                        
                        // Optionally redirect to appropriate page
                        var redirectType = NavigationGuard.GetRoleBasedRedirect();
                        if (redirectType != null && redirectType != typeof(TViewModel))
                        {
                            await NavigateToAsync(redirectType, parameter);
                        }
                        
                        return;
                    }
                }
                
                
                // Use Shell navigation if available (with fallback on failure)
                if (Application.Current?.MainPage is Shell)
                {
                    var route = _routeRegistry.GetRouteForViewModel<TViewModel>();
                    
                    if (!string.IsNullOrEmpty(route))
                    {
                        try
                        {
                            // Pass parameters through query parameters for Shell navigation
                            if (parameter != null)
                            {
                                var queryParams = BuildQueryParameters(parameter);
                                var fullRoute = string.IsNullOrEmpty(queryParams) ? route : $"{route}?{queryParams}";
                                
                                await Shell.Current.GoToAsync(fullRoute, true); // Animate transitions
                            }
                            else
                            {
                                await Shell.Current.GoToAsync(route, true); // Animate transitions
                            }
                            
                            // Update current route for breadcrumb tracking
                            _currentRoute = targetRoute;
                            UpdateNavigationBreadcrumb(targetRoute);
                            return;
                        }
                        catch (Exception shellEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Shell navigation failed: {shellEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"[WARNING] Falling back to traditional navigation");
                            // Continue to fallback navigation below
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARNING] NavigateToAsync: No route found for {typeof(TViewModel).Name}, falling back to traditional navigation");
                    }
                }
                else
                {
                }
                
                // Fallback to traditional navigation
                var page = await CreateAndInitializePage<TViewModel>(parameter);

                if (Application.Current?.MainPage is NavigationPage navigationPage)
                {
                    await navigationPage.PushAsync(page);
                }
                else
                {
                    // Create new navigation page
                    Application.Current.MainPage = new NavigationPage(page);
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAsync COM Exception (possible debugger disconnect): {comEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] COM Exception HRESULT: 0x{comEx.HResult:X8}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] This might be a Mono debugger disconnection issue");
                throw new InvalidOperationException("Navigation failed due to system error. Please try again.", comEx);
            }
            catch (Exception ex) when (ex.GetType().FullName.Contains("VMDisconnectedException"))
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAsync VM Disconnect Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Mono debugger has been disconnected during navigation");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception type: {ex.GetType().FullName}");
                throw new InvalidOperationException("Navigation failed due to debugger disconnection. Please restart the application.", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAsync failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception type: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Navigate to a page by type (used for redirects from navigation guard)
        /// </summary>
        public async Task NavigateToAsync(Type viewModelType, object parameter = null)
        {
            try
            {
                
                // Use reflection to call the generic method
                var method = typeof(NavigationService).GetMethod(nameof(NavigateToAsync), new[] { typeof(object) });
                var genericMethod = method?.MakeGenericMethod(viewModelType);
                
                if (genericMethod != null)
                {
                    var task = (Task)genericMethod.Invoke(this, new[] { parameter });
                    await task;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAsync (Type): Could not find generic method for {viewModelType.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAsync (Type): {ex.Message}");
                throw;
            }
        }

        public async Task NavigateToModalAsync<TViewModel>(object parameter = null)
        {
            var page = await CreateAndInitializePage<TViewModel>(parameter);
            await Application.Current.MainPage.Navigation.PushModalAsync(page);
        }

        public async Task GoBackAsync()
        {
            try
            {
                
                if (Application.Current.MainPage is Shell)
                {
                    
                    // Use navigation history for smarter back navigation
                    if (_navigationHistory.Count > 0)
                    {
                        var previousRoute = _navigationHistory.Pop();
                        _currentRoute = previousRoute;
                        await Shell.Current.GoToAsync($"///{GetShellRouteFromViewModel(previousRoute)}", true);
                        UpdateNavigationBreadcrumb(previousRoute);
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("..", true);
                    }
                }
                else if (Application.Current.MainPage is NavigationPage navigationPage)
                {
                    if (navigationPage.Navigation.NavigationStack.Count > 1)
                    {
                        await navigationPage.PopAsync();
                    }
                    else
                    {
                    }
                }
                else
                {
                    await GoToHomeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GoBackAsync failed: {ex.Message}");
                // Fallback: try to go to home page
                try
                {
                    await GoToHomeAsync();
                }
                catch (Exception homeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] GoToHomeAsync fallback also failed: {homeEx.Message}");
                    throw;
                }
            }
        }

        public async Task CloseModalAsync()
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        public async Task InitializeAsync<TViewModel>()
        {
            var page = await CreateAndInitializePage<TViewModel>();
            Application.Current.MainPage = new NavigationPage(page);
        }

        private async Task<Page> CreateAndInitializePage<TViewModel>(object parameter = null)
        {
            try
            {
                
                if (_serviceProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] CreateAndInitializePage: _serviceProvider is null");
                    throw new InvalidOperationException("ServiceProvider is null");
                }

                var viewModel = _serviceProvider.GetService<TViewModel>();
                
                if (viewModel == null)
                    throw new InvalidOperationException($"Impossible de résoudre le ViewModel {typeof(TViewModel).Name}");

                // Use route registry to find the corresponding view type
                var viewType = _routeRegistry.GetViewTypeForViewModel<TViewModel>();
                if (viewType == null)
                {
                    // Fallback to old method for backward compatibility
                    var viewModelTypeName = typeof(TViewModel).Name;
                    var viewTypeName = viewModelTypeName.Replace("ViewModel", "Page");
                    var viewTypeFullName = GetViewTypeFullName(viewTypeName);
                    
                    viewType = Assembly.GetExecutingAssembly().GetType(viewTypeFullName);
                }

                if (viewType == null)
                    throw new InvalidOperationException($"Type de vue non trouvé pour {typeof(TViewModel).Name}");

                var page = _serviceProvider.GetService(viewType) as Page;
                
                if (page == null)
                    throw new InvalidOperationException($"Impossible de créer une instance de {viewType.Name}");

                page.BindingContext = viewModel;

                // Check if ViewModel has InitializeAsync method
                var initMethod = viewModel.GetType().GetMethod("InitializeAsync", new[] { typeof(object) });
                if (initMethod != null)
                {
                    await (Task)initMethod.Invoke(viewModel, new[] { parameter });
                }
                else
                {
                    // Try method without parameters
                    var initMethodNoParam = viewModel.GetType().GetMethod("InitializeAsync", Type.EmptyTypes);
                    if (initMethodNoParam != null)
                    {
                        await (Task)initMethodNoParam.Invoke(viewModel, null);
                    }
                    else
                    {
                    }
                }

                return page;
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CreateAndInitializePage COM Exception: {comEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] COM Exception HRESULT: 0x{comEx.HResult:X8}");
                throw new InvalidOperationException("Page creation failed due to system error. Please try again.", comEx);
            }
            catch (Exception ex) when (ex.GetType().FullName.Contains("VMDisconnectedException"))
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CreateAndInitializePage VM Disconnect Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception type: {ex.GetType().FullName}");
                throw new InvalidOperationException("Page creation failed due to debugger disconnection. Please restart the application.", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CreateAndInitializePage failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception type: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        // Removed - now using IShellRouteRegistry.GetRouteForViewModel
        
        private string ConvertViewModelNameToRoute(string viewModelName)
        {
            // Convert ViewModel name to route (e.g., UserProfileViewModel -> userprofile)
            return viewModelName
                .Replace("ViewModel", "")
                .ToLower();
        }

        private string GetViewTypeFullName(string viewTypeName)
        {
            // Map specific page types to their correct namespaces
            return viewTypeName switch
            {
                "MapPage" => "SubExplore.Views.Map.MapPage",
                "FavoriteSpotsPage" => "SubExplore.Views.Favorites.FavoriteSpotsPage",
                "AddSpotPage" => "SubExplore.Views.Spots.AddSpotPage",
                "SpotDetailsPage" => "SubExplore.Views.Spots.SpotDetailsPage",
                "MySpotsPage" => "SubExplore.Views.Spots.MySpotsPage",
                "SettingsPage" => "SubExplore.Views.Settings.SettingsPage",
                "DatabaseTestPage" => "SubExplore.Views.Settings.DatabaseTestPage",
                "UserProfilePage" => "SubExplore.Views.Profile.UserProfilePage",
                "UserStatsPage" => "SubExplore.Views.Profile.UserStatsPage",
                "SpotValidationPage" => "SubExplore.Views.Admin.SpotValidationPage",
                "LoginPage" => "SubExplore.Views.Auth.LoginPage",
                "RegistrationPage" => "SubExplore.Views.Auth.RegistrationPage",
                "OrganizationDetailsPage" => "SubExplore.Views.Organizations.OrganizationDetailsPage",
                "BusinessDetailsPage" => "SubExplore.Views.Businesses.BusinessDetailsPage",
                _ => $"SubExplore.Views.{viewTypeName}" // Default fallback
            };
        }

        private string BuildQueryParameters(object parameter)
        {
            if (parameter == null)
                return string.Empty;

            try
            {
                var queryParams = new List<string>();
                var parameterType = parameter.GetType();

                // Handle simple value types (int, string, etc.) by treating them as "id" parameter
                // Special case for Guid: use "spotId" parameter name for SpotDetailsViewModel compatibility
                if (parameterType == typeof(Guid))
                {
                    var encodedValue = System.Web.HttpUtility.UrlEncode(parameter.ToString());
                    queryParams.Add($"spotId={encodedValue}");
                }
                else if (parameterType.IsPrimitive || parameterType == typeof(string) || parameterType == typeof(decimal))
                {
                    var encodedValue = System.Web.HttpUtility.UrlEncode(parameter.ToString());
                    queryParams.Add($"id={encodedValue}");
                }
                else
                {
                    // Special handling for SpotNavigationParameter to ensure SpotId is passed correctly
                    if (parameter is SubExplore.Models.Navigation.SpotNavigationParameter spotParam)
                    {
                        
                        // Shell Navigation has issues with complex parameters, so we use multiple simple parameters
                        // This ensures all parameters are preserved
                        if (spotParam.SpotId != Guid.Empty)
                        {
                            queryParams.Add($"spotid={spotParam.SpotId}");
                            queryParams.Add($"mode=edit");
                            if (!string.IsNullOrEmpty(spotParam.SpotName))
                            {
                                queryParams.Add($"spotname={System.Web.HttpUtility.UrlEncode(spotParam.SpotName)}");
                            }
                        }
                        
                        // Add latitude and longitude
                        if (spotParam.Latitude.HasValue)
                        {
                            queryParams.Add($"latitude={spotParam.Latitude.Value}");
                        }
                        if (spotParam.Longitude.HasValue)
                        {
                            queryParams.Add($"longitude={spotParam.Longitude.Value}");
                        }
                    }
                    else
                    {
                        // Handle anonymous objects and regular objects
                        foreach (var property in parameterType.GetProperties())
                        {
                            var value = property.GetValue(parameter);
                            if (value != null)
                            {
                                var encodedValue = System.Web.HttpUtility.UrlEncode(value.ToString());
                                queryParams.Add($"{property.Name.ToLower()}={encodedValue}");
                            }
                        }
                    }
                }

                var result = string.Join("&", queryParams);
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] BuildQueryParameters failed: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Navigate to home page (map page) and clear navigation history
        /// </summary>
        public async Task GoToHomeAsync()
        {
            try
            {
                
                // Clear navigation history when going home
                _navigationHistory.Clear();
                _currentRoute = "MapViewModel";
                
                if (Application.Current.MainPage is Shell)
                {
                    await Shell.Current.GoToAsync("///map", true);
                    UpdateNavigationBreadcrumb("MapViewModel");
                }
                else
                {
                    await NavigateToAsync<ViewModels.Map.MapViewModel>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GoToHomeAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Switch to Shell navigation (used after successful authentication)
        /// </summary>
        public void SwitchToShellNavigation()
        {
            try
            {
                Application.Current.MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigationService SwitchToShellNavigation failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get current navigation path for breadcrumb display
        /// </summary>
        public string GetCurrentNavigationPath()
        {
            return _currentRoute ?? "Accueil";
        }
        
        /// <summary>
        /// Get navigation history count for UI indicators
        /// </summary>
        public int GetNavigationHistoryCount()
        {
            return _navigationHistory.Count;
        }
        
        /// <summary>
        /// Clear navigation history (useful for logout or app reset)
        /// </summary>
        public void ClearNavigationHistory()
        {
            _navigationHistory.Clear();
            _currentRoute = null;
        }
        
        private void UpdateNavigationBreadcrumb(string routeName)
        {
            try
            {
                var breadcrumb = GetFriendlyRouteName(routeName);
                
                // Update Shell flyout header if possible
                if (Application.Current.MainPage is Shell shell)
                {
                    // This would require a custom Shell implementation to update breadcrumb
                    // For now, we log the breadcrumb information
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpdateNavigationBreadcrumb failed: {ex.Message}");
            }
        }
        
        private string GetFriendlyRouteName(string routeName)
        {
            return _routeRegistry.GetFriendlyName(routeName);
        }
        
        private string GetShellRouteFromViewModel(string viewModelName)
        {
            return _routeRegistry.GetShellRouteFromViewModel(viewModelName);
        }
    }
}
