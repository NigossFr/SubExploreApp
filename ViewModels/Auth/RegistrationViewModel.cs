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
    public partial class RegistrationViewModel : ObservableValidator
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<RegistrationViewModel> _logger;

        [ObservableProperty]
        [Required(ErrorMessage = "Le prénom est requis")]
        [MinLength(2, ErrorMessage = "Le prénom doit contenir au moins 2 caractères")]
        private string _firstName = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Le nom est requis")]
        [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
        private string _lastName = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères")]
        private string _username = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        private string _email = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        private string _password = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _acceptTermsAndConditions = false;

        [ObservableProperty]
        private bool _isPasswordVisible = false;

        [ObservableProperty]
        private bool _isConfirmPasswordVisible = false;

        [ObservableProperty]
        private bool _isRegistrationInProgress = false;

        [ObservableProperty]
        private string _registrationErrorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasRegistrationError = false;

        [ObservableProperty]
        private bool _isPasswordValid = false;

        [ObservableProperty]
        private bool _isPasswordConfirmationValid = false;

        [ObservableProperty]
        private bool _isEmailValid = false;

        [ObservableProperty]
        private bool _isUsernameValid = false;

        [ObservableProperty]
        private bool _isFirstNameValid = false;

        [ObservableProperty]
        private bool _isLastNameValid = false;

        [ObservableProperty]
        private bool _canRegister = false;

        [ObservableProperty]
        private double _registrationProgress = 0.0;

        // Services for navigation and dialogs
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        public string Title { get; set; } = "Inscription";

        public RegistrationViewModel(
            IAuthenticationService authenticationService,
            ILogger<RegistrationViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService)
        {
            _authenticationService = authenticationService;
            _logger = logger;
            _dialogService = dialogService;
            _navigationService = navigationService;
            Title = "Inscription";
        }

        [RelayCommand]
        private async Task Register()
        {
            if (IsRegistrationInProgress || !CanRegister) return;

            try
            {
                IsRegistrationInProgress = true;
                RegistrationProgress = 0.2;
                ClearRegistrationError();

                // Validate input
                if (!ValidateInput())
                {
                    return;
                }

                _logger.LogInformation("Attempting registration for email: {Email}", Email);
                RegistrationProgress = 0.4;

                // Add slight delay for smooth UX
                await Task.Delay(300);

                // Create registration request
                var registrationRequest = new UserRegistrationRequest
                {
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Username = Username.Trim(),
                    Email = Email.Trim().ToLowerInvariant(),
                    Password = Password,
                    ConfirmPassword = ConfirmPassword,
                    AcceptTermsAndConditions = AcceptTermsAndConditions
                };

                RegistrationProgress = 0.7;
                
                // Perform registration
                var result = await _authenticationService.RegisterAsync(registrationRequest);
                RegistrationProgress = 0.9;

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Registration successful for user: {UserId}", result.User?.Id);
                    RegistrationProgress = 1.0;
                    
                    await _dialogService.ShowAlertAsync(
                        "🎉 Inscription réussie !",
                        "Votre compte a été créé avec succès. Vous êtes maintenant connecté.",
                        "Continuer");
                    
                    // Small delay for progress completion
                    await Task.Delay(200);
                    
                    // Navigate to main application
                    await _navigationService.NavigateToAsync<MapViewModel>();
                }
                else
                {
                    _logger.LogWarning("Registration failed: {ErrorMessage}", result.ErrorMessage);
                    ShowRegistrationError(result.ErrorMessage ?? "Erreur d'inscription inconnue");

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
                _logger.LogError(ex, "Unexpected error during registration");
                ShowRegistrationError("Une erreur inattendue s'est produite. Veuillez réessayer.");
                await _dialogService.ShowAlertAsync("Erreur", "Une erreur inattendue s'est produite.", "OK");
            }
            finally
            {
                IsRegistrationInProgress = false;
                RegistrationProgress = 0.0;
            }
        }

        [RelayCommand]
        private async Task NavigateToLogin()
        {
            try
            {
                await _navigationService.NavigateToAsync<LoginViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to login");
                await _dialogService.ShowAlertAsync("Erreur", "Impossible d'accéder à la page de connexion.", "OK");
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        private void ToggleConfirmPasswordVisibility()
        {
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
        }

        [RelayCommand]
        private async Task ShowTermsAndConditions()
        {
            try
            {
                await _dialogService.ShowAlertAsync(
                    "Conditions d'utilisation",
                    "En utilisant SubExplore, vous acceptez de respecter les règles de sécurité en plongée, " +
                    "de partager des informations exactes sur les spots, et de respecter l'environnement marin. " +
                    "Vos données personnelles sont protégées conformément à notre politique de confidentialité.",
                    "Compris");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing terms and conditions");
            }
        }

        public async Task InitializeAsync(object parameter = null)
        {
            try
            {
                // Check if user is already authenticated
                if (_authenticationService.IsAuthenticated)
                {
                    _logger.LogInformation("User already authenticated, navigating to main page");
                    await _navigationService.NavigateToAsync<MapViewModel>();
                    return;
                }

                // Clear any previous state
                ClearRegistrationError();
                ClearForm();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing registration page");
            }
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            // First name validation
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                errors.Add("Le prénom est requis");
            }
            else if (FirstName.Trim().Length < 2)
            {
                errors.Add("Le prénom doit contenir au moins 2 caractères");
            }

            // Last name validation
            if (string.IsNullOrWhiteSpace(LastName))
            {
                errors.Add("Le nom est requis");
            }
            else if (LastName.Trim().Length < 2)
            {
                errors.Add("Le nom doit contenir au moins 2 caractères");
            }

            // Username validation
            if (string.IsNullOrWhiteSpace(Username))
            {
                errors.Add("Le nom d'utilisateur est requis");
            }
            else if (Username.Trim().Length < 3)
            {
                errors.Add("Le nom d'utilisateur doit contenir au moins 3 caractères");
            }

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

            // Confirm password validation
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                errors.Add("La confirmation du mot de passe est requise");
            }
            else if (Password != ConfirmPassword)
            {
                errors.Add("Les mots de passe ne correspondent pas");
            }

            // Terms and conditions validation
            if (!AcceptTermsAndConditions)
            {
                errors.Add("Vous devez accepter les conditions d'utilisation");
            }

            if (errors.Any())
            {
                ShowRegistrationError(string.Join("\n", errors));
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

        private void UpdateCanRegister()
        {
            CanRegister = IsFirstNameValid && IsLastNameValid && IsUsernameValid && 
                         IsEmailValid && IsPasswordValid && IsPasswordConfirmationValid && 
                         AcceptTermsAndConditions && !IsRegistrationInProgress;
        }

        private void ValidateFirstName()
        {
            IsFirstNameValid = !string.IsNullOrWhiteSpace(FirstName) && FirstName.Trim().Length >= 2;
            UpdateCanRegister();
        }

        private void ValidateLastName()
        {
            IsLastNameValid = !string.IsNullOrWhiteSpace(LastName) && LastName.Trim().Length >= 2;
            UpdateCanRegister();
        }

        private void ValidateUsername()
        {
            IsUsernameValid = !string.IsNullOrWhiteSpace(Username) && Username.Trim().Length >= 3;
            UpdateCanRegister();
        }

        private void ValidateEmailField()
        {
            IsEmailValid = !string.IsNullOrWhiteSpace(Email) && IsValidEmail(Email);
            UpdateCanRegister();
        }

        private void ValidatePassword()
        {
            IsPasswordValid = !string.IsNullOrWhiteSpace(Password) && Password.Length >= 8;
            UpdateCanRegister();
        }

        private void ValidatePasswordConfirmation()
        {
            IsPasswordConfirmationValid = !string.IsNullOrWhiteSpace(ConfirmPassword) && 
                                        Password == ConfirmPassword;
            UpdateCanRegister();
        }

        private void ShowRegistrationError(string message)
        {
            RegistrationErrorMessage = message;
            HasRegistrationError = true;
        }

        private void ClearRegistrationError()
        {
            RegistrationErrorMessage = string.Empty;
            HasRegistrationError = false;
        }

        private void ClearForm()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Username = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            AcceptTermsAndConditions = false;
            IsPasswordVisible = false;
            IsConfirmPasswordVisible = false;
        }

        partial void OnFirstNameChanged(string value)
        {
            ValidateFirstName();
            if (HasRegistrationError)
            {
                ClearRegistrationError();
            }
        }

        partial void OnLastNameChanged(string value)
        {
            ValidateLastName();
            if (HasRegistrationError)
            {
                ClearRegistrationError();
            }
        }

        partial void OnUsernameChanged(string value)
        {
            ValidateUsername();
            if (HasRegistrationError)
            {
                ClearRegistrationError();
            }
        }

        partial void OnEmailChanged(string value)
        {
            ValidateEmailField();
            if (HasRegistrationError)
            {
                ClearRegistrationError();
            }
        }

        partial void OnPasswordChanged(string value)
        {
            ValidatePassword();
            ValidatePasswordConfirmation();
            if (HasRegistrationError)
            {
                ClearRegistrationError();
            }
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            ValidatePasswordConfirmation();
            if (HasRegistrationError)
            {
                ClearRegistrationError();
            }
        }

        partial void OnAcceptTermsAndConditionsChanged(bool value)
        {
            UpdateCanRegister();
        }

        partial void OnIsRegistrationInProgressChanged(bool value)
        {
            UpdateCanRegister();
        }
    }
}