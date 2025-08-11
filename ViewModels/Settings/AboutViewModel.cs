using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.ViewModels.Base;
using SubExplore.Services.Interfaces;
using System.Reflection;

namespace SubExplore.ViewModels.Settings
{
    public partial class AboutViewModel : ViewModelBase
    {
        public AboutViewModel(IDialogService dialogService = null, INavigationService navigationService = null) 
            : base(dialogService, navigationService)
        {
            Title = "À Propos";
            LoadAppInfo();
        }

        [ObservableProperty]
        private string appName = string.Empty;

        [ObservableProperty]
        private string appVersion = string.Empty;

        [ObservableProperty]
        private string buildNumber = string.Empty;

        [ObservableProperty]
        private string platform = string.Empty;

        [ObservableProperty]
        private string framework = string.Empty;

        [ObservableProperty]
        private string developerName = string.Empty;

        [ObservableProperty]
        private string copyright = string.Empty;

        [ObservableProperty]
        private string contactEmail = string.Empty;

        [ObservableProperty]
        private string appDescription = string.Empty;

        [ObservableProperty]
        private string legalDisclaimer = string.Empty;

        private void LoadAppInfo()
        {
            try
            {
                // App basic information
                AppName = "SubExplore";
                AppDescription = "Découvrez et partagez les meilleurs spots de plongée";
                DeveloperName = "NigossFr";
                Copyright = $"© {DateTime.Now.Year} NigossFr. Tous droits réservés.";
                ContactEmail = "contact@subexplore.app";

                // Version information
                var version = AppInfo.Current.Version;
                AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
                BuildNumber = version.Revision.ToString();

                // Platform information
                Platform = DeviceInfo.Current.Platform.ToString();
                Framework = ".NET 8 MAUI";

                // Legal disclaimer
                LegalDisclaimer = "SubExplore est une application communautaire dédiée aux plongeurs. " +
                    "Les informations fournies sont données à titre indicatif et ne remplacent pas " +
                    "l'expertise locale et les vérifications de sécurité nécessaires avant toute plongée.";
            }
            catch (Exception ex)
            {
                // Fallback values in case of error
                AppName = "SubExplore";
                AppVersion = "1.0.0";
                BuildNumber = "1";
                Platform = "Unknown";
                Framework = ".NET MAUI";
                Console.WriteLine($"Error loading app info: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Contact()
        {
            try
            {
                IsLoading = true;
                
                var subject = $"Contact depuis {AppName} v{AppVersion}";
                var body = $"Bonjour,\n\nJe vous contacte depuis l'application {AppName}.\n\n" +
                          $"Informations techniques:\n" +
                          $"- Version: {AppVersion}\n" +
                          $"- Build: {BuildNumber}\n" +
                          $"- Plateforme: {Platform}\n\n" +
                          $"Message:\n";

                var message = new EmailMessage
                {
                    Subject = subject,
                    Body = body,
                    To = new List<string> { ContactEmail }
                };

                await Email.ComposeAsync(message);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erreur", $"Impossible d'ouvrir l'application email: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task PrivacyPolicy()
        {
            try
            {
                IsLoading = true;
                
                // You can either open a web URL or navigate to a local privacy policy page
                var privacyUrl = "https://subexplore.app/privacy-policy";
                await Browser.OpenAsync(privacyUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Erreur", $"Impossible d'ouvrir la politique de confidentialité: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await base.InitializeAsync(parameter);
            // Any additional initialization can be added here
        }
    }
}