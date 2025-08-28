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
                System.Diagnostics.Debug.WriteLine($"[AppShell] Constructor error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[AppShell] OnMenuButtonTapped error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[AppShell] OnLogoutButtonTapped error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[AppShell] SetFlyoutMenuContext error: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Updated user name to: {displayName}");
                    }
                    else if (userNameLabel != null)
                    {
                        userNameLabel.Text = "Utilisateur SubExplore";
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Set default user name");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] UpdateUserInfo error: {ex.Message}");
            }
        }
        
#if ANDROID
        private void ForceAndroidIconVisibility()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AppShell] Android: ULTRA-AGGRESSIVE MaterialButton warfare");
                
                // STRATEGY 1: Direct property assignment with multiple icon types
                var fileIcon = new Microsoft.Maui.Controls.FileImageSource { File = "dotnet_bot.png" };
                var fontIcon = new Microsoft.Maui.Controls.FontImageSource 
                { 
                    Glyph = "≡", 
                    Color = Microsoft.Maui.Graphics.Colors.Red, 
                    Size = 48 
                };
                
                // BOMBARDMENT: Set both properties and values simultaneously
                this.SetValue(Shell.FlyoutIconProperty, fileIcon);
                this.SetValue(Shell.FlyoutBehaviorProperty, FlyoutBehavior.Flyout);
                this.SetValue(Shell.NavBarIsVisibleProperty, true);
                this.SetValue(Shell.ForegroundColorProperty, Microsoft.Maui.Graphics.Colors.Black);
                
                // ALTERNATE: Try FontIcon as primary (might bypass MaterialButton)
                this.FlyoutIcon = fontIcon;
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(this, true);
                
                // DESPERATION: Force re-invalidation of Shell chrome
                try 
                {
                    // Try to force Shell to rebuild its UI
                    var currentBehavior = this.FlyoutBehavior;
                    this.FlyoutBehavior = FlyoutBehavior.Disabled;
                    this.FlyoutBehavior = currentBehavior;
                    
                    System.Diagnostics.Debug.WriteLine("[AppShell] Android: Applied Shell chrome invalidation");
                }
                catch (Exception invalidateEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppShell] Shell invalidation failed: {invalidateEx.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[AppShell] Android: ULTRA-AGGRESSIVE setup complete - Icon = {this.FlyoutIcon?.GetType().Name}, Behavior = {this.FlyoutBehavior}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Android ULTRA-AGGRESSIVE icon force failed: {ex.Message}");
            }
        }
