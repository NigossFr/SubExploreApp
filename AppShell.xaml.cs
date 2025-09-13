using SubExplore.Views.Map;
using SubExplore.Views.Spots;
// ✅ Favorites réactivé avec pages simples compatibles API Supabase  
using SubExplore.Views.Favorites;
using SubExplore.Views.Profile;
using SubExplore.Views.Admin;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels;
using SubExplore.Controls;
using SubExplore.Helpers;
using System.Diagnostics;

namespace SubExplore
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService? _authenticationService;
        private readonly IShellIconService? _shellIconService;
        private readonly FlyoutMenuViewModel _flyoutMenuViewModel;
        
        public AppShell()
        {
            InitializeComponent();
            
            RegisterRoutes();
            
            // Initialize services and ViewModel
            try
            {
                var navigationService = ServiceHelper.GetService<INavigationService>();
                var authService = ServiceHelper.TryGetService<IAuthenticationService>();
                _shellIconService = ServiceHelper.TryGetService<IShellIconService>();
                _flyoutMenuViewModel = new FlyoutMenuViewModel(navigationService, authService);
                _authenticationService = authService;
            }
            catch (Exception ex)
            {
                // AppShell constructor error
                // Fallback initialization - but don't create fallback services silently
                throw new InvalidOperationException("Failed to initialize AppShell dependencies. Please check service registration.", ex);
            }
            
            // Set the flyout content binding context
            SetFlyoutMenuContext();
            UpdateUserInfo();
            
            // Subscribe to authentication state changes if service is available
            if (_authenticationService != null)
            {
                _authenticationService.StateChanged += OnAuthenticationStateChanged;
            }
            
            // Subscribe to flyout menu requests from isolated pages (like SpotDetailsPage)
            MessagingCenter.Subscribe<object>(this, "OpenFlyoutMenu", (sender) =>
            {
                try
                {
                    Debug.WriteLine("[AppShell] Received flyout request via MessagingCenter");
                    Debug.WriteLine($"[AppShell] This Shell instance: {this.GetHashCode()}");
                    Debug.WriteLine($"[AppShell] Application.Current.MainPage: {Application.Current?.MainPage?.GetHashCode()}");
                    Debug.WriteLine($"[AppShell] Shell.Current: {Shell.Current?.GetHashCode() ?? -1}");
                    
                    // Try multiple approaches to open flyout
                    bool flyoutOpened = false;
                    
                    // Method 1: Try on Application.Current.MainPage (most reliable)
                    try
                    {
                        var mainPage = Application.Current?.MainPage;
                        Debug.WriteLine($"[AppShell] Method 1: MainPage type: {mainPage?.GetType().Name}");
                        
                        if (mainPage is Shell mainPageShell)
                        {
                            Debug.WriteLine($"[AppShell] Method 1: MainPage is Shell, setting FlyoutIsPresented = true");
                            mainPageShell.FlyoutIsPresented = true;
                            flyoutOpened = true;
                            Debug.WriteLine("[AppShell] ✅ Method 1: Flyout opened on Application.Current.MainPage Shell");
                        }
                        else
                        {
                            Debug.WriteLine($"[AppShell] Method 1: MainPage is not Shell, it's {mainPage?.GetType().Name}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Debug.WriteLine($"[AppShell] ❌ Method 1 failed: {ex1.Message}");
                    }
                    
                    // Method 2: Try on this instance if Method 1 failed
                    if (!flyoutOpened)
                    {
                        try
                        {
                            Debug.WriteLine($"[AppShell] Method 2: Trying on this instance (hash: {this.GetHashCode()})");
                            this.FlyoutIsPresented = true;
                            flyoutOpened = true;
                            Debug.WriteLine("[AppShell] ✅ Method 2: Flyout opened on this Shell instance");
                        }
                        catch (Exception ex2)
                        {
                            Debug.WriteLine($"[AppShell] ❌ Method 2 failed: {ex2.Message}");
                        }
                    }
                    
                    // Method 3: Try on Shell.Current if different and exists
                    if (!flyoutOpened && Shell.Current != null)
                    {
                        try
                        {
                            Debug.WriteLine($"[AppShell] Method 3: Trying on Shell.Current (hash: {Shell.Current.GetHashCode()})");
                            Shell.Current.FlyoutIsPresented = true;
                            flyoutOpened = true;
                            Debug.WriteLine("[AppShell] ✅ Method 3: Flyout opened on Shell.Current");
                        }
                        catch (Exception ex3)
                        {
                            Debug.WriteLine($"[AppShell] ❌ Method 3 failed: {ex3.Message}");
                        }
                    }
                    
                    // Method 4: Force using MainThread dispatch (last resort)
                    if (!flyoutOpened)
                    {
                        try
                        {
                            Debug.WriteLine("[AppShell] Method 4: Trying with MainThread dispatch as last resort");
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                try
                                {
                                    // Small delay to ensure UI is ready
                                    await Task.Delay(50);
                                    
                                    var mainPage = Application.Current?.MainPage;
                                    if (mainPage is Shell shell)
                                    {
                                        shell.FlyoutIsPresented = true;
                                        Debug.WriteLine("[AppShell] ✅ Method 4: Flyout opened via MainThread dispatch");
                                    }
                                    else
                                    {
                                        this.FlyoutIsPresented = true;
                                        Debug.WriteLine("[AppShell] ✅ Method 4: Flyout opened on this instance via MainThread dispatch");
                                    }
                                }
                                catch (Exception ex4Inner)
                                {
                                    Debug.WriteLine($"[AppShell] ❌ Method 4 inner failed: {ex4Inner.Message}");
                                }
                            });
                            flyoutOpened = true; // Assume it will work
                        }
                        catch (Exception ex4)
                        {
                            Debug.WriteLine($"[AppShell] ❌ Method 4 failed: {ex4.Message}");
                        }
                    }
                    
                    if (flyoutOpened)
                    {
                        Debug.WriteLine("[AppShell] ✅ Flyout opened via MessagingCenter");
                    }
                    else
                    {
                        Debug.WriteLine("[AppShell] ❌ All flyout opening methods failed");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppShell] ❌ MessagingCenter flyout failed: {ex.Message}");
                }
            });
            
            // Configure Shell icon using unified service with explicit icon
            ConfigureShellIcon();
        }
        
        
        // Alternative constructor for explicit dependency injection
        public AppShell(INavigationService navigationService, IAuthenticationService? authenticationService = null, IShellIconService? shellIconService = null)
        {
            InitializeComponent();
            RegisterRoutes();
            
            // Validate required services
            if (navigationService == null)
                throw new ArgumentNullException(nameof(navigationService), "NavigationService is required for AppShell");
            
            _authenticationService = authenticationService;
            _shellIconService = shellIconService;
            
            // Initialize flyout menu ViewModel with provided services
            _flyoutMenuViewModel = new FlyoutMenuViewModel(navigationService, authenticationService);
            
            SetFlyoutMenuContext();
            UpdateUserInfo();
            
            // Subscribe to authentication state changes
            if (_authenticationService != null)
            {
                _authenticationService.StateChanged += OnAuthenticationStateChanged;
            }
            
            // Configure Shell icon using provided service with explicit icon
            ConfigureShellIcon();
        }

        private void RegisterRoutes()
        {
            // Only register additional routes that aren't already defined in AppShell.xaml
            // Main routes are handled by ShellContent elements in XAML
            
            // 🚫 Routes supprimées - utilisaient Entity Framework
            // Routing.RegisterRoute("map/addspot", typeof(AddSpotPage));
            // Routing.RegisterRoute("map/spotdetails", typeof(SpotDetailsPage));
            
            // 🚫 Register routes for spot editing workflow - supprimé
            // Routing.RegisterRoute("spotdetails/editspot", typeof(AddSpotPage));
            
            // NOTE: Removed duplicate nested routes to avoid ambiguity
            // Direct routes (userprofile, favorites, etc.) are defined in AppShell.xaml
        }
        
        // Enhanced Flyout Navigation Handlers for Staged Menu Buttons
        private async void OnMenuButtonTapped(object sender, StagedMenuButtonTappedEventArgs e)
        {
            try
            {
                if (sender is StagedMenuButton button && button.CommandParameter is string route)
                {
                    await GoToAsync(route);
                    FlyoutIsPresented = false;
                }
            }
            catch (Exception ex)
            {
                // AppShell menu button error
            }
        }

        private async void OnLogoutButtonTapped(object sender, StagedMenuButtonTappedEventArgs e)
        {
            try
            {
                FlyoutIsPresented = false;
                
                // Show confirmation dialog
                bool confirm = await DisplayAlert("Déconnexion", "Êtes-vous sûr de vouloir vous déconnecter ?", "Oui", "Annuler");
                
                if (confirm)
                {
                    // Call authentication service to logout if available
                    if (_authenticationService != null)
                    {
                        await _authenticationService.LogoutAsync();
                        await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "D'accord");
                    }
                    else
                    {
                        await DisplayAlert("Déconnexion", "Vous avez été déconnecté avec succès.", "D'accord");
                    }
                    
                    // Navigate back to login or main page
                    await GoToAsync("///map");
                }
            }
            catch (Exception ex)
            {
                // AppShell logout button error
            }
        }

        // Legacy navigation handlers for backward compatibility
        private async void OnNavigateToMap(object sender, EventArgs e)
        {
            await GoToAsync("///map");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToMySpots(object sender, EventArgs e)
        {
            await GoToAsync("///myspots");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToFavorites(object sender, EventArgs e)
        {
            await GoToAsync("///favorites");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToProfile(object sender, EventArgs e)
        {
            await GoToAsync("///userprofile");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToPreferences(object sender, EventArgs e)
        {
            await GoToAsync("///userpreferences");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToStats(object sender, EventArgs e)
        {
            await GoToAsync("///userstats");
            FlyoutIsPresented = false;
        }
        
        private async void OnNavigateToValidation(object sender, EventArgs e)
        {
            await GoToAsync("///spotvalidation");
            FlyoutIsPresented = false;
        }
        
        private async void OnLogout(object sender, EventArgs e)
        {
            OnLogoutButtonTapped(sender, new StagedMenuButtonTappedEventArgs(MenuButtonStage.Error, "Déconnexion"));
        }
        
        private void OnAuthenticationStateChanged(object sender, Services.Interfaces.AuthenticationStateChangedEventArgs e)
        {
            UpdateUserInfo();
            _flyoutMenuViewModel?.RefreshMenu();
        }
        
        private void SetFlyoutMenuContext()
        {
            try
            {
                // Set the binding context directly on the flyout content template
                // The actual flyout content will be created with this context
                if (FlyoutContentTemplate != null)
                {
                    // Store the ViewModel as a bindable property or resource for access by the template
                    this.Resources["FlyoutMenuViewModel"] = _flyoutMenuViewModel;
                }
            }
            catch (Exception ex)
            {
                // AppShell flyout menu context error
            }
        }
        
        // Public methods to update menu state from external components
        public void UpdateMenuItemStage(string itemId, MenuButtonStage stage, string? badgeText = null)
        {
            _flyoutMenuViewModel?.UpdateMenuItemStage(itemId, stage, badgeText);
        }
        
        public void SetMenuItemBadge(string itemId, string badgeText, bool show = true)
        {
            _flyoutMenuViewModel?.SetMenuItemBadge(itemId, badgeText, show);
        }
        
        private void UpdateUserInfo()
        {
            try
            {
                // Find the UserNameLabel in the flyout header template
                var flyoutHeader = FlyoutHeader;
                if (flyoutHeader is View headerView)
                {
                    var userNameLabel = headerView.FindByName<Label>("UserNameLabel");
                    if (userNameLabel != null && _authenticationService?.CurrentUser != null)
                    {
                        var user = _authenticationService.CurrentUser;
                        var displayName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                            ? $"{user.FirstName} {user.LastName}"
                            : user.Username ?? "Utilisateur SubExplore";
                        
                        userNameLabel.Text = displayName;
                        // User name updated
                    }
                    else if (userNameLabel != null)
                    {
                        userNameLabel.Text = "Utilisateur SubExplore";
                        // Default user name set
                    }
                }
            }
            catch (Exception ex)
            {
                // AppShell user info update error
            }
        }
        

        private void ConfigureShellIcon()
        {
            try
            {
                // Simple Shell configuration
                
                // Simple, reliable configuration
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(this, true);
                
                // Use default MAUI hamburger icon
                this.FlyoutIcon = null;
                
                // Simple Shell configuration applied
            }
            catch (Exception ex)
            {
                // Simple configuration failed
            }
        }
        
        
        private void ApplySimpleFallback()
        {
            try
            {
                // Applying simple Shell configuration
                
                // Keep it simple - use MAUI defaults
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(this, true);
                this.FlyoutIcon = null; // Let MAUI handle the default hamburger icon
                
                // Simple Shell configuration applied
            }
            catch (Exception ex)
            {
                // Simple configuration failed
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            UpdateUserInfo();
            _flyoutMenuViewModel?.RefreshMenu();
            
            // Simple, single icon configuration
            ApplySimpleFallback();
        }
        
    }
}
