using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Reflection;

namespace SubExplore.Services.Implementations
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task NavigateToAsync<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
        {
            var page = await CreateAndInitializePage<TViewModel>(parameter);

            if (Application.Current.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PushAsync(page);
            }
            else if (Application.Current.MainPage is Shell shell)
            {
                // Si vous utilisez Shell, adaptez en fonction de votre structure de navigation
                var route = GetRouteForViewModel<TViewModel>();
                if (!string.IsNullOrEmpty(route))
                {
                    if (parameter != null)
                    {
                        await Shell.Current.GoToAsync($"{route}?id={parameter}");
                    }
                    else
                    {
                        await Shell.Current.GoToAsync(route);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Aucune route trouvée pour le ViewModel {typeof(TViewModel).Name}");
                }
            }
            else
            {
                // Si vous n'utilisez ni NavigationPage ni Shell
                Application.Current.MainPage = new NavigationPage(page);
            }
        }

        public async Task NavigateToModalAsync<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
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

        public async Task InitializeAsync<TViewModel>() where TViewModel : ViewModelBase
        {
            var page = await CreateAndInitializePage<TViewModel>();
            Application.Current.MainPage = new NavigationPage(page);
        }

        private async Task<Page> CreateAndInitializePage<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
        {
            var viewModel = _serviceProvider.GetService<TViewModel>();
            if (viewModel == null)
                throw new InvalidOperationException($"Impossible de résoudre le ViewModel {typeof(TViewModel).Name}");

            // Recherche de la page correspondante
            var viewModelTypeName = typeof(TViewModel).Name;
            var viewTypeName = viewModelTypeName.Replace("ViewModel", "Page");
            var viewTypeFullName = $"SubExplore.Views.{viewTypeName}";

            var viewType = Assembly.GetExecutingAssembly().GetType(viewTypeFullName);
            if (viewType == null)
                throw new InvalidOperationException($"Type de vue non trouvé: {viewTypeFullName}");

            var page = Activator.CreateInstance(viewType) as Page;
            if (page == null)
                throw new InvalidOperationException($"Impossible de créer une instance de {viewTypeFullName}");

            page.BindingContext = viewModel;

            if (parameter != null)
            {
                await viewModel.InitializeAsync(parameter);
            }
            else
            {
                await viewModel.InitializeAsync();
            }

            return page;
        }

        private string GetRouteForViewModel<TViewModel>() where TViewModel : ViewModelBase
        {
            // Cette méthode doit retourner la route Shell correspondant au ViewModel
            // Si vous utilisez Shell, vous devrez probablement ajuster cette implémentation
            // en fonction de votre structure de navigation

            var viewModelName = typeof(TViewModel).Name.Replace("ViewModel", "");

            // Exemple de conversion de nom : MapViewModel -> //map
            return $"//{viewModelName.ToLower()}";
        }
    }
}
