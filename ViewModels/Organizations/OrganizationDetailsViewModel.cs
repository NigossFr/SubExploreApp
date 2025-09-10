using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.Navigation;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.Organizations
{
    [ShellRoute("organizationdetails", FriendlyName = "🏢 Détails Organisation", IsVisible = false)]
    public partial class OrganizationDetailsViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ISharingService _sharingService;
        private readonly IConnectivityService? _connectivityService;
        private readonly ILogger<OrganizationDetailsViewModel>? _logger;

        [ObservableProperty]
        private SupabaseOrganization? _organization;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _isError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Détails de l'organisation";

        [ObservableProperty]
        private string _organizationTypeDisplay = string.Empty;


        [ObservableProperty]
        private string _coordinatesDisplay = string.Empty;

        [ObservableProperty]
        private string _addressDisplay = string.Empty;

        [ObservableProperty]
        private string _contactDisplay = string.Empty;

        [ObservableProperty]
        private bool _hasServices = false;

        [ObservableProperty]
        private bool _hasOperatingHours = false;

        [ObservableProperty]
        private string _servicesDisplay = string.Empty;

        [ObservableProperty]
        private string _operatingHoursDisplay = string.Empty;

        // Property change handler for dynamic title updates
        partial void OnOrganizationChanged(SupabaseOrganization? value)
        {
            if (value != null && !string.IsNullOrEmpty(value.Name))
            {
                PageTitle = value.Name;
            }
            else
            {
                PageTitle = "Détails de l'organisation";
            }
        }

        public OrganizationDetailsViewModel(
            ISupabaseApiService supabaseApiService,
            INavigationService navigationService,
            IDialogService dialogService,
            ISharingService sharingService,
            IConnectivityService? connectivityService = null,
            ILogger<OrganizationDetailsViewModel>? logger = null) : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _sharingService = sharingService;
            _connectivityService = connectivityService;
            _logger = logger;

            Title = "Détails de l'Organisation";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                IsLoading = true;
                IsError = false;

                if (parameter is SupabaseOrganization organization)
                {
                    Organization = organization;
                    await LoadOrganizationDetails();
                }
                else if (parameter is int organizationId)
                {
                    await LoadOrganizationById(organizationId);
                }
                else if (parameter is string stringParam && int.TryParse(stringParam, out var idFromString))
                {
                    await LoadOrganizationById(idFromString);
                }
                else
                {
                    if (parameter == null)
                    {
                        IsLoading = false;
                        return;
                    }
                    
                    await _dialogService.ShowAlertAsync("Erreur", "Paramètre de navigation invalide", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OrganizationDetailsViewModel.InitializeAsync");
                IsError = true;
                ErrorMessage = $"Erreur lors du chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadOrganizationById(int organizationId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var organizations = await _supabaseApiService.GetOrganizationsAsync().WaitAsync(cts.Token);
                var targetOrganization = organizations.FirstOrDefault(o => o.Id == organizationId);
                
                if (targetOrganization == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", $"Organisation non trouvée (ID: {organizationId})", "OK");
                    await _navigationService.GoBackAsync();
                    return;
                }
                
                Organization = targetOrganization;
                
                if (Organization != null)
                {
                    await LoadOrganizationDetails();
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de charger les données de l'organisation", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (TimeoutException)
            {
                await _dialogService.ShowAlertAsync("Timeout", 
                    "Le chargement a pris trop de temps. Vérifiez votre connexion réseau.", "OK");
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading organization by ID: {OrganizationId}", organizationId);
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur API Supabase: {ex.Message}", "OK");
                await _navigationService.GoBackAsync();
            }
        }

        private async Task LoadOrganizationDetails()
        {
            if (Organization == null) 
            {
                IsLoading = false;
                return;
            }

            try
            {
                // Format display information
                OrganizationTypeDisplay = GetOrganizationTypeDisplay(Organization.OrganizationType);
                CoordinatesDisplay = $"{Organization.Latitude:F6}, {Organization.Longitude:F6}";
                
                // Format address
                var addressParts = new List<string>();
                if (!string.IsNullOrEmpty(Organization.Address))
                    addressParts.Add(Organization.Address);
                if (!string.IsNullOrEmpty(Organization.PostalCode))
                    addressParts.Add(Organization.PostalCode);
                if (!string.IsNullOrEmpty(Organization.City))
                    addressParts.Add(Organization.City);
                if (!string.IsNullOrEmpty(Organization.Country))
                    addressParts.Add(Organization.Country);
                
                AddressDisplay = string.Join(", ", addressParts);

                // Format contact information
                var contactParts = new List<string>();
                if (!string.IsNullOrEmpty(Organization.Phone))
                    contactParts.Add($"📞 {Organization.Phone}");
                if (!string.IsNullOrEmpty(Organization.Email))
                    contactParts.Add($"📧 {Organization.Email}");
                if (!string.IsNullOrEmpty(Organization.Website))
                    contactParts.Add($"🌐 {Organization.Website}");
                
                ContactDisplay = string.Join(" | ", contactParts);

                // Handle services (JSON field)
                HasServices = !string.IsNullOrEmpty(Organization.Services?.ToString());
                if (HasServices)
                {
                    ServicesDisplay = FormatJsonField(Organization.Services);
                }

                // Handle operating hours (JSON field)
                HasOperatingHours = !string.IsNullOrEmpty(Organization.OperatingHours?.ToString());
                if (HasOperatingHours)
                {
                    OperatingHoursDisplay = FormatJsonField(Organization.OperatingHours);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error formatting organization details");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du formatage: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetOrganizationTypeDisplay(string organizationType)
        {
            return organizationType switch
            {
                "ClubFFESSM" => "Club FFESSM",
                "SCA" => "Structures Commerciales Agréées",
                "FederalBase" => "Base Fédérale",
                "DiveCenter" => "Centre de Plongée",
                _ => organizationType ?? "Non spécifié"
            };
        }


        private string FormatJsonField(object? jsonField)
        {
            if (jsonField == null) return "";
            
            try
            {
                var jsonString = jsonField.ToString();
                if (string.IsNullOrEmpty(jsonString)) return "";
                
                // Simple formatting - could be enhanced to parse actual JSON
                return jsonString.Replace("{", "").Replace("}", "").Replace("\"", "").Replace(",", ", ");
            }
            catch
            {
                return jsonField.ToString() ?? "";
            }
        }

        [RelayCommand]
        private async Task ShareOrganization()
        {
            try
            {
                if (Organization == null) return;

                // Create a simple sharing text
                var shareText = $"🏢 {Organization.Name}\n" +
                               $"📍 {AddressDisplay}\n" +
                               $"🏊 Type: {OrganizationTypeDisplay}\n";
                
                if (!string.IsNullOrEmpty(Organization.Phone))
                    shareText += $"📞 {Organization.Phone}\n";
                
                if (!string.IsNullOrEmpty(Organization.Website))
                    shareText += $"🌐 {Organization.Website}\n";

                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new Microsoft.Maui.ApplicationModel.DataTransfer.ShareTextRequest
                {
                    Text = shareText,
                    Title = "Partager l'organisation"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sharing organization");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du partage : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ShowOnMap()
        {
            try
            {
                if (Organization == null) return;

                // Navigate using Shell navigation to map route
                await Shell.Current.GoToAsync("//map", new Dictionary<string, object>
                {
                    ["organization"] = Organization,
                    ["centerOnOrganization"] = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing organization on map");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'affichage sur la carte : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task CallOrganization()
        {
            try
            {
                if (Organization == null || string.IsNullOrEmpty(Organization.Phone)) return;

                if (Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.IsSupported)
                {
                    Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.Open(Organization.Phone);
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Non supporté", "Les appels téléphoniques ne sont pas supportés sur cet appareil", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calling organization");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'appel : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task OpenWebsite()
        {
            try
            {
                if (Organization == null || string.IsNullOrEmpty(Organization.Website)) return;

                var uri = Organization.Website.StartsWith("http") ? Organization.Website : $"https://{Organization.Website}";
                await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening organization website");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'ouverture du site web : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                
                if (Organization != null)
                {
                    await LoadOrganizationById(Organization.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing organization details");
                IsError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand] 
        public async Task Back()
        {
            await _navigationService.GoBackAsync();
        }
    }
}