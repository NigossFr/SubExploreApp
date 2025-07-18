using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.Models.Menu;
using Microsoft.Extensions.Logging;
using SubExplore.ViewModels.Map;
using SubExplore.ViewModels.Spots;
using SubExplore.Repositories.Interfaces;
using MenuItemModel = SubExplore.Models.Menu.MenuItem;

namespace SubExplore.ViewModels.Menu
{
    public partial class MenuViewModel : ViewModelBase
    {
        private readonly ILogger<MenuViewModel> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private User _currentUser;

        [ObservableProperty]
        private ObservableCollection<MenuSection> _menuSections;

        [ObservableProperty]
        private bool _isMenuOpen;

        [ObservableProperty]
        private string _userDisplayName;

        [ObservableProperty]
        private string _userEmail;

        [ObservableProperty]
        private string _userAvatarUrl;

        public MenuViewModel(
            ILogger<MenuViewModel> logger,
            IUserRepository userRepository,
            ISettingsService settingsService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _logger = logger;
            _userRepository = userRepository;
            _settingsService = settingsService;
            
            Title = "Menu";
            MenuSections = new ObservableCollection<MenuSection>();
            
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            MenuSections.Clear();
            
            // Main Navigation Section
            var mainSection = new MenuSection
            {
                Title = "Navigation",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "Carte",
                        Icon = "🗺️",
                        Description = "Explorez les spots de plongée",
                        Command = NavigateToMapCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Mes Spots",
                        Icon = "📍",
                        Description = "Vos spots créés",
                        Command = NavigateToMySpotsCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Ajouter un Spot",
                        Icon = "➕",
                        Description = "Créer un nouveau spot",
                        Command = NavigateToAddSpotCommand,
                        IsEnabled = true
                    }
                }
            };
            
            // User Section
            var userSection = new MenuSection
            {
                Title = "Utilisateur",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "Profil",
                        Icon = "👤",
                        Description = "Gérer votre profil",
                        Command = NavigateToProfileCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Favoris",
                        Icon = "❤️",
                        Description = "Vos spots favoris",
                        Command = NavigateToFavoritesCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Historique",
                        Icon = "📋",
                        Description = "Vos plongées récentes",
                        Command = NavigateToHistoryCommand,
                        IsEnabled = true
                    }
                }
            };
            
            // Settings Section
            var settingsSection = new MenuSection
            {
                Title = "Paramètres",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "Préférences",
                        Icon = "⚙️",
                        Description = "Configurer l'application",
                        Command = NavigateToSettingsCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Notifications",
                        Icon = "🔔",
                        Description = "Gérer les notifications",
                        Command = NavigateToNotificationsCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "À propos",
                        Icon = "ℹ️",
                        Description = "Informations sur l'app",
                        Command = NavigateToAboutCommand,
                        IsEnabled = true
                    }
                }
            };
            
            // Help Section
            var helpSection = new MenuSection
            {
                Title = "Aide",
                Items = new ObservableCollection<MenuItemModel>
                {
                    new MenuItemModel
                    {
                        Title = "Guide d'utilisation",
                        Icon = "📖",
                        Description = "Apprendre à utiliser l'app",
                        Command = NavigateToHelpCommand,
                        IsEnabled = true
                    },
                    new MenuItemModel
                    {
                        Title = "Support",
                        Icon = "💬",
                        Description = "Contactez-nous",
                        Command = NavigateToSupportCommand,
                        IsEnabled = true
                    }
                }
            };
            
            MenuSections.Add(mainSection);
            MenuSections.Add(userSection);
            MenuSections.Add(settingsSection);
            MenuSections.Add(helpSection);
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await LoadCurrentUser();
        }

        private async Task LoadCurrentUser()
        {
            try
            {
                // TODO: Replace with actual user authentication
                var userId = 1; // Temporary hard-coded user ID
                CurrentUser = await _userRepository.GetByIdAsync(userId);
                
                if (CurrentUser != null)
                {
                    UserDisplayName = $"{CurrentUser.FirstName} {CurrentUser.LastName}";
                    UserEmail = CurrentUser.Email;
                    UserAvatarUrl = CurrentUser.AvatarUrl ?? "default_avatar.png";
                }
                else
                {
                    UserDisplayName = "Utilisateur Invité";
                    UserEmail = "guest@subexplore.com";
                    UserAvatarUrl = "default_avatar.png";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading current user");
                UserDisplayName = "Utilisateur Invité";
                UserEmail = "guest@subexplore.com";
                UserAvatarUrl = "default_avatar.png";
            }
        }

        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        [RelayCommand]
        private async Task NavigateToMap()
        {
            await NavigateToAsync<MapViewModel>();
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToMySpots()
        {
            // TODO: Implement MySpots page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToAddSpot()
        {
            await NavigateToAsync<AddSpotViewModel>();
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            // TODO: Implement Profile page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToFavorites()
        {
            // TODO: Implement Favorites page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToHistory()
        {
            // TODO: Implement History page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToSettings()
        {
            // TODO: Implement Settings page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToNotifications()
        {
            // TODO: Implement Notifications page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToAbout()
        {
            // TODO: Implement About page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToHelp()
        {
            // TODO: Implement Help page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task NavigateToSupport()
        {
            // TODO: Implement Support page
            await ShowToastAsync("Fonction à venir");
            IsMenuOpen = false;
        }

        [RelayCommand]
        private async Task Logout()
        {
            var confirmed = await ShowConfirmationAsync(
                "Déconnexion",
                "Êtes-vous sûr de vouloir vous déconnecter ?",
                "Oui",
                "Annuler");

            if (confirmed)
            {
                // TODO: Implement logout logic
                await ShowToastAsync("Déconnexion réussie");
                IsMenuOpen = false;
            }
        }
    }
}