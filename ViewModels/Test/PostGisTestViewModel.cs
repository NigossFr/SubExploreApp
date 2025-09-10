using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Implementations;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Test
{
    public partial class PostGisTestViewModel : ViewModelBase
    {
        private readonly PostGisTestService _postGisTestService;

        [ObservableProperty]
        private string testResults = "Prêt pour le test PostGIS...";

        [ObservableProperty]
        private bool isTestRunning = false;

        [ObservableProperty]
        private bool hasTestResults = false;

        public PostGisTestViewModel(
            PostGisTestService postGisTestService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
            _postGisTestService = postGisTestService;
            Title = "Test PostGIS";
        }

        [RelayCommand]
        public async Task RunPostGisTestAsync()
        {
            if (IsTestRunning) return;

            try
            {
                IsTestRunning = true;
                TestResults = "🧪 Démarrage des tests PostGIS...\n";

                var result = await _postGisTestService.TestAllPostGisFunctionsAsync();
                
                TestResults = result.GetSummary();
                HasTestResults = true;

                if (result.OverallSuccess)
                {
                    await DialogService.ShowAlertAsync("Test réussi", "Tous les tests PostGIS ont réussi !", "OK");
                }
                else
                {
                    var errors = new List<string>();
                    if (!result.PracticeSpotsSuccess && !string.IsNullOrEmpty(result.PracticeSpotsError))
                        errors.Add($"PracticeSpots: {result.PracticeSpotsError}");
                    if (!result.OrganizationsSuccess && !string.IsNullOrEmpty(result.OrganizationsError))
                        errors.Add($"Organizations: {result.OrganizationsError}");
                    if (!result.BusinessesSuccess && !string.IsNullOrEmpty(result.BusinessesError))
                        errors.Add($"Businesses: {result.BusinessesError}");

                    await DialogService.ShowAlertAsync("Tests partiellement échoués", string.Join("\n", errors), "OK");
                }
            }
            catch (Exception ex)
            {
                TestResults = $"❌ ERREUR GÉNÉRALE:\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}";
                await DialogService.ShowAlertAsync("Erreur", $"Erreur lors du test: {ex.Message}", "OK");
            }
            finally
            {
                IsTestRunning = false;
            }
        }

        [RelayCommand]
        public async Task RunFilterTestAsync()
        {
            if (IsTestRunning) return;

            try
            {
                IsTestRunning = true;
                TestResults = "🔍 Test avec filtres en cours...\n";

                var filterResults = await _postGisTestService.TestWithFiltersAsync();
                TestResults = $"🔍 Tests avec filtres:\n{filterResults}";
                HasTestResults = true;
            }
            catch (Exception ex)
            {
                TestResults = $"❌ ERREUR FILTRES:\n{ex.Message}";
                await DialogService.ShowAlertAsync("Erreur", $"Erreur lors du test avec filtres: {ex.Message}", "OK");
            }
            finally
            {
                IsTestRunning = false;
            }
        }

        [RelayCommand]
        public void ClearResults()
        {
            TestResults = "Prêt pour le test PostGIS...";
            HasTestResults = false;
        }
    }
}