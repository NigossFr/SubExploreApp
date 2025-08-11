using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Implementations;
using SubExplore.Migrations;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Settings
{
    public partial class DatabaseTestViewModel : ViewModelBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly DatabaseDiagnosticService _diagnosticService;
        private readonly SpotTypeMigrationService _migrationService;
        private readonly UpdateActivityCategoryStructure _categoryMigrationService;
        private readonly SpotTypeDiagnosticService _spotTypeDiagnosticService;
        private readonly ILogger<DatabaseTestViewModel> _logger;

        [ObservableProperty]
        private bool _canConnect;

        [ObservableProperty]
        private bool _isDatabaseCreated;

        [ObservableProperty]
        private bool _isDataSeeded;

        [ObservableProperty]
        private string _logMessages = string.Empty;

        [ObservableProperty]
        private bool _isSpotTypesCleanedUp;

        [ObservableProperty]
        private bool _isRealSpotsImported;

        [ObservableProperty]
        private bool _isCategoryMigrationExecuted;

        [ObservableProperty]
        private bool _isCategoryStructureMigrated;

        [ObservableProperty]
        private bool _isSpotTypesRepaired;

        public DatabaseTestViewModel(IDatabaseService databaseService, DatabaseDiagnosticService diagnosticService, SpotTypeMigrationService migrationService, UpdateActivityCategoryStructure categoryMigrationService, SpotTypeDiagnosticService spotTypeDiagnosticService, ILogger<DatabaseTestViewModel> logger)
        {
            _databaseService = databaseService;
            _diagnosticService = diagnosticService;
            _migrationService = migrationService;
            _categoryMigrationService = categoryMigrationService;
            _spotTypeDiagnosticService = spotTypeDiagnosticService;
            _logger = logger;
            Title = "Test Base de Données";
        }

        public override async Task InitializeAsync(IDictionary<string, object> parameters)
        {
            await TestConnectionAsync();
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages = "Test de connexion en cours...\n";
                CanConnect = await _databaseService.TestConnectionAsync();

                if (CanConnect)
                {
                    LogMessages += "✅ Connexion à la base de données établie avec succès\n";
                }
                else
                {
                    LogMessages += "❌ Impossible de se connecter à la base de données\n";
                    ShowError("Échec de connexion à la base de données");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de connexion");
                LogMessages += $"❌ Erreur: {ex.Message}\n";
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task EnsureDatabaseCreatedAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "Création/vérification de la base de données...\n";

                IsDatabaseCreated = await _databaseService.EnsureDatabaseCreatedAsync();

                if (IsDatabaseCreated)
                {
                    LogMessages += "✅ Base de données créée ou déjà existante\n";
                }
                else
                {
                    LogMessages += "❌ Échec de la création de la base de données\n";
                    ShowError("Échec de la création de la base de données");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la base de données");
                LogMessages += $"❌ Erreur: {ex.Message}\n";
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SeedDatabaseAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "Initialisation des données...\n";

                IsDataSeeded = await _databaseService.SeedDatabaseAsync();

                if (IsDataSeeded)
                {
                    LogMessages += "✅ Données initialisées avec succès\n";
                }
                else
                {
                    LogMessages += "❌ Échec de l'initialisation des données\n";
                    ShowError("Échec de l'initialisation des données");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des données");
                LogMessages += $"❌ Erreur: {ex.Message}\n";
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CleanupSpotTypesAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "Nettoyage des types de spots obsolètes...\n";

                IsSpotTypesCleanedUp = await _databaseService.CleanupSpotTypesAsync();

                if (IsSpotTypesCleanedUp)
                {
                    LogMessages += "✅ Types de spots nettoyés avec succès\n";
                    LogMessages += "Seuls les 5 types requis sont maintenant disponibles :\n";
                    LogMessages += "- Apnée\n- Photo sous-marine\n- Plongée récréative\n- Plongée technique\n- Randonnée sous marine\n";
                }
                else
                {
                    LogMessages += "❌ Échec du nettoyage des types de spots\n";
                    ShowError("Échec du nettoyage des types de spots");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des types de spots");
                LogMessages += $"❌ Erreur: {ex.Message}\n";
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ImportRealSpotsAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "Import des spots réels en cours...\n";

                IsRealSpotsImported = await _databaseService.ImportRealSpotsAsync();

                if (IsRealSpotsImported)
                {
                    LogMessages += "✅ Spots réels importés avec succès\n";
                    LogMessages += "Les nouveaux spots sont maintenant disponibles sur la carte\n";
                }
                else
                {
                    LogMessages += "❌ Échec de l'import des spots réels\n";
                    LogMessages += "Vérifiez que le fichier Data/real_spots.json existe et est bien formaté\n";
                    ShowError("Échec de l'import des spots réels");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'import des spots réels");
                LogMessages += $"❌ Erreur: {ex.Message}\n";
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ShowDatabaseDiagnosticsAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "Récupération des diagnostics de base de données...\n";

                var diagnostics = await _databaseService.GetDatabaseDiagnosticsAsync();
                LogMessages += diagnostics + "\n";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des diagnostics");
                LogMessages += $"❌ Erreur: {ex.Message}\n";
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RunUltraDeepDiagnosticAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🚨 Démarrage du diagnostic ultra-profond de la base de données...\n";

                // Get the service provider from DI
                var services = Application.Current?.Handler?.MauiContext?.Services;
                if (services == null)
                {
                    LogMessages += "❌ Cannot access service provider\n";
                    return;
                }
                
                // Run the ultra-deep diagnostic
                await DatabaseDiagnostic.RunUltraDeepDatabaseTestAsync(services);
                
                LogMessages += "✅ Diagnostic ultra-profond terminé - voir les logs Debug pour les détails\n";
                LogMessages += "💡 Conseil: Vérifiez la fenêtre Debug/Output de Visual Studio pour les résultats détaillés\n";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic ultra-profond");
                LogMessages += $"❌ Erreur diagnostic ultra-profond: {ex.Message}\n";
                if (ex.InnerException != null)
                {
                    LogMessages += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                ShowError($"Erreur diagnostic: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RunDetailedDatabaseDiagnosticAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔍 Démarrage du diagnostic détaillé de la base de données...\n";

                var diagnostics = await _diagnosticService.GetDetailedDatabaseStatusAsync();
                LogMessages += diagnostics + "\n";
                
                LogMessages += "✅ Diagnostic détaillé terminé\n";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic détaillé");
                LogMessages += $"❌ Erreur diagnostic détaillé: {ex.Message}\n";
                ShowError($"Erreur diagnostic: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ForceRecreateDataAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔄 Démarrage de la recréation forcée des données...\n";
                LogMessages += "⚠️ ATTENTION: Cette opération va supprimer TOUTES les données existantes!\n";

                var result = await _diagnosticService.ForceDataRecreationAsync();
                
                if (result)
                {
                    LogMessages += "✅ Recréation forcée terminée avec succès\n";
                    LogMessages += "🎯 La base de données contient maintenant les 8 nouveaux types de spots\n";
                    
                    // Refresh all the status flags
                    await TestConnectionAsync();
                    await SeedDatabaseAsync();
                    await ShowDatabaseDiagnosticsAsync();
                }
                else
                {
                    LogMessages += "❌ Échec de la recréation forcée\n";
                    ShowError("Échec de la recréation forcée des données");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recréation forcée");
                LogMessages += $"❌ Erreur recréation forcée: {ex.Message}\n";
                ShowError($"Erreur recréation: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ExecuteSpotTypeMigrationAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🚀 Démarrage de la migration EF Core vers la nouvelle structure...\n";

                var result = await _migrationService.ExecuteMigrationAsync();
                
                if (result)
                {
                    LogMessages += "✅ Migration EF Core terminée avec succès!\n";
                    LogMessages += "🎯 La base de données contient maintenant les 8 nouveaux types de spots\n";
                    
                    // Afficher le statut de la migration
                    var status = await _migrationService.GetMigrationStatusAsync();
                    LogMessages += status + "\n";
                    
                    // Refresh les autres indicateurs
                    await TestConnectionAsync();
                    await ShowDatabaseDiagnosticsAsync();
                }
                else
                {
                    LogMessages += "❌ Échec de la migration EF Core\n";
                    ShowError("Échec de la migration EF Core");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la migration EF Core");
                LogMessages += $"❌ Erreur migration EF Core: {ex.Message}\n";
                ShowError($"Erreur migration: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CheckMigrationStatusAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔍 Vérification de l'état de la migration...\n";

                var status = await _migrationService.GetMigrationStatusAsync();
                LogMessages += status + "\n";
                
                LogMessages += "✅ Vérification terminée\n";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du statut");
                LogMessages += $"❌ Erreur vérification: {ex.Message}\n";
                ShowError($"Erreur vérification: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ExecuteCategoryMappingMigrationAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔧 Exécution de la migration FixSpotTypeCategoryMapping...\n";

                IsCategoryMigrationExecuted = await _databaseService.ExecuteSpotTypeCategoryMappingMigrationAsync();

                if (IsCategoryMigrationExecuted)
                {
                    LogMessages += "✅ Migration de catégories exécutée avec succès!\n";
                    LogMessages += "🎯 Les catégories des types de spots ont été mises à jour\n";
                    
                    // Afficher les diagnostics après migration
                    await ShowDatabaseDiagnosticsAsync();
                }
                else
                {
                    LogMessages += "❌ Échec de la migration de catégories\n";
                    ShowError("Échec de la migration de catégories");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la migration de catégories");
                LogMessages += $"❌ Erreur migration catégories: {ex.Message}\n";
                ShowError($"Erreur migration: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AnalyzeFilteringIssuesAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔍 Analyse des problèmes de filtrage en cours...\n";

                var analysis = await _databaseService.AnalyzeFilteringIssuesAsync();
                LogMessages += analysis + "\n";

                LogMessages += "✅ Analyse de filtrage terminée!\n";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse de filtrage");
                LogMessages += $"❌ Erreur analyse filtrage: {ex.Message}\n";
                ShowError($"Erreur analyse: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task FixActivityCategoryStructureAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔧 Correction de la structure ActivityCategory en cours...\n";
                LogMessages += "⚠️ Cette opération va corriger le problème 'Boutiques dans Structures'\n";

                await _categoryMigrationService.ExecuteAsync();
                
                LogMessages += "✅ Structure ActivityCategory corrigée avec succès!\n";
                LogMessages += "🎯 Boutiques séparées des Structures - problème résolu\n";
                
                // Obtenir un rapport de statut
                var report = await _categoryMigrationService.GetStatusReportAsync();
                LogMessages += report + "\n";
                
                IsCategoryStructureMigrated = true;
                
                // Refresh les diagnostics
                await ShowDatabaseDiagnosticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la correction de la structure ActivityCategory");
                LogMessages += $"❌ Erreur correction structure: {ex.Message}\n";
                ShowError($"Erreur correction: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DiagnoseSpotTypesAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔍 Diagnostic complet des types de spots...\n";

                var diagnostic = await _spotTypeDiagnosticService.DiagnoseSpotTypesAsync();
                LogMessages += diagnostic + "\n";
                
                LogMessages += "✅ Diagnostic terminé!\n";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic des types de spots");
                LogMessages += $"❌ Erreur diagnostic: {ex.Message}\n";
                ShowError($"Erreur diagnostic: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RepairSpotTypesAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                LogMessages += "🔧 Réparation des types de spots en cours...\n";
                LogMessages += "⚠️ Cette opération va corriger les doublons et réparer les liens\n";

                var success = await _spotTypeDiagnosticService.RepairSpotTypesAsync();
                
                if (success)
                {
                    LogMessages += "✅ Réparation terminée avec succès!\n";
                    LogMessages += "🎯 Les doublons ont été supprimés et les spots réattribués\n";
                    
                    IsSpotTypesRepaired = true;
                    
                    // Refaire un diagnostic pour vérifier
                    var diagnostic = await _spotTypeDiagnosticService.DiagnoseSpotTypesAsync();
                    LogMessages += "\n" + diagnostic + "\n";
                }
                else
                {
                    LogMessages += "❌ Échec de la réparation\n";
                    ShowError("Échec de la réparation des types de spots");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réparation des types de spots");
                LogMessages += $"❌ Erreur réparation: {ex.Message}\n";
                ShowError($"Erreur réparation: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