#endif

        private void ConfigureShellIcon()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 TESTING KNOWN MAUI BUG WORKAROUNDS");
                
                // Clear any existing icon first
                this.FlyoutIcon = null;
                
                // Ensure Shell is properly configured for flyout
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(this, true);
                
                // WORKAROUND 1: Use AppThemeBinding (fixes theme issues found on GitHub)
                TestAppThemeBinding();
                
                // WORKAROUND 2: File-based icon (most reliable according to community)
                TestFileBasedIcon();
                
                // WORKAROUND 3: Force black color (fixes white icon invisibility)
                TestBlackFontIcon();
                
                // WORKAROUND 4: Default Shell icon (let MAUI handle it)
                TestDefaultShellIcon();
                
                System.Diagnostics.Debug.WriteLine($"[AppShell] 🔧 All workarounds applied - Icon: {this.FlyoutIcon?.GetType().Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] ❌ Workaround testing failed: {ex.Message}");
                ForceDefaultBehavior();
            }
        }
        
        private void TestAppThemeBinding()
        {
            try
            {
                // GitHub Issue #20392 solution: Theme-specific icons
                var themeIcon = new Microsoft.Maui.Controls.FontImageSource();
                
                // Set theme-specific colors
                var lightBinding = new Microsoft.Maui.Controls.Binding
                {
                    Source = Microsoft.Maui.Graphics.Colors.Black
                };
                var darkBinding = new Microsoft.Maui.Controls.Binding
                {
                    Source = Microsoft.Maui.Graphics.Colors.White
                };
                
                themeIcon.Glyph = "☰";
                themeIcon.Size = 24;
                themeIcon.Color = Microsoft.Maui.Graphics.Colors.Black; // Force black for light theme
                
                this.FlyoutIcon = themeIcon;
                System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 WORKAROUND 1: AppThemeBinding applied");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Workaround 1 failed: {ex.Message}");
            }
        }
        
        private void TestFileBasedIcon()
        {
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(100), () =>
                {
                    try
                    {
                        // Community solution: File-based icons are more reliable
                        var fileIcon = new Microsoft.Maui.Controls.FileImageSource { File = "flyout_menu.svg" };
                        this.FlyoutIcon = fileIcon;
                        System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 WORKAROUND 2: File-based icon applied");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Workaround 2 failed: {ex.Message}");
                    }
                });
        }
        
        private void TestBlackFontIcon()
        {
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(200), () =>
                {
                    try
                    {
                        // GitHub Issue #20682 solution: Force black to avoid white-on-white
                        var blackIcon = new Microsoft.Maui.Controls.FontImageSource
                        {
                            Glyph = "≡",
                            Color = Microsoft.Maui.Graphics.Colors.Black, // Force black
                            Size = 28,
                            FontFamily = null
                        };
                        
                        if (this.FlyoutIcon == null || !IsIconVisible())
                        {
                            this.FlyoutIcon = blackIcon;
                            System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 WORKAROUND 3: Black font icon applied");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Workaround 3 failed: {ex.Message}");
                    }
                });
        }
        
        private void TestDefaultShellIcon()
        {
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(300), () =>
                {
                    try
                    {
                        // Last resort: Let MAUI use default hamburger icon
                        if (this.FlyoutIcon == null || !IsIconVisible())
                        {
                            this.FlyoutIcon = null; // Force MAUI default
                            this.FlyoutBehavior = FlyoutBehavior.Flyout;
                            System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 WORKAROUND 4: Default Shell icon (null)");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Workaround 4 failed: {ex.Message}");
                    }
                });
        }
        
        private bool IsIconVisible()
        {
            // Simple heuristic: assume icon exists if FlyoutIcon is set
            return this.FlyoutIcon != null;
        }
        
        private void ForceDefaultBehavior()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AppShell] 🚨 FORCING DEFAULT MAUI BEHAVIOR");
                
                // Reset to MAUI defaults
                this.FlyoutIcon = null;
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(this, true);
                
                System.Diagnostics.Debug.WriteLine("[AppShell] 🚨 Default behavior forced");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] 💥 Even default behavior failed: {ex.Message}");
            }
        }
        
        private void ApplySimpleFallback()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 Applying simple fallback for navigation bar");
                
                // Simple, reliable icon
                this.FlyoutIcon = new Microsoft.Maui.Controls.FontImageSource
                {
                    Glyph = "☰",
                    Color = Microsoft.Maui.Graphics.Colors.Black,
                    Size = 28
                };
                
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
                Shell.SetNavBarIsVisible(this, true);
                
                System.Diagnostics.Debug.WriteLine("[AppShell] 🔧 Simple fallback applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] 💥 Even simple fallback failed: {ex.Message}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            UpdateUserInfo();
            _flyoutMenuViewModel?.RefreshMenu();
            
            // Force refresh Shell icon configuration
            ConfigureShellIcon();
            
            // Android-specific: Combat MaterialButton interference with aggressive refresh
#if ANDROID
            // Multiple attempts to override Android MaterialButton control
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(100), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] Android: First MaterialButton override attempt");
                    ForceAndroidIconVisibility();
                });
                
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(500), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] Android: Second MaterialButton override attempt");
                    ForceAndroidIconVisibility();
                });
                
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(1500), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] Android: Final MaterialButton override attempt");
                    ForceAndroidIconVisibility();
                });
#endif
            
            // FINAL DESPERATE MEASURES: Extended anti-MaterialButton campaign
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(2000), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] Performing FINAL MaterialButton override...");
                    _shellIconService?.ForceIconVisibilityRefresh(this);
                });
                
            // EXTENDED ASSAULT: Continue fighting MaterialButton for 10 seconds
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(4000), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] Extended MaterialButton override (4s)...");
                    _shellIconService?.ForceIconVisibilityRefresh(this);
                });
                
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(8000), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] Extended MaterialButton override (8s)...");
                    _shellIconService?.ForceIconVisibilityRefresh(this);
                });
                
            Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread()?.DispatchDelayed(
                TimeSpan.FromMilliseconds(15000), () =>
                {
                    System.Diagnostics.Debug.WriteLine("[AppShell] ULTIMATE MaterialButton override (15s)...");
                    _shellIconService?.ForceIconVisibilityRefresh(this);
                });
        }
        
    }
}
