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

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task NavigateToAsync<TViewModel>(object parameter = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigateToAsync: Starting navigation to {typeof(TViewModel).Name}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] NavigateToAsync failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
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
            if (Application.Current.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PopAsync();
            }
            else if (Application.Current.MainPage is Shell)
            {
                await Shell.Current.GoToAsync("..");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CreateAndInitializePage failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private string GetRouteForViewModel<TViewModel>()
        {
            var viewModelName = typeof(TViewModel).Name;
            
            // Map ViewModels to their corresponding routes
            return viewModelName switch
            {
                "MapViewModel" => "map",
                "AddSpotViewModel" => "addspot",
                "SpotDetailsViewModel" => "spotdetails",
                "UserProfileViewModel" => "userprofile",
                "UserPreferencesViewModel" => "userpreferences",
                "UserStatsViewModel" => "userstats",
                _ => ConvertViewModelNameToRoute(viewModelName)
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
                "AddSpotPage" => "SubExplore.Views.Spots.AddSpotPage",
                "SpotDetailsPage" => "SubExplore.Views.Spots.SpotDetailsPage",
                "SettingsPage" => "SubExplore.Views.Settings.SettingsPage",
                "DatabaseTestPage" => "SubExplore.Views.Settings.DatabaseTestPage",
                "UserProfilePage" => "SubExplore.Views.Profile.UserProfilePage",
                "UserPreferencesPage" => "SubExplore.Views.Profile.UserPreferencesPage",
                "UserStatsPage" => "SubExplore.Views.Profile.UserStatsPage",
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
    }
}
