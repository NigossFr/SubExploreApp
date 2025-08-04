using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.ViewModels.Base;
using Microsoft.Maui.Graphics;

namespace SubExplore.ViewModels.Admin
{
    public partial class SpotDiagnosticViewModel : ViewModelBase
    {
        private readonly ILogger<SpotDiagnosticViewModel> _logger;
        private readonly ISpotRepository _spotRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authenticationService;

        [ObservableProperty]
        private string _migrationStatus = "Non vérifié";

        [ObservableProperty]
        private string _currentUserInfo = "Non connecté";

        [ObservableProperty]
        private string _spotsSummary = "Chargement...";

        [ObservableProperty]
        private ObservableCollection<SpotDiagnosticInfo> _spots = new();

        public SpotDiagnosticViewModel(
            ILogger<SpotDiagnosticViewModel> logger,
            ISpotRepository spotRepository,
            IUserRepository userRepository,
            IAuthenticationService authenticationService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _logger = logger;
            _spotRepository = spotRepository;
            _userRepository = userRepository;
            _authenticationService = authenticationService;
            Title = "Diagnostic des Spots";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await RefreshDataAsync();
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await RefreshDataAsync();
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("[SpotDiagnostic] Starting data refresh");

                // Check current user
                await CheckCurrentUser();

                // Load all spots
                await LoadAllSpots();

                // Update summary
                await UpdateSpotsSummary();

                _logger.LogInformation("[SpotDiagnostic] Data refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotDiagnostic] Error during data refresh");
                await ShowAlertAsync("Erreur", $"Erreur lors du rafraîchissement: {ex.Message}", "D'accord");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckCurrentUser()
        {
            try
            {
                if (_authenticationService.IsAuthenticated && _authenticationService.CurrentUser != null)
                {
                    var user = _authenticationService.CurrentUser;
                    CurrentUserInfo = $"✅ {user.FirstName} {user.LastName} ({user.Email})\\n" +
                                    $"Rôle: {user.AccountType}\\n" +
                                    $"ID: {user.Id}";
                    _logger.LogInformation("[SpotDiagnostic] Current user: {UserId} - {AccountType}", user.Id, user.AccountType);
                }
                else
                {
                    CurrentUserInfo = "❌ Aucun utilisateur connecté";
                    _logger.LogWarning("[SpotDiagnostic] No authenticated user found");
                }
            }
            catch (Exception ex)
            {
                CurrentUserInfo = $"❌ Erreur: {ex.Message}";
                _logger.LogError(ex, "[SpotDiagnostic] Error checking current user");
            }
        }

        private async Task LoadAllSpots()
        {
            try
            {
                _logger.LogInformation("[SpotDiagnostic] Loading all spots from database");
                
                // Get all spots regardless of status
                var allSpots = await _spotRepository.GetAllAsync();
                _logger.LogInformation("[SpotDiagnostic] Retrieved {Count} total spots", allSpots?.Count() ?? 0);

                Spots.Clear();

                if (allSpots?.Any() == true)
                {
                    foreach (var spot in allSpots.OrderByDescending(s => s.CreatedAt))
                    {
                        // Get creator info
                        var creator = await _userRepository.GetByIdAsync(spot.CreatorId);
                        
                        var diagnosticInfo = new SpotDiagnosticInfo
                        {
                            Name = spot.Name,
                            Details = $"Statut: {spot.ValidationStatus} (#{(int)spot.ValidationStatus})\\n" +
                                     $"Position: {spot.Latitude:F6}, {spot.Longitude:F6}\\n" +
                                     $"Créé: {spot.CreatedAt:dd/MM/yyyy HH:mm}",
                            CreatorInfo = creator != null 
                                ? $"Créateur: {creator.FirstName} {creator.LastName} ({creator.AccountType})"
                                : $"Créateur ID: {spot.CreatorId} (utilisateur introuvable)",
                            StatusColor = GetStatusColor(spot.ValidationStatus),
                            ValidationStatus = spot.ValidationStatus
                        };

                        Spots.Add(diagnosticInfo);
                        
                        _logger.LogInformation("[SpotDiagnostic] Spot: {Name} - Status: {Status} ({StatusValue}) - Creator: {CreatorType}", 
                            spot.Name, spot.ValidationStatus, (int)spot.ValidationStatus, creator?.AccountType.ToString() ?? "Unknown");
                    }
                }
                else
                {
                    _logger.LogWarning("[SpotDiagnostic] No spots found in database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotDiagnostic] Error loading spots");
                await ShowAlertAsync("Erreur", $"Erreur lors du chargement des spots: {ex.Message}", "D'accord");
            }
        }

        private Color GetStatusColor(SpotValidationStatus status)
        {
            return status switch
            {
                SpotValidationStatus.Approved => Colors.LightGreen,
                SpotValidationStatus.Pending => Colors.LightYellow,
                SpotValidationStatus.UnderReview => Colors.LightBlue,
                SpotValidationStatus.NeedsRevision => Colors.Orange,
                SpotValidationStatus.SafetyReview => Colors.Pink,
                SpotValidationStatus.Rejected => Colors.LightCoral,
                SpotValidationStatus.Draft => Colors.LightGray,
                SpotValidationStatus.Archived => Colors.DarkGray,
                _ => Colors.White
            };
        }

        private async Task UpdateSpotsSummary()
        {
            try
            {
                var statusCounts = Spots.GroupBy(s => s.ValidationStatus)
                    .ToDictionary(g => g.Key, g => g.Count());

                var summary = "📊 Répartition par statut:\\n";
                foreach (SpotValidationStatus status in Enum.GetValues<SpotValidationStatus>())
                {
                    var count = statusCounts.GetValueOrDefault(status, 0);
                    summary += $"• {status}: {count} ({(int)status})\\n";
                }

                summary += $"\\n🎯 Total: {Spots.Count} spots";
                
                // Check specifically for approved spots
                var approvedCount = statusCounts.GetValueOrDefault(SpotValidationStatus.Approved, 0);
                summary += $"\\n✅ Spots visibles sur carte: {approvedCount}";

                SpotsSummary = summary;

                _logger.LogInformation("[SpotDiagnostic] Summary updated - Total: {Total}, Approved: {Approved}", 
                    Spots.Count, approvedCount);
            }
            catch (Exception ex)
            {
                SpotsSummary = $"Erreur: {ex.Message}";
                _logger.LogError(ex, "[SpotDiagnostic] Error updating summary");
            }
        }

        [RelayCommand]
        private async Task ForceMigration()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("[SpotDiagnostic] Starting forced migration");

                await ShowToastAsync("Migration en cours...");

                // Force migration
                await Helpers.SpotValidationMigrationHelper.ApplySpotValidationMigrationAsync();
                
                MigrationStatus = "✅ Migration forcée exécutée avec succès";
                await ShowToastAsync("Migration terminée");

                // Refresh data
                await RefreshDataAsync();

                _logger.LogInformation("[SpotDiagnostic] Forced migration completed");
            }
            catch (Exception ex)
            {
                MigrationStatus = $"❌ Erreur migration: {ex.Message}";
                _logger.LogError(ex, "[SpotDiagnostic] Error during forced migration");
                await ShowAlertAsync("Erreur", $"Erreur lors de la migration: {ex.Message}", "D'accord");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreatePendingSpot()
        {
            try
            {
                IsLoading = true;
                await ShowToastAsync("Création des données de test...");

                // Force the creation of test users and pending spots
                var migrationService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetService<SpotMigrationService>(Application.Current.Handler.MauiContext.Services);
                
                if (migrationService != null)
                {
                    await migrationService.ApplySpotValidationMigrationAsync();
                    await ShowToastAsync("✅ Données de test créées avec succès!");
                }
                else
                {
                    await ShowAlertAsync("Erreur", "SpotMigrationService non trouvé", "D'accord");
                }

                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpotDiagnostic] Error creating pending spot");
                await ShowAlertAsync("Erreur", $"Erreur: {ex.Message}", "D'accord");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class SpotDiagnosticInfo
    {
        public string Name { get; set; } = "";
        public string Details { get; set; } = "";
        public string CreatorInfo { get; set; } = "";
        public Color StatusColor { get; set; } = Colors.White;
        public SpotValidationStatus ValidationStatus { get; set; }
    }
}