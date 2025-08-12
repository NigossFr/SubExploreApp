using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Controls;
using SubExplore.Services.Interfaces;
using System.Collections.ObjectModel;

namespace SubExplore.ViewModels;

public partial class FlyoutMenuViewModel : ObservableObject
{
    private readonly IAuthenticationService? _authenticationService;
    private readonly INavigationService _navigationService;
    private readonly ISpotValidationService? _spotValidationService;

    public FlyoutMenuViewModel(INavigationService navigationService, IAuthenticationService? authenticationService = null, ISpotValidationService? spotValidationService = null)
    {
        _navigationService = navigationService;
        _authenticationService = authenticationService;
        _spotValidationService = spotValidationService;
        
        InitializeMenuItems();
        
        // Load validation badge count if service is available
        _ = UpdateValidationBadgeAsync();
        
        // Subscribe to authentication state changes
        if (_authenticationService != null)
        {
            _authenticationService.StateChanged += OnAuthenticationStateChanged;
        }
    }

    [ObservableProperty]
    private ObservableCollection<MenuItemViewModel> _menuItems = new();

    [ObservableProperty]
    private string _userName = "Utilisateur SubExplore";

    [ObservableProperty]
    private string _userSubtitle = "Explorez les profondeurs";

    [ObservableProperty]
    private bool _isUserLoggedIn = false;

    private void InitializeMenuItems()
    {
        MenuItems.Clear();

        // Navigation Section
        MenuItems.Add(new MenuItemViewModel
        {
            Id = "navigation_header",
            Title = "Navigation",
            IsHeader = true
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "map",
            Title = "Carte",
            Description = "Explorer les spots de plongée",
            Icon = "🗺️",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///map"
        });

        // Mes Spots Section
        MenuItems.Add(new MenuItemViewModel
        {
            Id = "spots_header",
            Title = "Mes Spots",
            IsHeader = true
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "myspots",
            Title = "Mes Spots",
            Description = "Spots que vous avez créés",
            Icon = "📍",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///myspots"
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "favorites",
            Title = "Favoris",
            Description = "Vos spots préférés",
            Icon = "⭐",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///favorites"
        });

        // Profil Section
        MenuItems.Add(new MenuItemViewModel
        {
            Id = "profile_header",
            Title = "Profil",
            IsHeader = true
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "profile",
            Title = "Profil",
            Description = "Votre profil utilisateur",
            Icon = "👤",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///userprofile"
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "preferences",
            Title = "Préférences",
            Description = "Paramètres de l'application",
            Icon = "⚙️",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///userpreferences"
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "stats",
            Title = "Statistiques",
            Description = "Vos statistiques de plongée",
            Icon = "📊",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///userstats"
        });

        // Administration Section (conditional)
        if (IsUserAdmin())
        {
            MenuItems.Add(new MenuItemViewModel
            {
                Id = "admin_header",
                Title = "Administration",
                IsHeader = true
            });

            MenuItems.Add(new MenuItemViewModel
            {
                Id = "validation",
                Title = "Validation des Spots",
                Description = "Administration des spots",
                Icon = "⚖️",
                Stage = MenuButtonStage.Warning,
                ShowIcon = true,
                ShowBadge = false, // Will be updated dynamically
                BadgeText = "",
                Route = "///spotvalidation"
            });
        }

#if DEBUG
        // Debug Section - Only visible in debug builds
        MenuItems.Add(new MenuItemViewModel
        {
            Id = "debug_header",
            Title = "🐛 Debug",
            IsHeader = true
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "debug_validation",
            Title = "🔧 Validation Page (Debug)",
            Description = "Direct access to validation page",
            Icon = "⚖️",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "///spotvalidation"
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "debug_auth_info",
            Title = "🔍 Auth Debug Info",
            Description = "Show current authentication status",
            Icon = "📋",
            Stage = MenuButtonStage.Default,
            ShowIcon = true,
            Route = "debug_auth"
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Id = "debug_login_admin",
            Title = "🔑 Login as Admin",
            Description = "Quick login as admin user for testing",
            Icon = "👑",
            Stage = MenuButtonStage.Error,
            ShowIcon = true,
            Route = "debug_login_admin"
        });
#endif

        // Logout Button
        MenuItems.Add(new MenuItemViewModel
        {
            Id = "logout",
            Title = "Déconnexion",
            Description = "",
            Icon = "🚪",
            Stage = MenuButtonStage.Error,
            ShowIcon = true,
            ShowDescription = false,
            IsLogoutButton = true
        });
    }

