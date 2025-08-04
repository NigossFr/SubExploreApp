using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SubExplore.Services.Interfaces;

namespace SubExplore.ViewModels.Base
{
    public abstract partial class ViewModelBase : ObservableObject, IDisposable
    {
        protected readonly IDialogService DialogService;
        protected readonly INavigationService NavigationService;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isEmpty;

        [ObservableProperty]
        private string _loadingMessage = "Chargement...";

        [ObservableProperty]
        private bool _showBackButton;

        [ObservableProperty]
        private string _errorTitle = "Erreur";

        protected ViewModelBase(IDialogService dialogService = null, INavigationService navigationService = null)
        {
            DialogService = dialogService;
            NavigationService = navigationService;
        }

        public virtual Task InitializeAsync(object parameter = null)
        {
            return Task.CompletedTask;
        }

        public virtual Task InitializeAsync(IDictionary<string, object> parameters)
        {
            return Task.CompletedTask;
        }

        protected void ShowError(string message)
        {
            ErrorMessage = message;
            IsError = true;
        }

        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            IsError = false;
        }

        protected async Task ShowAlertAsync(string title, string message, string buttonText = "D'accord")
        {
            if (DialogService != null)
            {
                await DialogService.ShowAlertAsync(title, message, buttonText);
            }
        }

        protected async Task<bool> ShowConfirmationAsync(string title, string message, string okText = "Oui", string cancelText = "Annuler")
        {
            if (DialogService != null)
            {
                return await DialogService.ShowConfirmationAsync(title, message, okText, cancelText);
            }
            return false;
        }

        protected async Task ShowToastAsync(string message, int durationInSeconds = 2)
        {
            if (DialogService != null)
            {
                await DialogService.ShowToastAsync(message, durationInSeconds);
            }
        }

        protected async Task NavigateToAsync<T>(object parameter = null) where T : ViewModelBase
        {
            if (NavigationService != null)
            {
                await NavigationService.NavigateToAsync<T>(parameter);
            }
        }

        protected async Task GoBackAsync()
        {
            if (NavigationService != null)
            {
                await NavigationService.GoBackAsync();
            }
        }

        [RelayCommand]
        protected async Task GoBack()
        {
            await GoBackAsync();
        }


        /// <summary>
        /// Dispose pattern implementation for ViewModels
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            // Override in derived classes for cleanup
        }

        /// <summary>
        /// Public dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}