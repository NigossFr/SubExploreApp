using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.Services.Validation;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotPhotosViewModel : ViewModelBase
    {
        private readonly IMediaService _mediaService;
        private readonly IValidationService _validationService;

        [ObservableProperty]
        private ObservableCollection<string> _photosPaths;

        [ObservableProperty]
        private string _primaryPhotoPath;

        [ObservableProperty]
        private bool _isUploading;

        [ObservableProperty]
        private bool _isUploadingPrimaryPhoto;

        [ObservableProperty]
        private bool _canAddMorePhotos = true;

        [ObservableProperty]
        private string _photoCountStatus = string.Empty;

        [ObservableProperty]
        private string _photoCountStatusColor = "Black";

        [ObservableProperty]
        private bool _showPhotoCountStatus;

        private const int MaxPhotosAllowed = 3;

        public SpotPhotosViewModel(
            IMediaService mediaService,
            IValidationService validationService,
            IDialogService dialogService)
            : base(dialogService)
        {
            _mediaService = mediaService;
            _validationService = validationService;
            PhotosPaths = new ObservableCollection<string>();
            PhotosPaths.CollectionChanged += OnPhotosPathsChanged;
            Title = "Photos du spot";
            UpdatePhotoStatus();
        }
        
        private void OnPhotosPathsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdatePhotoStatus();
        }
        
        private void UpdatePhotoStatus()
        {
            var count = PhotosPaths.Count;
            CanAddMorePhotos = count < MaxPhotosAllowed;
            
            if (count == 0)
            {
                PhotoCountStatus = "Aucune photo ajoutée";
                PhotoCountStatusColor = "#757575";
                ShowPhotoCountStatus = true;
            }
            else if (count < MaxPhotosAllowed)
            {
                PhotoCountStatus = $"{count}/{MaxPhotosAllowed} photos - Vous pouvez en ajouter {MaxPhotosAllowed - count} de plus";
                PhotoCountStatusColor = "#4CAF50";
                ShowPhotoCountStatus = true;
            }
            else
            {
                PhotoCountStatus = $"{count}/{MaxPhotosAllowed} photos - Limite atteinte";
                PhotoCountStatusColor = "#FF9800";
                ShowPhotoCountStatus = true;
            }
        }

        public override Task InitializeAsync(object parameter = null)
        {
            if (parameter is List<string> existingPhotos)
            {
                // Initialiser avec des photos existantes
                PhotosPaths.Clear();
                foreach (var photo in existingPhotos)
                {
                    PhotosPaths.Add(photo);
                }

                // Définir la première comme principale si elle existe
                if (PhotosPaths.Count > 0 && string.IsNullOrEmpty(PrimaryPhotoPath))
                {
                    PrimaryPhotoPath = PhotosPaths[0];
                }
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task TakePhoto()
        {
            if (PhotosPaths.Count >= MaxPhotosAllowed)
            {
                await DialogService.ShowAlertAsync("Limite atteinte", $"Vous ne pouvez pas ajouter plus de {MaxPhotosAllowed} photos.", "OK");
                return;
            }

            IsUploading = true;
            var isFirstPhoto = PhotosPaths.Count == 0;
            if (isFirstPhoto)
            {
                IsUploadingPrimaryPhoto = true;
            }
            
            try
            {
                var photoPath = await _mediaService.TakePhotoAsync();
                if (!string.IsNullOrEmpty(photoPath))
                {
                    PhotosPaths.Add(photoPath);

                    // Si c'est la première photo, la définir comme principale
                    if (string.IsNullOrEmpty(PrimaryPhotoPath))
                    {
                        PrimaryPhotoPath = photoPath;
                    }
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de prendre une photo.", "OK");
            }
            finally
            {
                IsUploading = false;
                IsUploadingPrimaryPhoto = false;
            }
        }

        [RelayCommand]
        private async Task PickPhoto()
        {
            if (PhotosPaths.Count >= MaxPhotosAllowed)
            {
                await DialogService.ShowAlertAsync("Limite atteinte", $"Vous ne pouvez pas ajouter plus de {MaxPhotosAllowed} photos.", "OK");
                return;
            }

            IsUploading = true;
            var isFirstPhoto = PhotosPaths.Count == 0;
            if (isFirstPhoto)
            {
                IsUploadingPrimaryPhoto = true;
            }
            
            try
            {
                var photoPath = await _mediaService.PickPhotoAsync();
                if (!string.IsNullOrEmpty(photoPath))
                {
                    PhotosPaths.Add(photoPath);

                    // Si c'est la première photo, la définir comme principale
                    if (string.IsNullOrEmpty(PrimaryPhotoPath))
                    {
                        PrimaryPhotoPath = photoPath;
                    }
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de sélectionner une photo.", "OK");
            }
            finally
            {
                IsUploading = false;
                IsUploadingPrimaryPhoto = false;
            }
        }

        [RelayCommand]
        private void RemovePhoto(string path)
        {
            if (PhotosPaths.Contains(path))
            {
                PhotosPaths.Remove(path);

                // Si c'était la photo principale, en choisir une autre
                if (PrimaryPhotoPath == path)
                {
                    PrimaryPhotoPath = PhotosPaths.Count > 0 ? PhotosPaths[0] : null;
                }
            }
        }

        [RelayCommand]
        private void SetPrimaryPhoto(string path)
        {
            if (PhotosPaths.Contains(path))
            {
                PrimaryPhotoPath = path;
            }
        }

        [RelayCommand]
        private async Task ValidatePhotos()
        {
            var validationResult = _validationService.ValidatePhotos(PhotosPaths, PrimaryPhotoPath);
            
            if (!validationResult.IsValid)
            {
                await DialogService.ShowAlertAsync("Erreurs de validation", validationResult.GetErrorsText(), "OK");
                return;
            }
            
            if (validationResult.HasWarnings)
            {
                var shouldContinue = await DialogService.ShowConfirmationAsync(
                    "Avertissements", 
                    $"Des avertissements ont été détectés:\n{validationResult.GetWarningsText()}\n\nVoulez-vous continuer ?", 
                    "Continuer", 
                    "Corriger");
                    
                if (!shouldContinue)
                {
                    return;
                }
            }
            
            await DialogService.ShowToastAsync("Photos validées avec succès");
        }
    }
}
