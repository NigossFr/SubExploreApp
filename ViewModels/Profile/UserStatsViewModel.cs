using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.DTOs;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Profile
{
    public partial class UserStatsViewModel : ViewModelBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private UserStatsDto? _userStats;

        [ObservableProperty]
        private Guid _userId;

        [ObservableProperty]
        private User? _currentUser;

        public UserStatsViewModel(
            IUserProfileService userProfileService,
            INavigationService navigationService)
        {
            _userProfileService = userProfileService;
            _navigationService = navigationService;
            
            Title = "Mes Statistiques";
        }

        public override async Task InitializeAsync(object? parameter = null)
        {
            if (parameter is Guid userId)
            {
                UserId = userId;
            }
            else
            {
                // Si aucun paramètre, utiliser l'utilisateur actuel
                var currentUserId = _userProfileService.CurrentUserId;
                if (currentUserId.HasValue)
                {
                    UserId = currentUserId.Value;
                }
            }
            
            if (UserId != Guid.Empty)
            {
                await LoadStatsAsync();
            }
        }

        [RelayCommand]
        private async Task LoadStatsAsync()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                ErrorMessage = string.Empty;

                // Charger les informations utilisateur
                CurrentUser = await _userProfileService.GetUserByIdAsync(UserId);
                System.Diagnostics.Debug.WriteLine($"[UserStatsViewModel] CurrentUser loaded: {CurrentUser?.DisplayName}, AccountType: {CurrentUser?.AccountType}");
                
                // Charger les statistiques
                UserStats = await _userProfileService.GetUserStatsAsync(UserId);
                System.Diagnostics.Debug.WriteLine($"[UserStatsViewModel] UserStats loaded: TotalSpots={UserStats?.TotalSpots}, ValidatedSpots={UserStats?.ValidatedSpots}");

                if (UserStats == null)
                {
                    IsError = true;
                    ErrorMessage = "Impossible de charger les statistiques. Veuillez réessayer.";
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = "An error occurred while loading statistics.";
                System.Diagnostics.Debug.WriteLine($"[UserStatsViewModel] LoadStatsAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadStatsAsync();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[UserStatsViewModel] GoBackAsync: Navigating back to user profile");
                
                // Naviguer directement vers la page profil utilisateur
                await Shell.Current.GoToAsync("///userprofile");
                System.Diagnostics.Debug.WriteLine("[UserStatsViewModel] GoBackAsync: Navigation to userprofile successful");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserStatsViewModel] GoBackAsync error: {ex.Message}");
                
                // Fallback : essayer avec le service de navigation
                try
                {
                    await _navigationService.GoBackAsync();
                    System.Diagnostics.Debug.WriteLine("[UserStatsViewModel] GoBackAsync: NavigationService fallback successful");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[UserStatsViewModel] GoBackAsync fallback error: {fallbackEx.Message}");
                }
            }
        }
        

        // Computed properties for UI binding
        public string ValidationRate
        {
            get
            {
                if (UserStats == null || UserStats.TotalSpots == 0)
                    return "0%";

                var rate = (double)UserStats.ValidatedSpots / UserStats.TotalSpots * 100;
                return $"{rate:F1}%";
            }
        }

        public string AverageDepthDisplay => UserStats?.AverageDepth != null 
            ? $"{UserStats.AverageDepth:F1}m" 
            : "N/A";

        public string MaxDepthDisplay => UserStats?.MaxDepth != null 
            ? $"{UserStats.MaxDepth:F1}m" 
            : "N/A";

        public string LastActivityDisplay => UserStats?.LastActivity?.ToString("MMM dd, yyyy") ?? "Unknown";

        public string LastSpotCreatedDisplay => UserStats?.LastSpotCreated?.ToString("MMM dd, yyyy") ?? "None";

        public string ExpertiseLevelDisplay => UserStats?.ExpertiseLevel?.ToString() ?? "Not Set";

        public string ContributionLevel
        {
            get
            {
                if (UserStats == null) return "Nouveau Membre";
                
                var validatedSpots = UserStats.ValidatedSpots;
                
                // Pour les administrateurs, affichage spécial
                if (CurrentUser?.AccountType == AccountType.Administrator)
                {
                    return validatedSpots switch
                    {
                        >= 50 => "Administrateur Expert",
                        >= 20 => "Administrateur Expérimenté", 
                        >= 10 => "Administrateur Actif",
                        >= 1 => "Administrateur Contributeur",
                        _ => "Administrateur"
                    };
                }
                
                // Pour les modérateurs, système basé sur les récompenses du cahier des charges
                if (CurrentUser?.AccountType == AccountType.ExpertModerator)
                {
                    return validatedSpots switch
                    {
                        >= 100 => "Modérateur Légendaire",
                        >= 50 => "Modérateur Expert",
                        >= 25 => "Modérateur Confirmé", 
                        >= 10 => "Modérateur Actif",
                        >= 1 => "Nouveau Modérateur",
                        _ => "Modérateur en Formation"
                    };
                }
                
                // Pour les utilisateurs standards
                return validatedSpots switch
                {
                    >= 50 => "Légende de la Communauté",
                    >= 25 => "Expert des Profondeurs",
                    >= 10 => "Plongeur Confirmé",
                    >= 5 => "Explorateur Actif",
                    >= 2 => "Contributeur Régulier",
                    >= 1 => "Découvreur",
                    _ => "Nouveau Membre"
                };
            }
        }

        public string NextLevelInfo
        {
            get
            {
                if (UserStats == null) return "";
                
                var validatedSpots = UserStats.ValidatedSpots;
                
                return validatedSpots switch
                {
                    >= 100 => "Niveau maximum atteint !",
                    >= 50 => $"Plus que {100 - validatedSpots} spots pour devenir Légende des Abysses",
                    >= 25 => $"Plus que {50 - validatedSpots} spots pour devenir Maître Explorateur",
                    >= 10 => $"Plus que {25 - validatedSpots} spots pour devenir Expert des Profondeurs",
                    >= 5 => $"Plus que {10 - validatedSpots} spots pour devenir Plongeur Confirmé",
                    >= 2 => $"Plus que {5 - validatedSpots} spots pour devenir Explorateur Actif",
                    >= 1 => $"Plus que {2 - validatedSpots} spots pour devenir Contributeur Régulier",
                    _ => "Publiez votre premier spot pour devenir Découvreur"
                };
            }
        }

        public double ProgressToNextLevel
        {
            get
            {
                if (UserStats == null) return 0;
                
                var validatedSpots = UserStats.ValidatedSpots;
                
                return validatedSpots switch
                {
                    >= 100 => 1.0, // Niveau max
                    >= 50 => (double)(validatedSpots - 50) / 50,  // 50-100
                    >= 25 => (double)(validatedSpots - 25) / 25,  // 25-50
                    >= 10 => (double)(validatedSpots - 10) / 15,  // 10-25
                    >= 5 => (double)(validatedSpots - 5) / 5,     // 5-10
                    >= 2 => (double)(validatedSpots - 2) / 3,     // 2-5
                    >= 1 => (double)(validatedSpots - 1) / 1,     // 1-2
                    _ => (double)validatedSpots / 1               // 0-1
                };
            }
        }

        // Progression sur l'échelle complète 0-100 pour la barre d'échelons premium
        public double OverallProgress
        {
            get
            {
                if (UserStats == null) return 0;
                var validatedSpots = Math.Min(UserStats.ValidatedSpots, 100);
                return (double)validatedSpots / 100.0;
            }
        }

        public string LevelDescription
        {
            get
            {
                if (UserStats == null) return "";
                
                var validatedSpots = UserStats.ValidatedSpots;
                
                return validatedSpots switch
                {
                    >= 100 => "Vous êtes une véritable légende ! Votre expertise et votre dévouement ont contribué à créer une base de données exceptionnelle.",
                    >= 50 => "Maître incontesté de l'exploration sous-marine, vos contributions sont une référence pour la communauté.",
                    >= 25 => "Votre expertise des fonds marins fait de vous un pilier de la communauté SubExplore.",
                    >= 10 => "Plongeur expérimenté, vos découvertes enrichissent considérablement notre base de spots.",
                    >= 5 => "Explorateur passionné, vous participez activement à l'expansion de notre communauté.",
                    >= 2 => "Contributeur apprécié, chacune de vos découvertes compte pour la communauté.",
                    >= 1 => "Félicitations pour votre première contribution ! Continuez à explorer et partager.",
                    _ => "Bienvenue dans la communauté SubExplore ! Partagez votre premier spot pour commencer votre aventure."
                };
            }
        }

        public string PremiumStatusInfo
        {
            get
            {
                if (UserStats == null || CurrentUser?.AccountType != AccountType.ExpertModerator) 
                    return "";
                
                var validatedSpots = UserStats.ValidatedSpots;
                
                return validatedSpots switch
                {
                    >= 100 => "🎖️ Premium 12 mois offert ! (Modérateur)",
                    >= 50 => "🏆 Premium 6 mois offert ! (Modérateur)",
                    >= 25 => "🥉 Premium 3 mois offert ! (Modérateur)", 
                    >= 10 => "🥇 Premium 1 mois offert ! (Modérateur)",
                    _ => ""
                };
            }
        }

        public bool HasPremiumReward => CurrentUser?.AccountType == AccountType.ExpertModerator && UserStats?.ValidatedSpots >= 10;

        public string UserStatusDisplay
        {
            get
            {
                if (CurrentUser == null) return "";
                
                return CurrentUser.AccountType switch
                {
                    AccountType.Administrator => "👑 Administrateur",
                    AccountType.ExpertModerator => $"🎯 Modérateur Expert ({CurrentUser.ModeratorSpecialization})",
                    AccountType.VerifiedProfessional => "✅ Professionnel Vérifié",
                    AccountType.Standard => "👤 Utilisateur Standard",
                    _ => "👤 Utilisateur"
                };
            }
        }

        public bool IsUserAdmin => CurrentUser?.AccountType == AccountType.Administrator;
        public bool IsUserModerator => CurrentUser?.AccountType == AccountType.ExpertModerator;

        public bool HasSpots => UserStats?.TotalSpots > 0;

        public bool HasPhotos => UserStats?.TotalPhotos > 0;

        public bool HasCertifications => UserStats?.RecentCertifications?.Count > 0;

        public bool HasSpotsByType => UserStats?.SpotsByType?.Count > 0;

        public string PhotosPerSpotDisplay
        {
            get
            {
                if (UserStats?.TotalSpots == 0) return "0";
                var ratio = (double)(UserStats?.TotalPhotos ?? 0) / (UserStats?.TotalSpots ?? 1);
                return $"{ratio:F1}";
            }
        }
    }
}