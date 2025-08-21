using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SubExplore.ViewModels.Auth
{
    /// <summary>
    /// ViewModel simplifié pour la connexion utilisant ISimpleAuthenticationService
    /// </summary>
    public partial class SimpleLoginViewModel : ObservableValidator
    {
        private readonly ISimpleAuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<SimpleLoginViewModel> _logger;

        [ObservableProperty]
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        private string _email = "admin@subexplore.com";

        [ObservableProperty]
        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        private string _password = "Admin123!";

        [ObservableProperty]
        private bool _rememberMe = false;

        [ObservableProperty]
        private bool _isPasswordVisible = false;

        [ObservableProperty]
        private bool _isLoginInProgress = false;

        [ObservableProperty]
        private string _loginErrorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasLoginError = false;

        public string Title { get; set; } = "Connexion SubExplore";

        public SimpleLoginViewModel(
            ISimpleAuthenticationService authenticationService,
            IDialogService dialogService,
            INavigationService navigationService,
            ILogger<SimpleLoginViewModel> logger)
        {
            _authenticationService = authenticationService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _logger = logger;
        }

        [RelayCommand]
        private async Task Login()
        {
            if (IsLoginInProgress) return;

            try
            {
                IsLoginInProgress = true;
                ClearLoginError();

                _logger.LogInformation("🔐 Tentative de connexion pour: {Email}", Email);

                // Valider les entrées
                if (!ValidateInput())
                {
                    return;
                }

                // Connexion via Supabase
                var result = await _authenticationService.LoginAsync(Email, Password);

                if (result)
                {
                    _logger.LogInformation("✅ Connexion réussie pour: {Email}", Email);
                    
                    await _dialogService.ShowToastAsync("🎉 Connexion réussie !");
                    
                    // Navigation vers l'application principale
                    _navigationService.SwitchToShellNavigation();
                }
                else
                {
                    _logger.LogWarning("❌ Échec de connexion pour: {Email}", Email);
                    ShowLoginError("Email ou mot de passe incorrect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la connexion");
                ShowLoginError($"Erreur de connexion: {ex.Message}");
            }
            finally
            {
                IsLoginInProgress = false;
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                await _dialogService.ShowAlertAsync("Email requis", "Veuillez saisir votre email d'abord", "OK");
                return;
            }

            await _dialogService.ShowAlertAsync("Réinitialisation", 
                "Fonctionnalité de réinitialisation du mot de passe non encore implémentée avec Supabase", "OK");
        }

        [RelayCommand]
        private async Task Register()
        {
            await _dialogService.ShowAlertAsync("Inscription", 
                "Fonctionnalité d'inscription non encore implémentée", "OK");
        }

        [RelayCommand]
        private async Task TestMode()
        {
            _logger.LogInformation("🧪 Mode test - Navigation directe vers AppShell");
            _navigationService.SwitchToShellNavigation();
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            // Email validation
            if (string.IsNullOrWhiteSpace(Email))
            {
                errors.Add("L'email est requis");
            }
            else if (!IsValidEmail(Email))
            {
                errors.Add("Format d'email invalide");
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(Password))
            {
                errors.Add("Le mot de passe est requis");
            }
            else if (Password.Length < 6)
            {
                errors.Add("Le mot de passe doit contenir au moins 6 caractères");
            }

            if (errors.Count > 0)
            {
                ShowLoginError(string.Join("\n", errors));
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var emailAttribute = new EmailAddressAttribute();
                return emailAttribute.IsValid(email);
            }
            catch
            {
                return false;
            }
        }

        private void ShowLoginError(string message)
        {
            LoginErrorMessage = message;
            HasLoginError = true;
        }

        private void ClearLoginError()
        {
            LoginErrorMessage = string.Empty;
            HasLoginError = false;
        }

        partial void OnEmailChanged(string value)
        {
            ValidateProperty(value, nameof(Email));
        }

        partial void OnPasswordChanged(string value)
        {
            ValidateProperty(value, nameof(Password));
        }
    }
}