    private async Task UpdateValidationBadgeAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] === DEBUGGING BADGE UPDATE ===");
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Service available: {_spotValidationService != null}");
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] User is admin: {IsUserAdmin()}");
            
            // Only update if validation service is available and user is admin/moderator
            if (_spotValidationService == null || !IsUserAdmin())
            {
                System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] Validation service not available or user not admin - skipping badge update");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] Calling GetValidationStatsAsync...");
            var statsResult = await _spotValidationService.GetValidationStatsAsync();
            
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Stats result - Success: {statsResult.Success}, Error: {statsResult.ErrorMessage}");
            
            if (statsResult.Success && statsResult.Data != null)
            {
                var pendingCount = statsResult.Data.PendingCount;
                var totalSpots = statsResult.Data.TotalSpots;
                
                System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Stats data - PendingCount: {pendingCount}, TotalSpots: {totalSpots}");
                
                var validationItem = MenuItems.FirstOrDefault(m => m.Id == "validation");
                System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Found validation menu item: {validationItem != null}");
                
                if (validationItem != null)
                {
                    if (pendingCount > 0)
                    {
                        validationItem.BadgeText = pendingCount.ToString();
                        validationItem.ShowBadge = true;
                        System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Updated validation badge: {pendingCount} pending spots");
                    }
                    else
                    {
                        validationItem.ShowBadge = false;
                        validationItem.BadgeText = "";
                        System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] No pending spots, hiding badge");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Failed to get stats or no data returned");
            }
            System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] === END BADGE DEBUG ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Error updating validation badge: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task NavigateToAsync(string route)
    {
        try
        {
            if (string.IsNullOrEmpty(route))
                return;

#if DEBUG
            // Handle debug routes
            if (route == "debug_auth")
            {
                await ShowAuthDebugInfoAsync();
                Shell.Current.FlyoutIsPresented = false;
                return;
            }
            
            if (route == "debug_login_admin")
            {
                await DebugLoginAsAdminAsync();
                Shell.Current.FlyoutIsPresented = false;
                return;
            }
#endif

            await Shell.Current.GoToAsync(route);
            Shell.Current.FlyoutIsPresented = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Navigation error: {ex.Message}");
        }
    }

#if DEBUG
    private async Task ShowAuthDebugInfoAsync()
    {
        try
        {
            var currentUser = _authenticationService?.CurrentUser;
            var isAuthenticated = currentUser != null;
            var isAdmin = IsUserAdmin();

            var debugInfo = $"""
                🔍 Authentication Debug Info:
                
                📍 Service Status: {(_authenticationService != null ? "Available" : "NULL")}
                🔐 Is Authenticated: {isAuthenticated}
                👤 Current User: {currentUser?.Username ?? "None"}
                🆔 User ID: {currentUser?.Id ?? 0}
                🎭 Account Type: {currentUser?.AccountType ?? Models.Enums.AccountType.Standard}
                ⚖️ Is Admin/Moderator: {isAdmin}
                
                📋 Full User Details:
                {System.Text.Json.JsonSerializer.Serialize(currentUser, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}
                """;

            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] {debugInfo}");
            
            await Shell.Current.DisplayAlert("🐛 Auth Debug", 
                $"User: {currentUser?.Username ?? "Not authenticated"}\n" +
                $"Account Type: {currentUser?.AccountType ?? Models.Enums.AccountType.Standard}\n" +
                $"Is Admin: {isAdmin}\n\n" +
                "Check debug console for full details.", 
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] ShowAuthDebugInfoAsync error: {ex.Message}");
            await Shell.Current.DisplayAlert("Debug Error", ex.Message, "OK");
        }
    }

    private async Task DebugLoginAsAdminAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] Debug: Attempting admin login...");

            // Try to login with the default admin credentials
            var result = await _authenticationService.LoginAsync("admin@subexplore.com", "AdminPassword123!");
            
            if (result.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Debug: Admin login successful! User: {result.User?.Username}");
                
                // Refresh the menu to show admin items
                InitializeMenuItems();
                
                await Shell.Current.DisplayAlert("🔑 Debug Login", 
                    $"Successfully logged in as: {result.User?.Username}\n" +
                    $"Account Type: {result.User?.AccountType}\n\n" +
                    "Admin menu should now be visible!", 
                    "OK");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Debug: Admin login failed: {result.ErrorMessage}");
                
                // Try alternative approach - create temp admin session
                await CreateTempAdminSessionAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] DebugLoginAsAdminAsync error: {ex.Message}");
            await Shell.Current.DisplayAlert("Debug Login Error", 
                $"Login attempt failed: {ex.Message}\n\nTry using the direct Validation Page access instead.", 
                "OK");
        }
    }

    private async Task CreateTempAdminSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] Debug: Creating temporary admin session...");
            
            await Shell.Current.DisplayAlert("🔧 Debug Mode", 
                "Login failed, but you can still access the Validation Page directly via the debug menu.\n\n" +
                "This bypasses authentication for testing purposes only.", 
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] CreateTempAdminSessionAsync error: {ex.Message}");
        }
    }
