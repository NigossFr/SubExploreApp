using Microsoft.Maui.Controls;
using SubExplore.Services.Interfaces;
using SubExplore.Views.Auth;
using SubExplore.ViewModels.Auth;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace SubExplore.Views.Auth
{
    public partial class SimpleTestPage : ContentPage
    {
        private readonly IAuthenticationService _authService;
        private readonly IServiceProvider _services;

        public SimpleTestPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            _authService = services.GetRequiredService<IAuthenticationService>();
            
            // Load debug info
            LoadDebugInfo();
        }

        private async void LoadDebugInfo()
        {
            try
            {
                var debugInfo = $"Auth Service: {(_authService != null ? "✓" : "✗")}\n";
                debugInfo += $"Is Authenticated: {_authService?.IsAuthenticated}\n";
                debugInfo += $"Current User: {_authService?.CurrentUser?.Email ?? "None"}\n";
                
                // Check if admin user exists in database
                var dbInit = _services.GetService<IDatabaseInitializationService>();
                debugInfo += $"DB Init Service: {(dbInit != null ? "✓" : "✗")}\n";
                
                if (dbInit != null)
                {
                    var isInitialized = await dbInit.IsDatabaseInitializedAsync();
                    debugInfo += $"DB Initialized: {isInitialized}\n";
                }

                DebugLabel.Text = debugInfo;
            }
            catch (Exception ex)
            {
                DebugLabel.Text = $"Erreur lors du chargement des informations de débogage : {ex.Message}";
            }
        }

        private async void OnTestLoginClicked(object sender, EventArgs e)
        {
            try
            {
                StatusLabel.Text = "Test de connexion en cours...";
                StatusLabel.TextColor = Colors.Orange;

                var email = EmailEntry.Text;
                var password = PasswordEntry.Text;

                System.Diagnostics.Debug.WriteLine($"[SimpleTestPage] Attempting login with {email}");
                
                var result = await _authService.LoginAsync(email, password);

                if (result.IsSuccess)
                {
                    StatusLabel.Text = $"✅ Connexion réussie ! Utilisateur : {result.User?.Email}";
                    StatusLabel.TextColor = Colors.Green;
                }
                else
                {
                    StatusLabel.Text = $"❌ Échec de la connexion : {result.ErrorMessage}";
                    StatusLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Erreur : {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
                System.Diagnostics.Debug.WriteLine($"[SimpleTestPage] Login error: {ex}");
            }
        }

        private async void OnRealLoginClicked(object sender, EventArgs e)
        {
            try
            {
                // Get the real LoginPage and switch to it
                var loginViewModel = _services.GetService<LoginViewModel>();
                if (loginViewModel != null)
                {
                    var loginPage = new LoginPage(loginViewModel);
                    Application.Current.MainPage = new NavigationPage(loginPage);
                }
                else
                {
                    StatusLabel.Text = "❌ LoginViewModel introuvable";
                    StatusLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Erreur lors du passage à la page de connexion : {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
            }
        }
    }
}