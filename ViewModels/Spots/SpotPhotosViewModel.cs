using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using System.Collections.ObjectModel;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotPhotosViewModel : ViewModelBase
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly IEnhancedMediaService _enhancedMediaService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private Spot? _spot;

        [ObservableProperty]
        private ObservableCollection<SupabaseSpotMedia> _spotPhotos = new();

        [ObservableProperty]
        private bool _isUploading = false;

        [ObservableProperty]
        private double _uploadProgress = 0.0;

        [ObservableProperty]
        private string _uploadStatus = string.Empty;

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

        public string PrimaryPhotoPath =>
            SpotPhotos.FirstOrDefault(p => p.IsPrimary)?.MediaUrl ?? string.Empty;

        public bool CanAddMorePhotos => SpotPhotos.Count < 10; // Max 10 photos per spot

        public string PhotoCountStatus => 
            $"{SpotPhotos.Count}/10 photos ajoutées";

        public string PhotoCountStatusColor => 
            SpotPhotos.Count >= 8 ? "#FF6B00" : "#666666";

        public bool ShowPhotoCountStatus => SpotPhotos.Count > 0;

        public SpotPhotosViewModel(
            ISupabaseApiService supabaseApiService,
            IEnhancedMediaService enhancedMediaService,
            INavigationService navigationService,
            IDialogService dialogService)
            : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _enhancedMediaService = enhancedMediaService;
            _dialogService = dialogService;

            Title = "Gestion des Photos";
        }

        public async Task InitializeAsync(string spotId)
        {
            if (!Guid.TryParse(spotId, out var guid))
            {
                await _dialogService.ShowAlertAsync("Erreur", "ID de spot invalide", "OK");
                return;
            }

            await LoadSpotPhotos(guid);
        }

        private async Task LoadSpotPhotos(Guid spotId)
        {
            try
            {
                IsLoading = true;
                IsError = false;

                // Load spot information
                var spots = await _supabaseApiService.GetSpotsAsync();
                var supabaseSpot = spots.FirstOrDefault(s => s.Id == spotId);

                if (supabaseSpot == null)
                {
                    IsError = true;
                    ErrorMessage = "Spot non trouvé";
                    return;
                }

                // Convert to domain model
                Spot = new Spot
                {
                    Id = supabaseSpot.Id,
                    Name = supabaseSpot.Name,
                    Description = supabaseSpot.Description
                };

                // Load spot photos (conversion temporaire Guid->int pour compatibilité)
                // TODO: Migrer vers l'architecture hybride avec int IDs
                var photos = await _supabaseApiService.GetSpotMediaAsync((int)spotId.GetHashCode());
                SpotPhotos.Clear();
                foreach (var photo in photos.OrderBy(p => p.DisplayOrder))
                {
                    SpotPhotos.Add(photo);
                }
                OnPropertyChanged(nameof(PrimaryPhotoPath));
                OnPropertyChanged(nameof(CanAddMorePhotos));
                OnPropertyChanged(nameof(PhotoCountStatus));
                OnPropertyChanged(nameof(PhotoCountStatusColor));
                OnPropertyChanged(nameof(ShowPhotoCountStatus));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSpotPhotos failed: {ex.Message}");
                IsError = true;
                ErrorMessage = "Erreur lors du chargement des photos";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddPhotoFromCamera()
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    ShowToastMessage("❌ Appareil photo non disponible", "#F44336", "#F44336", "White");
                    return;
                }

                var result = await MediaPicker.Default.CapturePhotoAsync();
                if (result != null)
                {
                    await ProcessSelectedPhoto(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] AddPhotoFromCamera failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la prise de photo", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task AddPhotoFromGallery()
        {
            try
            {
                var result = await MediaPicker.Default.PickPhotoAsync();
                if (result != null)
                {
                    await ProcessSelectedPhoto(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] AddPhotoFromGallery failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la sélection de photo", "#F44336", "#F44336", "White");
            }
        }

        private async Task ProcessSelectedPhoto(FileResult photo)
        {
            try
            {
                if (Spot == null) return;

                IsUploading = true;
                UploadProgress = 0.0;
                UploadStatus = "Préparation de l'image...";

                // Validate file size (max 5MB)
                using var stream = await photo.OpenReadAsync();
                if (stream.Length > 5 * 1024 * 1024)
                {
                    ShowToastMessage("❌ Image trop grande (max 5MB)", "#F44336", "#F44336", "White");
                    return;
                }

                UploadProgress = 25.0;
                UploadStatus = "Optimisation de l'image...";

                // Optimize the image
                using var optimizedImageStream = await _enhancedMediaService.OptimizeImageAsync(stream);
                
                // Convert to byte array for upload
                using var memoryStream = new MemoryStream();
                await optimizedImageStream.CopyToAsync(memoryStream);
                var optimizedImageBytes = memoryStream.ToArray();
                
                UploadProgress = 50.0;
                UploadStatus = "Upload vers Supabase...";

                // Upload to Supabase
                string fileName = $"spot_{Spot.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
                var uploadResult = await _supabaseApiService.UploadImageAsync(
                    "spot-photos", 
                    fileName, 
                    optimizedImageBytes);

                if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.PublicUrl))
                {
                    UploadProgress = 75.0;
                    UploadStatus = "Création de l'enregistrement...";

                    // Create media record
                    var mediaResult = await _supabaseApiService.CreateSpotMediaAsync(new SupabaseSpotMedia
                    {
                        // Id sera auto-généré par la DB (SERIAL), ne pas l'assigner
                        SpotId = Spot.Id.GetHashCode(), // Conversion Guid vers int (temporaire jusqu'à migration complète)
                        MediaType = 1, // Photo
                        MediaUrl = uploadResult.PublicUrl,
                        IsPrimary = SpotPhotos.Count == 0, // First photo is primary
                        Width = 800, // Standard optimized width
                        Height = 600, // Standard optimized height
                        FileSize = optimizedImageBytes.Length,
                        ContentType = "image/jpeg",
                        Status = 1, // Processing
                        DisplayOrder = SpotPhotos.Count,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (mediaResult != null)
                    {
                        UploadProgress = 100.0;
                        UploadStatus = "Terminé !";
                        
                        // Refresh the photos list
                        await LoadSpotPhotos(Spot.Id);
                        
                        ShowToastMessage("✅ Photo ajoutée avec succès !", "#4CAF50", "#4CAF50", "White");
                    }
                    else
                    {
                        ShowToastMessage("❌ Erreur lors de l'enregistrement", "#F44336", "#F44336", "White");
                    }
                }
                else
                {
                    ShowToastMessage("❌ Erreur lors de l'upload", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ProcessSelectedPhoto failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors du traitement", "#F44336", "#F44336", "White");
            }
            finally
            {
                IsUploading = false;
                UploadProgress = 0.0;
                UploadStatus = string.Empty;
            }
        }

        [RelayCommand]
        private async Task DeletePhoto(SupabaseSpotMedia photo)
        {
            try
            {
                bool confirm = await _dialogService.ShowConfirmationAsync(
                    "Supprimer la photo",
                    "Êtes-vous sûr de vouloir supprimer cette photo ?",
                    "Supprimer",
                    "Annuler");

                if (!confirm) return;

                var success = await _supabaseApiService.DeleteSpotMediaAsync(photo.Id);
                if (success)
                {
                    SpotPhotos.Remove(photo);
                    OnPropertyChanged(nameof(PrimaryPhotoPath));
                    OnPropertyChanged(nameof(CanAddMorePhotos));
                    OnPropertyChanged(nameof(PhotoCountStatus));
                    OnPropertyChanged(nameof(PhotoCountStatusColor));
                    OnPropertyChanged(nameof(ShowPhotoCountStatus));
                    ShowToastMessage("✅ Photo supprimée", "#4CAF50", "#4CAF50", "White");
                }
                else
                {
                    ShowToastMessage("❌ Erreur lors de la suppression", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] DeletePhoto failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la suppression", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task SetAsPrimary(SupabaseSpotMedia photo)
        {
            try
            {
                if (Spot == null) return;

                var success = await _supabaseApiService.SetPrimarySpotPhotoAsync(Spot.Id.GetHashCode(), photo.Id);
                if (success)
                {
                    // Update local collection
                    foreach (var p in SpotPhotos)
                    {
                        p.IsPrimary = p.Id == photo.Id;
                    }
                    OnPropertyChanged(nameof(PrimaryPhotoPath));
                    ShowToastMessage("✅ Photo principale mise à jour", "#4CAF50", "#4CAF50", "White");
                }
                else
                {
                    ShowToastMessage("❌ Erreur lors de la mise à jour", "#F44336", "#F44336", "White");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SetAsPrimary failed: {ex.Message}");
                ShowToastMessage("❌ Erreur lors de la mise à jour", "#F44336", "#F44336", "White");
            }
        }

        [RelayCommand]
        private async Task ReorderPhotos()
        {
            try
            {
                // This could be expanded to allow drag-and-drop reordering
                ShowToastMessage("ℹ️ Fonction en développement", "#2196F3", "#2196F3", "White");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ReorderPhotos failed: {ex.Message}");
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