#endif

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;

            bool confirm = await Shell.Current.DisplayAlert("Déconnexion", 
                "Êtes-vous sûr de vouloir vous déconnecter ?", "Oui", "Annuler");

            if (confirm)
            {
                if (_authenticationService != null)
                {
                    await _authenticationService.LogoutAsync();
                    await Shell.Current.DisplayAlert("Déconnexion", 
                        "Vous avez été déconnecté avec succès.", "D'accord");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Déconnexion", 
                        "Vous avez été déconnecté avec succès.", "D'accord");
                }

                await Shell.Current.GoToAsync("///map");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Logout error: {ex.Message}");
        }
    }

    public void UpdateMenuItemStage(string itemId, MenuButtonStage stage, string? badgeText = null)
    {
        var item = MenuItems.FirstOrDefault(m => m.Id == itemId);
        if (item != null)
        {
            item.Stage = stage;
            if (!string.IsNullOrEmpty(badgeText))
            {
                item.BadgeText = badgeText;
                item.ShowBadge = true;
            }
            else if (string.IsNullOrEmpty(badgeText))
            {
                item.ShowBadge = false;
            }
        }
    }

    public void SetMenuItemBadge(string itemId, string badgeText, bool show = true)
    {
        var item = MenuItems.FirstOrDefault(m => m.Id == itemId);
        if (item != null)
        {
            item.BadgeText = badgeText;
            item.ShowBadge = show;
        }
    }

    public void RefreshMenu()
    {
        InitializeMenuItems();
        UpdateUserInfo();
        _ = UpdateValidationBadgeAsync(); // Update badge asynchronously
    }

    private void OnAuthenticationStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
    {
        UpdateUserInfo();
        RefreshMenu();
    }

    private void UpdateUserInfo()
    {
        try
        {
            if (_authenticationService?.CurrentUser != null)
            {
                var user = _authenticationService.CurrentUser;
                var displayName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                    ? $"{user.FirstName} {user.LastName}"
                    : user.Username ?? "Utilisateur SubExplore";

                UserName = displayName;
                IsUserLoggedIn = true;
                UserSubtitle = "Explorez les profondeurs";
            }
            else
            {
                UserName = "Utilisateur SubExplore";
                IsUserLoggedIn = false;
                UserSubtitle = "Connectez-vous pour explorer";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] UpdateUserInfo error: {ex.Message}");
        }
    }

    private bool IsUserAdmin()
    {
        // Check if current user is admin based on AccountType
        if (_authenticationService?.CurrentUser == null)
        {
            System.Diagnostics.Debug.WriteLine("[FlyoutMenuViewModel] IsUserAdmin: No current user authenticated");
            return false;
        }
            
        var user = _authenticationService.CurrentUser;
        var isAdmin = user.AccountType == Models.Enums.AccountType.Administrator || 
                      user.AccountType == Models.Enums.AccountType.ExpertModerator;
        
        System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] IsUserAdmin: User {user.Username} (ID:{user.Id}) AccountType:{user.AccountType} IsAdmin:{isAdmin}");
        
        return isAdmin;
    }
}

public partial class MenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private MenuButtonStage _stage = MenuButtonStage.Default;

    [ObservableProperty]
    private bool _showIcon = false;

    [ObservableProperty]
    private bool _showDescription = true;

    [ObservableProperty]
    private bool _showBadge = false;

    [ObservableProperty]
    private string _badgeText = string.Empty;

    [ObservableProperty]
    private string _route = string.Empty;

    [ObservableProperty]
    private bool _isHeader = false;

    [ObservableProperty]
    private bool _isLogoutButton = false;
}