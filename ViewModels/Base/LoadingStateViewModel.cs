using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using System.ComponentModel;

namespace SubExplore.ViewModels.Base
{
    public abstract partial class LoadingStateViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _loadingMessage = "Chargement...";

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private string _errorTitle = "Erreur";

        [ObservableProperty]
        private string _errorMessage = "Une erreur s'est produite";

        [ObservableProperty]
        private bool _canRetry = true;

        [ObservableProperty]
        private bool _isEmpty;

        [ObservableProperty]
        private string _emptyIcon = "🏊";

        [ObservableProperty]
        private string _emptyTitle = "Aucun élément trouvé";

        [ObservableProperty]
        private string _emptyMessage = "Aucun élément à afficher pour le moment";

        [ObservableProperty]
        private string _emptyActionText = "Actualiser";

        [ObservableProperty]
        private bool _hasEmptyAction = false;

        protected LoadingStateViewModel(IDialogService dialogService = null, INavigationService navigationService = null)
            : base(dialogService, navigationService)
        {
        }

        [RelayCommand]
        protected virtual async Task Retry()
        {
            await ExecuteRetryOperation();
        }

        [RelayCommand]
        protected virtual async Task EmptyAction()
        {
            await ExecuteEmptyAction();
        }

        protected virtual async Task ExecuteRetryOperation()
        {
            // Override in derived classes
            await Task.CompletedTask;
        }

        protected virtual async Task ExecuteEmptyAction()
        {
            // Override in derived classes
            await Task.CompletedTask;
        }

        protected async Task ExecuteWithLoadingState(Func<Task> operation, string loadingMessage = null)
        {
            try
            {
                ClearAllStates();
                IsLoading = true;
                LoadingMessage = loadingMessage ?? "Chargement...";

                await operation();

                IsLoading = false;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                ShowError("Erreur", ex.Message);
            }
        }

        protected async Task<T> ExecuteWithLoadingState<T>(Func<Task<T>> operation, string loadingMessage = null)
        {
            try
            {
                ClearAllStates();
                IsLoading = true;
                LoadingMessage = loadingMessage ?? "Chargement...";

                var result = await operation();

                IsLoading = false;
                return result;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                ShowError("Erreur", ex.Message);
                return default(T);
            }
        }

        protected void ShowError(string title, string message, bool canRetry = true)
        {
            ClearAllStates();
            IsError = true;
            ErrorTitle = title;
            ErrorMessage = message;
            CanRetry = canRetry;
        }

        protected void ShowEmpty(string title = null, string message = null, string icon = null, string actionText = null, bool hasAction = false)
        {
            ClearAllStates();
            IsEmpty = true;
            EmptyTitle = title ?? "Aucun élément trouvé";
            EmptyMessage = message ?? "Aucun élément à afficher pour le moment";
            EmptyIcon = icon ?? "🏊";
            EmptyActionText = actionText ?? "Actualiser";
            HasEmptyAction = hasAction;
        }

        protected void ClearAllStates()
        {
            IsLoading = false;
            IsError = false;
            IsEmpty = false;
            ClearError();
        }

        protected void ShowLoadingState(string message = null)
        {
            ClearAllStates();
            IsLoading = true;
            LoadingMessage = message ?? "Chargement...";
        }

        protected void HideLoadingState()
        {
            IsLoading = false;
        }

        protected bool IsAnyStateActive => IsLoading || IsError || IsEmpty;

        protected bool IsContentVisible => !IsAnyStateActive;
    }
}