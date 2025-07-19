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
            // Use Shell navigation if available
            if (Application.Current.MainPage is Shell)
            {
                var route = GetRouteForViewModel<TViewModel>();
                if (!string.IsNullOrEmpty(route))
                {
                    // Pass parameters through query parameters for Shell navigation
                    if (parameter != null)
                    {
                        var queryParams = BuildQueryParameters(parameter);
                        var fullRoute = string.IsNullOrEmpty(queryParams) ? route : $"{route}?{queryParams}";
                        
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] NavigationService: Navigating to {fullRoute}");
                        await Shell.Current.GoToAsync(fullRoute);
                    }
                    else
                    {
                        await Shell.Current.GoToAsync(route);
                    }
                    return;
                }
            }
            
            // Fallback to traditional navigation
            var page = await CreateAndInitializePage<TViewModel>(parameter);

            if (Application.Current.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PushAsync(page);
            }
            else
            {
                // Create new navigation page
                Application.Current.MainPage = new NavigationPage(page);
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
            var viewModel = _serviceProvider.GetService<TViewModel>();
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
            if (page == null)
                throw new InvalidOperationException($"Impossible de créer une instance de {viewTypeFullName}");

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
            }

            return page;
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

                return string.Join("&", queryParams);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] BuildQueryParameters failed: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
