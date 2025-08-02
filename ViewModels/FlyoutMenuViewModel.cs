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

    public FlyoutMenuViewModel(INavigationService navigationService, IAuthenticationService? authenticationService = null)
    {
        _navigationService = navigationService;
        _authenticationService = authenticationService;
        
        InitializeMenuItems();
        
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
                ShowBadge = true,
                BadgeText = "3",
                Route = "///spotvalidation"
            });
        }

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

    [RelayCommand]
    private async Task NavigateToAsync(string route)
    {
        try
        {
            if (string.IsNullOrEmpty(route))
                return;

            await Shell.Current.GoToAsync(route);
            Shell.Current.FlyoutIsPresented = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlyoutMenuViewModel] Navigation error: {ex.Message}");
        }
    }

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
                        "Vous avez été déconnecté avec succès.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Déconnexion", 
                        "Vous avez été déconnecté avec succès.", "OK");
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
            return false;
            
        var user = _authenticationService.CurrentUser;
        return user.AccountType == Models.Enums.AccountType.Administrator || 
               user.AccountType == Models.Enums.AccountType.ExpertModerator;
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