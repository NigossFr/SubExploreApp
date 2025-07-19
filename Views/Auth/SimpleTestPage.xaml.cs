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
                DebugLabel.Text = $"Error loading debug info: {ex.Message}";
            }
        }

        private async void OnTestLoginClicked(object sender, EventArgs e)
        {
            try
            {
                StatusLabel.Text = "Testing login...";
                StatusLabel.TextColor = Colors.Orange;

                var email = EmailEntry.Text;
                var password = PasswordEntry.Text;

                Debug.WriteLine($"[SimpleTestPage] Attempting login with {email}");
                
                var result = await _authService.LoginAsync(email, password);

                if (result.IsSuccess)
                {
                    StatusLabel.Text = $"✅ Login successful! User: {result.User?.Email}";
                    StatusLabel.TextColor = Colors.Green;
                }
                else
                {
                    StatusLabel.Text = $"❌ Login failed: {result.ErrorMessage}";
                    StatusLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Error: {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
                Debug.WriteLine($"[SimpleTestPage] Login error: {ex}");
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
                    StatusLabel.Text = "❌ LoginViewModel not found";
                    StatusLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Error switching to login page: {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
            }
        }
    }
}