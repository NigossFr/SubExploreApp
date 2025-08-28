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
        private readonly ISimpleAuthenticationService _simpleAuthenticationService;
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

        [ObservableProperty]
        private bool _isEmailValid = false;

        [ObservableProperty]
        private bool _isPasswordValid = false;

        [ObservableProperty]
        private bool _canLogin = false;

        [ObservableProperty]
        private double _loginProgress = 0.0;

        [ObservableProperty]
        private bool _showAutoLoginOption = false;

        [ObservableProperty]
        private bool _isAutoLoginInProgress = false;

        [ObservableProperty]
        private string _savedUserEmail = string.Empty;

        // Services for navigation and dialogs
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly ISecureSettingsService _secureSettings;

        public string Title { get; set; } = "Connexion";

        public LoginViewModel(
            ISimpleAuthenticationService authenticationService,
            ILogger<LoginViewModel> logger,
            IDialogService dialogService,
            INavigationService navigationService,
            IPasswordResetService passwordResetService,
            ISecureSettingsService secureSettings)
        {
            _simpleAuthenticationService = authenticationService;
            _logger = logger;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _passwordResetService = passwordResetService;
            _secureSettings = secureSettings;
            Title = "Connexion";
        }

        [RelayCommand]
        private async Task Login()
        {
            if (IsLoginInProgress || !CanLogin) return;

            try
            {
                IsLoginInProgress = true;
                LoginProgress = 0.2;
                ClearLoginError();

                // Validate input
                if (!ValidateInput())
                {
                    return;
                }

                _logger.LogInformation("Attempting login for email: {Email}", Email);
                LoginProgress = 0.5;

                // Add slight delay for smooth UX on fast devices
                await Task.Delay(300);

                // Perform login
                var result = await _simpleAuthenticationService.LoginSimpleAsync(Email, Password);
                LoginProgress = 0.8;

                if (result)
                {
                    _logger.LogInformation("Login successful for user: {Email}", Email);
                    LoginProgress = 1.0;
                    
                    // Save credentials if "Remember Me" is checked
                    if (RememberMe)
                    {
                        await SaveRememberedCredentialsAsync();
                    }
                    else
                    {
                        await ClearRememberedCredentialsAsync();
                    }
                    
                    await _dialogService.ShowToastAsync("🎉 Connexion réussie !");
                    
                    // Small delay for progress completion animation
                    await Task.Delay(200);
                    
                    // Switch to Shell navigation for main application
                    _navigationService.SwitchToShellNavigation();
                    _logger.LogInformation("Switched to Shell navigation after successful login");
                }
                else
                {
                    _logger.LogWarning("Login failed for user: {Email}", Email);
                    ShowLoginError("Email ou mot de passe incorrect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                ShowLoginError("Une erreur inattendue s'est produite. Veuillez réessayer.");
                await _dialogService.ShowAlertAsync("Erreur", "Une erreur inattendue s'est produite.", "D'accord");
            }
            finally
            {
                IsLoginInProgress = false;
                LoginProgress = 0.0;
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
                await _dialogService.ShowAlertAsync("Erreur", "Impossible d'accéder à la page d'inscription.", "D'accord");
            }
        }

        [RelayCommand]
        private async Task NavigateToForgotPassword()
        {
            try
            {
                // Show forgot password dialog
                var email = await _dialogService.ShowPromptAsync(
                    "Mot de passe oublié",
                    "Entrez votre adresse email pour réinitialiser votre mot de passe :",
                    "Envoyer",
                    "Annuler",
                    Email);

                if (!string.IsNullOrWhiteSpace(email))
                {
                    // Use the dedicated password reset service
                    var result = await _passwordResetService.RequestPasswordResetAsync(email);
                    
                    if (result.Success)
                    {
                        await _dialogService.ShowAlertAsync(
                            "🔒 Email envoyé",
                            "Si cette adresse email est associée à un compte vérifié, vous recevrez un email de réinitialisation.\n\nVérifiez votre boîte de réception et vos spams.",
                            "D'accord");
                    }
                    else
                    {
                        string errorMessage = result.ResultType switch
                        {
                            PasswordResetResultType.DailyLimitReached => "Limite quotidienne atteinte. Réessayez demain.",
                            PasswordResetResultType.UserNotVerified => "Veuillez d'abord vérifier votre adresse email.",
                            _ => "Impossible d'envoyer l'email de réinitialisation. Veuillez réessayer plus tard."
                        };

                        await _dialogService.ShowAlertAsync("Erreur", errorMessage, "D'accord");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password flow");
                await _dialogService.ShowAlertAsync("Erreur", "Une erreur s'est produite.", "D'accord");
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }


        public async Task InitializeAsync(object parameter = null)
        {
            try
            {
                // Don't auto-navigate on initialization - let the user choose to login
                // The authentication check will happen when they click the login button
                
                // Clear any previous state
                ClearLoginError();
                Email = "admin@subexplore.com"; // Pre-fill for debugging
                Password = "Admin123!"; // Pre-fill for debugging
                RememberMe = false;
                IsPasswordVisible = false;
                
                // Load remembered credentials if available
                await LoadRememberedCredentialsAsync();
                
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

        private void UpdateCanLogin()
        {
            CanLogin = IsEmailValid && IsPasswordValid && !IsLoginInProgress;
        }

        private void ValidateEmail()
        {
            IsEmailValid = !string.IsNullOrWhiteSpace(Email) && IsValidEmail(Email);
            UpdateCanLogin();
        }

        private void ValidatePasswordField()
        {
            IsPasswordValid = !string.IsNullOrWhiteSpace(Password) && Password.Length >= 8;
            UpdateCanLogin();
        }

        partial void OnEmailChanged(string value)
        {
            ValidateEmail();
            if (HasLoginError)
            {
                ClearLoginError();
            }
        }

        partial void OnPasswordChanged(string value)
        {
            ValidatePasswordField();
            if (HasLoginError)
            {
                ClearLoginError();
            }
        }

        partial void OnIsLoginInProgressChanged(bool value)
        {
            UpdateCanLogin();
        }

        /// <summary>
        /// Save user credentials securely when "Remember Me" is checked
        /// </summary>
        private async Task SaveRememberedCredentialsAsync()
        {
            try
            {
                await _secureSettings.SetSecureAsync("remembered_email", Email);
                await _secureSettings.SetSecureAsync("remember_me", true);
                await _secureSettings.SetSecureAsync("last_login_time", DateTime.UtcNow);
                await _secureSettings.SetSecureAsync("auto_login_enabled", true);
                _logger.LogInformation("User credentials saved securely");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving remembered credentials");
            }
        }

        /// <summary>
        /// Load previously saved credentials if "Remember Me" was used
        /// </summary>
        private async Task LoadRememberedCredentialsAsync()
        {
            try
            {
                var rememberMe = await _secureSettings.GetSecureAsync("remember_me", false);
                
                if (rememberMe)
                {
                    var savedEmail = await _secureSettings.GetSecureAsync("remembered_email", string.Empty);
                    var autoLogin = await _secureSettings.GetSecureAsync("auto_login_enabled", false);
                    var lastLoginTime = await _secureSettings.GetSecureAsync("last_login_time", DateTime.MinValue);
                    
                    if (!string.IsNullOrWhiteSpace(savedEmail))
                    {
                        Email = savedEmail;
                        SavedUserEmail = savedEmail;
                        RememberMe = true;
                        
                        // Show auto-login option if credentials were saved recently (within 7 days)
                        var daysSinceLastLogin = (DateTime.UtcNow - lastLoginTime).TotalDays;
                        ShowAutoLoginOption = autoLogin && daysSinceLastLogin <= 7;
                        
                        _logger.LogInformation("Loaded remembered credentials for user");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading remembered credentials");
            }
        }

        /// <summary>
        /// Clear saved credentials when "Remember Me" is unchecked
        /// </summary>
        private async Task ClearRememberedCredentialsAsync()
        {
            try
            {
                await _secureSettings.RemoveSecureAsync("remembered_email");
                await _secureSettings.RemoveSecureAsync("remember_me");
                await _secureSettings.RemoveSecureAsync("last_login_time");
                await _secureSettings.RemoveSecureAsync("auto_login_enabled");
                _logger.LogInformation("Cleared remembered credentials");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing remembered credentials");
            }
        }

        /// <summary>
        /// Add command to clear remembered credentials if user wants to forget
        /// </summary>
        [RelayCommand]
        private async Task ClearRememberedCredentials()
        {
            try
            {
                await ClearRememberedCredentialsAsync();
                Email = string.Empty;
                Password = string.Empty;
                RememberMe = false;
                ShowAutoLoginOption = false;
                SavedUserEmail = string.Empty;
                await _dialogService.ShowToastAsync("Identifiants oubliés");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in clear remembered credentials command");
                await _dialogService.ShowAlertAsync("Erreur", "Impossible d'effacer les identifiants sauvegardés.", "D'accord");
            }
        }

        /// <summary>
        /// Auto-login with saved credentials
        /// </summary>
        [RelayCommand]
        private async Task AutoLogin()
        {
            if (IsAutoLoginInProgress || string.IsNullOrEmpty(SavedUserEmail)) return;

            try
            {
                IsAutoLoginInProgress = true;
                
                // Check if user is already authenticated
                if (_simpleAuthenticationService.IsAuthenticated)
                {
                    _logger.LogInformation("User already authenticated, navigating to main app");
                    _navigationService.SwitchToShellNavigation();
                    return;
                }

                _logger.LogInformation("Attempting auto-login for saved user");
                await _dialogService.ShowToastAsync($"Connexion automatique pour {SavedUserEmail}...");
                
                // Small delay for UX
                await Task.Delay(1000);
                
                // Try to restore session - if this fails, user will need to login manually
                await _simpleAuthenticationService.InitializeAsync();
                
                if (_simpleAuthenticationService.IsAuthenticated)
                {
                    _logger.LogInformation("Auto-login successful");
                    await _dialogService.ShowToastAsync("Connecté automatiquement !");
                    _navigationService.SwitchToShellNavigation();
                }
                else
                {
                    _logger.LogInformation("Auto-login failed, session expired");
                    ShowAutoLoginOption = false;
                    await _dialogService.ShowToastAsync("Session expirée, veuillez vous reconnecter");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-login");
                ShowAutoLoginOption = false;
                await _dialogService.ShowToastAsync("Erreur lors de la connexion automatique");
            }
            finally
            {
                IsAutoLoginInProgress = false;
            }
        }

        /// <summary>
        /// Disable auto-login feature
        /// </summary>
        [RelayCommand]
        private async Task DisableAutoLogin()
        {
            try
            {
                await _secureSettings.SetSecureAsync("auto_login_enabled", false);
                ShowAutoLoginOption = false;
                await _dialogService.ShowToastAsync("Connexion automatique désactivée");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling auto-login");
            }
        }
    }
}