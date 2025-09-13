using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.Navigation;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.Businesses
{
    [ShellRoute("businessdetails", FriendlyName = "🏪 Détails Commerce", IsVisible = false)]
    [QueryProperty(nameof(BusinessId), "id")]
    public partial class BusinessDetailsViewModel : ViewModelBase, IQueryAttributable
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ISharingService _sharingService;
        private readonly IConnectivityService? _connectivityService;
        private readonly ILogger<BusinessDetailsViewModel>? _logger;

        [ObservableProperty]
        private SupabaseBusiness? _business;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _isError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Détails du commerce";

        [ObservableProperty]
        private string _businessTypeDisplay = string.Empty;

        [ObservableProperty]
        private string _priceRangeDisplay = string.Empty;

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

        [ObservableProperty]
        private string _paymentMethodsDisplay = string.Empty;

        [ObservableProperty]
        private bool _hasPaymentMethods = false;

        // QueryProperty must be a public property, not ObservableProperty
        public string BusinessId { get; set; } = string.Empty;

        // Property change handler for dynamic title updates
        partial void OnBusinessChanged(SupabaseBusiness? value)
        {
            if (value != null && !string.IsNullOrEmpty(value.Name))
            {
                PageTitle = value.Name;
            }
            else
            {
                PageTitle = "Détails du commerce";
            }
        }

        public BusinessDetailsViewModel(
            ISupabaseApiService supabaseApiService,
            INavigationService navigationService,
            IDialogService dialogService,
            ISharingService sharingService,
            IConnectivityService? connectivityService = null,
            ILogger<BusinessDetailsViewModel>? logger = null) : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _sharingService = sharingService;
            _connectivityService = connectivityService;
            _logger = logger;

            Title = "Détails du Commerce";
        }

        // ✅ IQueryAttributable implementation for Shell navigation
        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _logger?.LogInformation("ApplyQueryAttributes called with {Count} parameters", query.Count);
            
            if (query.TryGetValue("id", out var idValue))
            {
                BusinessId = idValue?.ToString() ?? string.Empty;
                _logger?.LogInformation("ApplyQueryAttributes: BusinessId set to {BusinessId}", BusinessId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel ApplyQueryAttributes: BusinessId set to '{BusinessId}'");
                
                // 🚀 CRITICAL FIX: Initialize immediately when QueryProperty is received
                if (!string.IsNullOrEmpty(BusinessId))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel ApplyQueryAttributes: Triggering initialization with BusinessId '{BusinessId}'");
                    await InitializeAsync();
                }
            }
            
            if (query.TryGetValue("businessId", out var bizIdValue))
            {
                BusinessId = bizIdValue?.ToString() ?? string.Empty;
                _logger?.LogInformation("ApplyQueryAttributes: BusinessId (alt) set to {BusinessId}", BusinessId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel ApplyQueryAttributes: BusinessId (alt) set to '{BusinessId}'");
                
                // 🚀 CRITICAL FIX: Initialize immediately when QueryProperty is received (alt)
                if (!string.IsNullOrEmpty(BusinessId))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel ApplyQueryAttributes: Triggering initialization (alt) with BusinessId '{BusinessId}'");
                    await InitializeAsync();
                }
            }
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                IsLoading = true;
                IsError = false;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 1 - Starting initialization");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 2 - Parameter is: {parameter?.GetType().Name ?? "null"} = {parameter}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 3 - BusinessId QueryProperty: '{BusinessId}'");

                // Check QueryProperty first (Shell navigation)
                if (!string.IsNullOrEmpty(BusinessId))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 4A - Found BusinessId from QueryProperty: '{BusinessId}'");
                    
                    if (int.TryParse(BusinessId, out var queryBusinessId))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 4B - Successfully parsed BusinessId as integer: {queryBusinessId}");
                        _logger?.LogInformation("Found BusinessId from QueryProperty as integer: {BusinessId}", queryBusinessId);
                        await LoadBusinessById(queryBusinessId);
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 4C - Failed to parse BusinessId as integer: '{BusinessId}'");
                        _logger?.LogWarning("BusinessId from QueryProperty is not a valid integer: {BusinessId}", BusinessId);
                    }
                }

                // Handle direct parameter navigation (legacy/programmatic)
                if (parameter is SupabaseBusiness business)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 5A - Parameter is SupabaseBusiness object");
                    Business = business;
                    await LoadBusinessDetails();
                }
                else if (parameter is int businessId)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 5B - Parameter is integer: {businessId}");
                    await LoadBusinessById(businessId);
                }
                else if (parameter is string stringParam && int.TryParse(stringParam, out var idFromString))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 5C - Parameter is string that parses as integer: {idFromString}");
                    await LoadBusinessById(idFromString);
                }
                else
                {
                    if (parameter == null && string.IsNullOrEmpty(BusinessId))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 6A - No parameters found");
                        IsLoading = false;
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BusinessDetailsViewModel InitializeAsync: STEP 6B - Invalid parameter type");
                    _logger?.LogError("Invalid navigation parameter: {Parameter}", parameter);
                    await _dialogService.ShowAlertAsync("Erreur", "Paramètre de navigation invalide", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in BusinessDetailsViewModel.InitializeAsync");
                IsError = true;
                ErrorMessage = $"Erreur lors du chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBusinessById(int businessId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var businesses = await _supabaseApiService.GetBusinessesAsync().WaitAsync(cts.Token);
                var targetBusiness = businesses.FirstOrDefault(b => b.Id == businessId);
                
                if (targetBusiness == null)
                {
                    await _dialogService.ShowAlertAsync("Erreur", $"Commerce non trouvé (ID: {businessId})", "OK");
                    await _navigationService.GoBackAsync();
                    return;
                }
                
                Business = targetBusiness;
                
                if (Business != null)
                {
                    await LoadBusinessDetails();
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de charger les données du commerce", "OK");
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
                _logger?.LogError(ex, "Error loading business by ID: {BusinessId}", businessId);
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur API Supabase: {ex.Message}", "OK");
                await _navigationService.GoBackAsync();
            }
        }

        private async Task LoadBusinessDetails()
        {
            if (Business == null) 
            {
                IsLoading = false;
                return;
            }

            try
            {
                // Format display information
                BusinessTypeDisplay = GetBusinessTypeDisplay(Business.BusinessType);
                PriceRangeDisplay = GetPriceRangeDisplay(Business.PriceRange);
                CoordinatesDisplay = $"{Business.Latitude:F6}, {Business.Longitude:F6}";
                
                // Format address
                var addressParts = new List<string>();
                if (!string.IsNullOrEmpty(Business.Address))
                    addressParts.Add(Business.Address);
                if (!string.IsNullOrEmpty(Business.PostalCode))
                    addressParts.Add(Business.PostalCode);
                if (!string.IsNullOrEmpty(Business.City))
                    addressParts.Add(Business.City);
                if (!string.IsNullOrEmpty(Business.Country))
                    addressParts.Add(Business.Country);
                
                AddressDisplay = string.Join(", ", addressParts);

                // Format contact information
                var contactParts = new List<string>();
                if (!string.IsNullOrEmpty(Business.Phone))
                    contactParts.Add($"📞 {Business.Phone}");
                if (!string.IsNullOrEmpty(Business.Email))
                    contactParts.Add($"📧 {Business.Email}");
                if (!string.IsNullOrEmpty(Business.Website))
                    contactParts.Add($"🌐 {Business.Website}");
                
                ContactDisplay = string.Join(" | ", contactParts);

                // Handle services (JSON field)
                HasServices = !string.IsNullOrEmpty(Business.Services?.ToString());
                if (HasServices)
                {
                    ServicesDisplay = FormatJsonField(Business.Services);
                }

                // Handle operating hours (JSON field)
                HasOperatingHours = !string.IsNullOrEmpty(Business.OperatingHours?.ToString());
                if (HasOperatingHours)
                {
                    OperatingHoursDisplay = FormatJsonField(Business.OperatingHours);
                }

                // Handle payment methods (JSON field)
                HasPaymentMethods = !string.IsNullOrEmpty(Business.PaymentMethods?.ToString());
                if (HasPaymentMethods)
                {
                    PaymentMethodsDisplay = FormatJsonField(Business.PaymentMethods);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error formatting business details");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du formatage: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetBusinessTypeDisplay(string businessType)
        {
            return businessType switch
            {
                "DiveShop" => "Magasin de plongée",
                "EquipmentRental" => "Location de matériel",
                "BoatCharter" => "Service bateau",
                _ => businessType ?? "Non spécifié"
            };
        }


        private string GetPriceRangeDisplay(string priceRange)
        {
            return priceRange switch
            {
                "Budget" => "€ Économique",
                "MidRange" => "€€ Gamme moyenne",
                "Premium" => "€€€ Haut de gamme",
                _ => "Non spécifié"
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
        private async Task ShareBusiness()
        {
            try
            {
                if (Business == null) return;

                // Create a simple sharing text
                var shareText = $"🏪 {Business.Name}\n" +
                               $"📍 {AddressDisplay}\n" +
                               $"🏬 Type: {BusinessTypeDisplay}\n" +
                               $"💰 Prix: {PriceRangeDisplay}\n";
                
                if (!string.IsNullOrEmpty(Business.Phone))
                    shareText += $"📞 {Business.Phone}\n";
                
                if (!string.IsNullOrEmpty(Business.Website))
                    shareText += $"🌐 {Business.Website}\n";

                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new Microsoft.Maui.ApplicationModel.DataTransfer.ShareTextRequest
                {
                    Text = shareText,
                    Title = "Partager le commerce"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sharing business");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du partage : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ShowOnMap()
        {
            try
            {
                if (Business == null) return;

                // Navigate using Shell navigation to map route
                await Shell.Current.GoToAsync("//map", new Dictionary<string, object>
                {
                    ["business"] = Business,
                    ["centerOnBusiness"] = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing business on map");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'affichage sur la carte : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task CallBusiness()
        {
            try
            {
                if (Business == null || string.IsNullOrEmpty(Business.Phone)) return;

                if (Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.IsSupported)
                {
                    Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.Open(Business.Phone);
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Non supporté", "Les appels téléphoniques ne sont pas supportés sur cet appareil", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calling business");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors de l'appel : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task OpenWebsite()
        {
            try
            {
                if (Business == null || string.IsNullOrEmpty(Business.Website)) return;

                var uri = Business.Website.StartsWith("http") ? Business.Website : $"https://{Business.Website}";
                await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening business website");
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
                
                if (Business != null)
                {
                    await LoadBusinessById(Business.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing business details");
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