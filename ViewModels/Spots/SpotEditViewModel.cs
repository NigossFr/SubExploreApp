using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotEditViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly ISpotEditService _spotEditService;
        private readonly IEnhancedMediaService _enhancedMediaService;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private Spot? _originalSpot;

        [ObservableProperty]
        private string _spotName = string.Empty;

        [ObservableProperty]
        private string _spotDescription = string.Empty;

        [ObservableProperty]
        private string _requiredEquipment = string.Empty;

        [ObservableProperty]
        private string _safetyNotes = string.Empty;

        [ObservableProperty]
        private string _bestConditions = string.Empty;

        [ObservableProperty]
        private string _maxDepth = string.Empty;

        [ObservableProperty]
        private string _latitude = string.Empty;

        [ObservableProperty]
        private string _longitude = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _difficultyLevels = new();

        [ObservableProperty]
        private string _selectedDifficultyLevel = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _currentStrengths = new();

        [ObservableProperty]
        private string _selectedCurrentStrength = string.Empty;

        // Toast properties
        [ObservableProperty]
        private bool _isToastVisible = false;

        [ObservableProperty]
        private string _toastMessage = string.Empty;

        [ObservableProperty]
        private string _toastBackgroundColor = "#4CAF50";

        [ObservableProperty]
        private string _toastBorderColor = "#4CAF50";

        [ObservableProperty]
        private string _toastTextColor = "White";

        public SpotEditViewModel(
            ISupabaseApiService supabaseApiService,
            INavigationService navigationService,
            IDialogService dialogService,
            ISpotEditService spotEditService,
            IEnhancedMediaService enhancedMediaService,
            ILocationService locationService)
            : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _spotEditService = spotEditService;
            _enhancedMediaService = enhancedMediaService;
            _locationService = locationService;

            Title = "Édition du Spot";
            InitializeEnums();
        }

        public async Task InitializeAsync(string spotId)
        {
            if (!Guid.TryParse(spotId, out var guid))
            {
                await DialogService.ShowAlertAsync("Erreur", "ID de spot invalide", "OK");
                return;
            }

            await LoadSpotData(guid);
        }

        private async Task LoadSpotData(Guid spotId)
        {
            try
            {
                IsLoading = true;
                IsError = false;

                // Load spot data from Supabase
                var spots = await _supabaseApiService.GetSpotsAsync();
                var supabaseSpot = spots.FirstOrDefault(s => s.Id == spotId);

                if (supabaseSpot == null)
                {
                    IsError = true;
                    ErrorMessage = "Spot non trouvé";
                    return;
                }

                // Convert to domain model
                OriginalSpot = new Spot
                {
                    Id = supabaseSpot.Id,
                    Name = supabaseSpot.Name,
                    Description = supabaseSpot.Description,
                    RequiredEquipment = supabaseSpot.RequiredEquipment,
                    SafetyNotes = supabaseSpot.SafetyNotes,
                    BestConditions = supabaseSpot.BestConditions,
                    MaxDepth = supabaseSpot.MaxDepth != null ? (int?)supabaseSpot.MaxDepth : null,
                    Latitude = supabaseSpot.Latitude,
                    Longitude = supabaseSpot.Longitude,
                    DifficultyLevel = (DifficultyLevel)supabaseSpot.DifficultyLevel,
                    CurrentStrength = supabaseSpot.CurrentStrength > 0 
                        ? (CurrentStrength)supabaseSpot.CurrentStrength 
                        : null
                };

                // Check permissions
                bool canEdit = await _spotEditService.CanUserEditSpotAsync(OriginalSpot);
                if (!canEdit)
                {
                    IsError = true;
                    ErrorMessage = "Vous n'avez pas l'autorisation de modifier ce spot";
                    return;
                }

                // Populate form fields
                PopulateFormFields();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotData failed: {ex.Message}");
                IsError = true;
                ErrorMessage = "Erreur lors du chargement des données";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PopulateFormFields()
        {
            if (OriginalSpot == null) return;

            SpotName = OriginalSpot.Name ?? string.Empty;
            SpotDescription = OriginalSpot.Description ?? string.Empty;
            RequiredEquipment = OriginalSpot.RequiredEquipment ?? string.Empty;
            SafetyNotes = OriginalSpot.SafetyNotes ?? string.Empty;
            BestConditions = OriginalSpot.BestConditions ?? string.Empty;
            MaxDepth = OriginalSpot.MaxDepth?.ToString() ?? string.Empty;
            Latitude = OriginalSpot.Latitude.ToString();
            Longitude = OriginalSpot.Longitude.ToString();
            
            SelectedDifficultyLevel = GetDifficultyDisplayName(OriginalSpot.DifficultyLevel);
            SelectedCurrentStrength = OriginalSpot.CurrentStrength?.ToString() ?? "None";
        }

        private void InitializeEnums()
        {
            // Difficulty levels
            DifficultyLevels.Add("Débutant");
            DifficultyLevels.Add("Intermédiaire");
            DifficultyLevels.Add("Avancé");
            DifficultyLevels.Add("Expert");
            DifficultyLevels.Add("Technique Uniquement");

            // Current strengths
            CurrentStrengths.Add("Aucun");
            CurrentStrengths.Add("Faible");
            CurrentStrengths.Add("Modéré");
            CurrentStrengths.Add("Fort");
            CurrentStrengths.Add("Extrême");
        }

        private string GetDifficultyDisplayName(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Beginner => "Débutant",
                DifficultyLevel.Intermediate => "Intermédiaire",
                DifficultyLevel.Advanced => "Avancé",
                DifficultyLevel.Expert => "Expert",
                DifficultyLevel.TechnicalOnly => "Technique Uniquement",
                _ => "Débutant"
            };
        }

        private DifficultyLevel GetDifficultyFromDisplayName(string displayName)
        {
            return displayName switch
            {
                "Débutant" => DifficultyLevel.Beginner,
                "Intermédiaire" => DifficultyLevel.Intermediate,
                "Avancé" => DifficultyLevel.Advanced,
                "Expert" => DifficultyLevel.Expert,
                "Technique Uniquement" => DifficultyLevel.TechnicalOnly,
                _ => DifficultyLevel.Beginner
            };
        }

        private CurrentStrength? GetCurrentStrengthFromString(string value)
        {
            return value switch
            {
                "Aucun" => CurrentStrength.None,
                "Faible" => CurrentStrength.Weak,
                "Modéré" => CurrentStrength.Moderate,
                "Fort" => CurrentStrength.Strong,
                "Extrême" => CurrentStrength.Extreme,
                _ => null
            };
        }

        [RelayCommand]
        private async Task SaveBasicInfo()
        {
            try
            {
                if (OriginalSpot == null) return;

                bool success = await _spotEditService.UpdateSpotBasicInfoAsync(
                    OriginalSpot.Id,
                    SpotName.Trim(),
                    SpotDescription.Trim(),
                    RequiredEquipment.Trim(),
                    SafetyNotes.Trim(),
                    BestConditions.Trim()
                );

                if (success)
                {
                    ShowToastMessage("✅ Informations sauvegardées !", "#4CAF50", "#4CAF50", "White");
                }
                else
                {
                    ShowToastMessage("❌ Erreur lors de la sauvegarde", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SaveBasicInfo failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la sauvegarde", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task SaveTechnicalDetails()
        {
            try
            {
                if (OriginalSpot == null) return;

                int? maxDepthValue = null;
                if (!string.IsNullOrWhiteSpace(MaxDepth) && int.TryParse(MaxDepth, out var depth))
                {
                    maxDepthValue = depth;
                }

                var difficulty = GetDifficultyFromDisplayName(SelectedDifficultyLevel);
                var currentStrength = GetCurrentStrengthFromString(SelectedCurrentStrength);

                bool success = await _spotEditService.UpdateSpotTechnicalDetailsAsync(
                    OriginalSpot.Id,
                    maxDepthValue,
                    difficulty,
                    currentStrength
                );

                if (success)
                {
                    ShowToastMessage("✅ Détails techniques sauvegardés !", "#4CAF50", "#4CAF50", "White");
                }
                else
                {
                    ShowToastMessage("❌ Erreur lors de la sauvegarde", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SaveTechnicalDetails failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la sauvegarde", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task SaveLocation()
        {
            try
            {
                if (OriginalSpot == null) return;

                if (!decimal.TryParse(Latitude, out var lat) || !decimal.TryParse(Longitude, out var lon))
                {
                    ShowToastMessage("❌ Coordonnées invalides", "#F44336", "#F44336", "White");
                    return;
                }

                bool success = await _spotEditService.UpdateSpotLocationAsync(OriginalSpot.Id, lat, lon);

                if (success)
                {
                    ShowToastMessage("✅ Localisation sauvegardée !", "#4CAF50", "#4CAF50", "White");
                }
                else
                {
                    ShowToastMessage("❌ Erreur lors de la sauvegarde", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SaveLocation failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la sauvegarde", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task UseCurrentLocation()
        {
            try
            {
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    Latitude = location.Latitude.ToString("F6");
                    Longitude = location.Longitude.ToString("F6");
                    ShowToastMessage("📍 Position actuelle obtenue !", "#2196F3", "#2196F3", "White");
                }
                else
                {
                    ShowToastMessage("❌ Impossible d'obtenir la position", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UseCurrentLocation failed: {ex.Message}");
                ShowToastMessage("❌ Erreur GPS", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task ManagePhotos()
        {
            try
            {
                if (OriginalSpot == null) return;

                // Navigate to photo management page
                await Shell.Current.GoToAsync("spotphotos", new Dictionary<string, object>
                {
                    ["spotId"] = OriginalSpot.Id.ToString()
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ManagePhotos failed: {ex.Message}");
                ShowToastMessage("❌ Erreur navigation", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task SubmitForRevalidation()
        {
            try
            {
                if (OriginalSpot == null) return;

                string reason = await DialogService.ShowPromptAsync(
                    "Revalidation",
                    "Pourquoi ce spot nécessite-t-il une revalidation ?",
                    "Soumettre",
                    "Annuler",
                    "Raison de la revalidation...");

                if (string.IsNullOrWhiteSpace(reason))
                    return;

                bool success = await _spotEditService.SubmitForRevalidationAsync(OriginalSpot.Id, reason);
                
                if (success)
                {
                    ShowToastMessage("✅ Spot soumis pour revalidation", "#4CAF50", "#4CAF50", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SubmitForRevalidation failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la soumission", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (OriginalSpot != null)
            {
                await LoadSpotData(OriginalSpot.Id);
            }
        }

        private void ShowToastMessage(string message, string backgroundColor, string borderColor, string textColor)
        {
            ToastMessage = message;
            ToastBackgroundColor = backgroundColor;
            ToastBorderColor = borderColor;
            ToastTextColor = textColor;
            IsToastVisible = true;

            // Auto-hide after 3 seconds
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                IsToastVisible = false;
            });
        }
    }
}