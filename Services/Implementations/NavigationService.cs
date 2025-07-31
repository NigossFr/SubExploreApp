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
        private INavigationGuardService? _navigationGuard;
        private IDialogService? _dialogService;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Starting navigation to {typeof(TViewModel).Name}");
                
                // Check navigation permissions first
                if (NavigationGuard != null)
                {
                    var canNavigate = await NavigationGuard.CanNavigateToAsync<TViewModel>();
                    if (!canNavigate)
                    {
                        var message = NavigationGuard.GetAccessDeniedMessage(typeof(TViewModel));
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Access denied - {message}");
                        
                        if (DialogService != null)
                        {
                            await DialogService.ShowAlertAsync("Access Denied", message, "OK");
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
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Parameter: {parameter}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Application.Current: {Application.Current != null}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: MainPage type: {Application.Current?.MainPage?.GetType().Name}");
                
                // Use Shell navigation if available (with fallback on failure)
                if (Application.Current?.MainPage is Shell)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Using Shell navigation");
                    var route = GetRouteForViewModel<TViewModel>();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Route found: {route}");
                    
                    if (!string.IsNullOrEmpty(route))
                    {
                        try
                        {
                            // Pass parameters through query parameters for Shell navigation
                            if (parameter != null)
                            {
                                var queryParams = BuildQueryParameters(parameter);
                                var fullRoute = string.IsNullOrEmpty(queryParams) ? route : $"{route}?{queryParams}";
                                
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigationService: Navigating to {fullRoute}");
                                await Shell.Current.GoToAsync(fullRoute);
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Shell navigation completed successfully");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigationService: Navigating to {route} (no parameters)");
                                await Shell.Current.GoToAsync(route);
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Shell navigation completed successfully");
                            }
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
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Not using Shell navigation (MainPage is not Shell)");
                }
                
                // Fallback to traditional navigation
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Using traditional navigation fallback");
                var page = await CreateAndInitializePage<TViewModel>(parameter);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Page created successfully");

                if (Application.Current?.MainPage is NavigationPage navigationPage)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Pushing to existing NavigationPage");
                    await navigationPage.PushAsync(page);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Traditional navigation completed successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Creating new NavigationPage");
                    // Create new navigation page
                    Application.Current.MainPage = new NavigationPage(page);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: New NavigationPage set as MainPage");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync (Type): Starting navigation to {viewModelType.Name}");
                
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
                System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: Starting navigation back");
                
                if (Application.Current.MainPage is Shell)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: Using Shell navigation back");
                    await Shell.Current.GoToAsync("..");
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: Shell navigation back completed");
                }
                else if (Application.Current.MainPage is NavigationPage navigationPage)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: Using NavigationPage pop");
                    if (navigationPage.Navigation.NavigationStack.Count > 1)
                    {
                        await navigationPage.PopAsync();
                        System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: NavigationPage pop completed");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: Cannot go back - already at root page");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoBackAsync: Unknown navigation type, attempting to go to home");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Starting for {typeof(TViewModel).Name}");
                
                if (_serviceProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] CreateAndInitializePage: _serviceProvider is null");
                    throw new InvalidOperationException("ServiceProvider is null");
                }

                var viewModel = _serviceProvider.GetService<TViewModel>();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: ViewModel resolved: {viewModel != null}");
                
                if (viewModel == null)
                    throw new InvalidOperationException($"Impossible de résoudre le ViewModel {typeof(TViewModel).Name}");

                // Recherche de la page correspondante avec mapping de namespace
                var viewModelTypeName = typeof(TViewModel).Name;
                var viewTypeName = viewModelTypeName.Replace("ViewModel", "Page");
                var viewTypeFullName = GetViewTypeFullName(viewTypeName);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigationService: Looking for view type: {viewTypeFullName}");
                
                var viewType = Assembly.GetExecutingAssembly().GetType(viewTypeFullName);
                if (viewType == null)
                    throw new InvalidOperationException($"Type de vue non trouvé: {viewTypeFullName}");

                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigationService: Found view type, creating instance via DI");
                var page = _serviceProvider.GetService(viewType) as Page;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Page created: {page != null}");
                
                if (page == null)
                    throw new InvalidOperationException($"Impossible de créer une instance de {viewTypeFullName}");

                System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Setting BindingContext");
                page.BindingContext = viewModel;

                // Check if ViewModel has InitializeAsync method
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Looking for InitializeAsync method");
                var initMethod = viewModel.GetType().GetMethod("InitializeAsync", new[] { typeof(object) });
                if (initMethod != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Calling InitializeAsync with parameter");
                    await (Task)initMethod.Invoke(viewModel, new[] { parameter });
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: InitializeAsync completed");
                }
                else
                {
                    // Try method without parameters
                    var initMethodNoParam = viewModel.GetType().GetMethod("InitializeAsync", Type.EmptyTypes);
                    if (initMethodNoParam != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Calling InitializeAsync without parameter");
                        await (Task)initMethodNoParam.Invoke(viewModel, null);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: InitializeAsync completed");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: No InitializeAsync method found");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] CreateAndInitializePage: Returning page successfully");
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

        private string GetRouteForViewModel<TViewModel>()
        {
            var viewModelName = typeof(TViewModel).Name;
            
            // Map ViewModels to their corresponding routes with Shell prefix
            return viewModelName switch
            {
                "MapViewModel" => "///map",
                "FavoriteSpotsViewModel" => "///favorites",
                "AddSpotViewModel" => "///addspot",
                "SpotDetailsViewModel" => "///spotdetails",
                "MySpotsViewModel" => "///myspots",
                "UserProfileViewModel" => "///userprofile",
                "UserPreferencesViewModel" => "///userpreferences",
                "UserStatsViewModel" => "///userstats",
                "SpotValidationViewModel" => "///spotvalidation",
                _ => "///" + ConvertViewModelNameToRoute(viewModelName)
            };
        }
        
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
                "UserPreferencesPage" => "SubExplore.Views.Profile.UserPreferencesPage",
                "UserStatsPage" => "SubExplore.Views.Profile.UserStatsPage",
                "SpotValidationPage" => "SubExplore.Views.Admin.SpotValidationPage",
                "LoginPage" => "SubExplore.Views.Auth.LoginPage",
                "RegistrationPage" => "SubExplore.Views.Auth.RegistrationPage",
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
                if (parameterType.IsPrimitive || parameterType == typeof(string) || parameterType == typeof(decimal))
                {
                    var encodedValue = System.Web.HttpUtility.UrlEncode(parameter.ToString());
                    queryParams.Add($"id={encodedValue}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BuildQueryParameters: Simple type {parameterType.Name} -> id={encodedValue}");
                }
                else
                {
                    // Special handling for SpotNavigationParameter to ensure SpotId is passed correctly
                    if (parameter is SubExplore.Models.Navigation.SpotNavigationParameter spotParam)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BuildQueryParameters: SpotNavigationParameter detected with SpotId={spotParam.SpotId}");
                        
                        // Shell Navigation has issues with complex parameters, so we use multiple simple parameters
                        // This ensures all parameters are preserved
                        if (spotParam.SpotId > 0)
                        {
                            queryParams.Add($"spotid={spotParam.SpotId}");
                            queryParams.Add($"mode=edit");
                            if (!string.IsNullOrEmpty(spotParam.SpotName))
                            {
                                queryParams.Add($"spotname={System.Web.HttpUtility.UrlEncode(spotParam.SpotName)}");
                            }
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Added SpotId parameters: spotid={spotParam.SpotId}, mode=edit");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BuildQueryParameters result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] BuildQueryParameters failed: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Navigate to home page (map page)
        /// </summary>
        public async Task GoToHomeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] GoToHomeAsync: Navigating to home page");
                
                if (Application.Current.MainPage is Shell)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoToHomeAsync: Using Shell navigation to map");
                    await Shell.Current.GoToAsync("///map");
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoToHomeAsync: Shell navigation to map completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] GoToHomeAsync: Using ViewModel navigation to MapViewModel");
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
                System.Diagnostics.Debug.WriteLine("[DEBUG] NavigationService: Switching to Shell navigation");
                Application.Current.MainPage = new AppShell();
                System.Diagnostics.Debug.WriteLine("[DEBUG] NavigationService: ✓ AppShell set as MainPage successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigationService SwitchToShellNavigation failed: {ex.Message}");
                throw;
            }
        }
    }
}
