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
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Settings
{
    public partial class DatabaseTestViewModel : ViewModelBase
    {
        private readonly IDatabaseService _databaseService;
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

        public DatabaseTestViewModel(IDatabaseService databaseService, ILogger<DatabaseTestViewModel> logger)
        {
            _databaseService = databaseService;
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
    }
}
