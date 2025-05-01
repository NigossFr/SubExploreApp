using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
