using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.DTOs;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.ViewModels.Map;
using System.ComponentModel.DataAnnotations;

namespace SubExplore.ViewModels.Auth
{
    public partial class LoginViewModel : ObservableValidator
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<LoginViewModel> _logger;

        [ObservableProperty]
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        private string _email = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        private string _password = string.Empty;

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

        // Services for navigation and dialogs
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        public string Title { get; set; } = "Connexion";

        public LoginViewModel(
            IAuthenticationService authenticationService,
            ILogger<LoginViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService)
        {
            _authenticationService = authenticationService;
            _logger = logger;
            _dialogService = dialogService;
            _navigationService = navigationService;
            Title = "Connexion";
        }

        [RelayCommand]
        private async Task Login()
        {
            if (IsLoginInProgress) return;

            try
            {
                IsLoginInProgress = true;
                ClearLoginError();

                // Validate input
                if (!ValidateInput())
                {
                    return;
                }

                _logger.LogInformation("Attempting login for email: {Email}", Email);

                // Perform login
                var result = await _authenticationService.LoginAsync(Email, Password);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Login successful for user: {UserId}", result.User?.Id);
                    
                    await _dialogService.ShowToastAsync("Connexion réussie !");
                    
                    // Navigate to main application
                    await _navigationService.NavigateToAsync<MapViewModel>();
                }
                else
                {
                    _logger.LogWarning("Login failed: {ErrorMessage}", result.ErrorMessage);
                    ShowLoginError(result.ErrorMessage ?? "Erreur de connexion inconnue");

                    // Show validation errors if any
                    if (result.ValidationErrors?.Any() == true)
                    {
                        var errors = string.Join("\n", result.ValidationErrors);
                        await _dialogService.ShowAlertAsync("Erreurs de validation", errors, "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                ShowLoginError("Une erreur inattendue s'est produite. Veuillez réessayer.");
                await _dialogService.ShowAlertAsync("Erreur", "Une erreur inattendue s'est produite.", "OK");
            }
            finally
            {
                IsLoginInProgress = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToRegistration()
        {
            try
            {
                await _navigationService.NavigateToAsync<RegistrationViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to registration");
                await _dialogService.ShowAlertAsync("Erreur", "Impossible d'accéder à la page d'inscription.", "OK");
            }
        }

        [RelayCommand]
        private async Task NavigateToForgotPassword()
        {
            try
            {
                // Show forgot password dialog for now
                var email = await _dialogService.ShowPromptAsync(
                    "Mot de passe oublié",
                    "Entrez votre adresse email pour réinitialiser votre mot de passe :",
                    "Envoyer",
                    "Annuler",
                    Email);

                if (!string.IsNullOrWhiteSpace(email))
                {
                    var success = await _authenticationService.RequestPasswordResetAsync(email);
                    if (success)
                    {
                        await _dialogService.ShowAlertAsync(
                            "Email envoyé",
                            "Si cette adresse email est associée à un compte, vous recevrez un email de réinitialisation.",
                            "OK");
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync(
                            "Erreur",
                            "Impossible d'envoyer l'email de réinitialisation. Veuillez réessayer plus tard.",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password flow");
                await _dialogService.ShowAlertAsync("Erreur", "Une erreur s'est produite.", "OK");
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        private async Task NavigateToMainWithoutLogin()
        {
            try
            {
                _logger.LogInformation("User chose to continue without login");
                await _navigationService.NavigateToAsync<MapViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to main page");
                await _dialogService.ShowAlertAsync("Erreur", "Impossible d'accéder à l'application.", "OK");
            }
        }

        public async Task InitializeAsync(object parameter = null)
        {
            try
            {
                // Don't auto-navigate on initialization - let the user choose to login
                // The authentication check will happen when they click the login button
                
                // Clear any previous state
                ClearLoginError();
                Email = string.Empty;
                Password = string.Empty;
                RememberMe = false;
                IsPasswordVisible = false;
                
                _logger.LogInformation("LoginViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing login page");
            }
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
            else if (Password.Length < 8)
            {
                errors.Add("Le mot de passe doit contenir au moins 8 caractères");
            }

            if (errors.Any())
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
            if (HasLoginError)
            {
                ClearLoginError();
            }
        }

        partial void OnPasswordChanged(string value)
        {
            if (HasLoginError)
            {
                ClearLoginError();
            }
        }
    }
}