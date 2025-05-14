using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spot
{
    public partial class SpotPhotosViewModel : ViewModelBase
    {
        private readonly IMediaService _mediaService;

        [ObservableProperty]
        private ObservableCollection<string> _photosPaths;

        [ObservableProperty]
        private string _primaryPhotoPath;

        [ObservableProperty]
        private bool _isUploading;

        public SpotPhotosViewModel(
            IMediaService mediaService,
            IDialogService dialogService)
            : base(dialogService)
        {
            _mediaService = mediaService;
            PhotosPaths = new ObservableCollection<string>();
            Title = "Photos du spot";
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
            if (PhotosPaths.Count >= 3)
            {
                await DialogService.ShowAlertAsync("Limite atteinte", "Vous ne pouvez pas ajouter plus de 3 photos.", "OK");
                return;
            }

            IsUploading = true;
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
            }
        }

        [RelayCommand]
        private async Task PickPhoto()
        {
            if (PhotosPaths.Count >= 3)
            {
                await DialogService.ShowAlertAsync("Limite atteinte", "Vous ne pouvez pas ajouter plus de 3 photos.", "OK");
                return;
            }

            IsUploading = true;
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
        private void ValidatePhotos()
        {
            // Cette méthode serait appelée pour valider cette étape
            // Le ViewModel parent (AddSpotViewModel) gère la transition d'étapes
        }
    }
}
