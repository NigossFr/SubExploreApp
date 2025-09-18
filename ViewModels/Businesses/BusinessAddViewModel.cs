// ========================================
// BUSINESS ADD VIEWMODEL
// ========================================
// ViewModel pour l'ajout de commerces/boutiques

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Businesses
{
    public partial class BusinessAddViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _apiService;
        private readonly ILocationService _locationService;
        private readonly ISimpleAuthenticationService _authService;
        private readonly ILogger<BusinessAddViewModel> _logger;

        // Propriétés de base
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private double _latitude = 43.6047; // Marseille coordinates as default

        [ObservableProperty]
        private double _longitude = 1.4442; // Toulouse area coordinates

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private string _city = string.Empty;

        [ObservableProperty]
        private string _postalCode = string.Empty;

        [ObservableProperty]
        private string _country = "France";

        // Contact
        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _website = string.Empty;

        // Type de commerce
        [ObservableProperty]
        private BusinessType _selectedBusinessType = BusinessType.DiveShop;

        public ObservableCollection<BusinessType> BusinessTypes { get; } = new();

        // Services et prix
        [ObservableProperty]
        private string _services = string.Empty;

        [ObservableProperty]
        private string _priceRange = "€";

        [ObservableProperty]
        private ObservableCollection<string> _paymentMethods = new();

        public ObservableCollection<string> AvailablePaymentMethods { get; } = new()
        {
            "Espèces",
            "Carte bancaire",
            "Chèque",
            "Virement",
            "PayPal",
            "Apple Pay",
            "Google Pay"
        };

        // Horaires d'ouverture
        [ObservableProperty]
        private string _operatingHours = string.Empty;

        // État UI
        [ObservableProperty]
        private bool _isSubmitting = false;

        [ObservableProperty]
        private bool _isLocationLoading = false;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private bool _hasValidationErrors = false;

        public BusinessAddViewModel(
            ISupabaseApiService apiService,
            ILocationService locationService,
            ISimpleAuthenticationService authService,
            ILogger<BusinessAddViewModel> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LoadBusinessTypes();
            InitializePaymentMethods();

            _logger.LogDebug("BusinessAddViewModel initialized");
        }

        public override async Task InitializeAsync(IDictionary<string, object> parameters)
        {
            try
            {
                _logger.LogDebug("BusinessAddViewModel.InitializeAsync called with parameter: {Parameter}", parameters?.ToString() ?? "null");

                // Handle location parameters from map navigation
                if (parameters?.Count > 0)
                {
                    _logger.LogDebug("Processing navigation parameters for business creation");

                    if (parameters.TryGetValue("Latitude", out var latValue) && latValue is double lat)
                    {
                        Latitude = lat;
                        _logger.LogDebug("Set latitude from parameter: {Latitude}", lat);
                    }

                    if (parameters.TryGetValue("Longitude", out var lonValue) && lonValue is double lon)
                    {
                        Longitude = lon;
                        _logger.LogDebug("Set longitude from parameter: {Longitude}", lon);
                    }

                    // Si on a des coordonnées, essayer de récupérer l'adresse
                    if (Math.Abs(Latitude - 43.6047) > 0.0001 || Math.Abs(Longitude - 1.4442) > 0.0001) // Si différent des valeurs par défaut
                    {
                        await TryReverseGeocoding();
                    }
                }
                else
                {
                    // No navigation parameters, get current GPS location
                    await GetCurrentLocationAsync();
                }

                _logger.LogDebug("BusinessAddViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BUSINESS_ADD_ERROR: Failed to initialize BusinessAddViewModel");
                ValidationMessage = "Erreur lors de l'initialisation. Veuillez réessayer.";
                HasValidationErrors = true;
            }
        }

        private void LoadBusinessTypes()
        {
            BusinessTypes.Clear();
            foreach (BusinessType type in Enum.GetValues<BusinessType>())
            {
                BusinessTypes.Add(type);
            }
        }

        private void InitializePaymentMethods()
        {
            // Méthodes de paiement par défaut
            PaymentMethods.Add("Espèces");
            PaymentMethods.Add("Carte bancaire");
        }

        [RelayCommand]
        private async Task GetCurrentLocationAsync()
        {
            try
            {
                IsLocationLoading = true;
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = (double)location.Latitude;
                    Longitude = (double)location.Longitude;
                    await TryReverseGeocoding();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current location for business");
            }
            finally
            {
                IsLocationLoading = false;
            }
        }

        private async Task TryReverseGeocoding()
        {
            try
            {
                // Ici on pourrait implémenter la géocodification inverse
                // Pour récupérer automatiquement l'adresse depuis les coordonnées
                _logger.LogDebug("Reverse geocoding for coordinates: {Lat}, {Lon}", Latitude, Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reverse geocoding failed");
            }
        }

        [RelayCommand]
        private void TogglePaymentMethod(string method)
        {
            if (PaymentMethods.Contains(method))
            {
                PaymentMethods.Remove(method);
            }
            else
            {
                PaymentMethods.Add(method);
            }
        }

        [RelayCommand]
        private async Task SubmitBusinessAsync()
        {
            try
            {
                if (!ValidateForm())
                {
                    return;
                }

                IsSubmitting = true;
                ClearValidationErrors();

                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser?.Id == null)
                {
                    ValidationMessage = "Vous devez être connecté pour créer un commerce.";
                    HasValidationErrors = true;
                    return;
                }

                // Créer l'objet business pour Supabase
                var newBusiness = new SupabaseBusiness
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    Latitude = (decimal)Latitude,
                    Longitude = (decimal)Longitude,
                    Address = Address.Trim(),
                    City = City.Trim(),
                    PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode.Trim(),
                    Country = Country.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Website = string.IsNullOrWhiteSpace(Website) ? null : Website.Trim(),
                    BusinessType = SelectedBusinessType.ToString(),
                    Services = string.IsNullOrWhiteSpace(Services) ? null : new { description = Services.Trim() },
                    PriceRange = PriceRange,
                    PaymentMethods = PaymentMethods.Count > 0 ? PaymentMethods.ToArray() : null,
                    OperatingHours = string.IsNullOrWhiteSpace(OperatingHours) ? null : new { description = OperatingHours.Trim() },
                    CreatedAt = DateTime.UtcNow
                };

                // Enregistrer via l'API Supabase
                var createdBusiness = await _apiService.CreateBusinessAsync(newBusiness);

                if (createdBusiness != null)
                {
                    _logger.LogInformation("Business created successfully: {BusinessName}", Name);

                    // Retourner vers la carte
                    await Shell.Current.GoToAsync("//map");

                    // Notification de succès
                    await Application.Current.MainPage.DisplayAlert(
                        "Succès",
                        $"Le commerce '{Name}' a été créé avec succès !",
                        "OK"
                    );
                }
                else
                {
                    ValidationMessage = "Erreur lors de la création du commerce. Veuillez réessayer.";
                    HasValidationErrors = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BUSINESS_CREATE_ERROR: Failed to create business");
                ValidationMessage = $"Erreur : {ex.Message}";
                HasValidationErrors = true;
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private bool ValidateForm()
        {
            ClearValidationErrors();
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("Le nom du commerce est obligatoire.");
            }
            else if (Name.Trim().Length < 3)
            {
                errors.Add("Le nom doit contenir au moins 3 caractères.");
            }

            if (string.IsNullOrWhiteSpace(Address))
            {
                errors.Add("L'adresse est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(City))
            {
                errors.Add("La ville est obligatoire.");
            }

            if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email))
            {
                errors.Add("L'adresse email n'est pas valide.");
            }

            if (!string.IsNullOrWhiteSpace(Website) && !IsValidUrl(Website))
            {
                errors.Add("L'URL du site web n'est pas valide.");
            }

            if (Latitude < -90 || Latitude > 90 || Longitude < -180 || Longitude > 180)
            {
                errors.Add("Les coordonnées GPS ne sont pas valides.");
            }

            if (PaymentMethods.Count == 0)
            {
                errors.Add("Veuillez sélectionner au moins une méthode de paiement.");
            }

            if (errors.Count > 0)
            {
                ValidationMessage = string.Join(" ", errors);
                HasValidationErrors = true;
                return false;
            }

            return true;
        }

        private void ClearValidationErrors()
        {
            ValidationMessage = string.Empty;
            HasValidationErrors = false;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("//map");
        }
    }
}