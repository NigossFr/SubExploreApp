using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Services.Interfaces;
using System.IO;

namespace SubExplore.Services.Implementations
{
    public class MediaService : IMediaService
    {
        private readonly IDialogService _dialogService;
        private readonly IConnectivityService _connectivityService;

        public MediaService(IDialogService dialogService, IConnectivityService connectivityService)
        {
            _dialogService = dialogService;
            _connectivityService = connectivityService;
        }

        public async Task<string> TakePhotoAsync(int quality = 80)
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "La prise de photo n'est pas supportée sur cet appareil.", "OK");
                    return null;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null)
                    return null;

                // Créer un fichier temporaire
                var tempFile = Path.GetTempFileName();

                using (var stream = await photo.OpenReadAsync())
                using (var fileStream = File.Create(tempFile))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // Compresser la photo pour optimiser l'espace
                var compressedFile = await CompressImageAsync(tempFile, quality, 1200);

                // Si on a pu compresser, on supprime l'original
                if (compressedFile != tempFile && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                return compressedFile;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Impossible de prendre une photo : {ex.Message}", "OK");
                return null;
            }
        }

        public async Task<string> PickPhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null)
                    return null;

                // Créer un fichier temporaire
                var tempFile = Path.GetTempFileName();

                using (var stream = await photo.OpenReadAsync())
                using (var fileStream = File.Create(tempFile))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // Compresser la photo pour optimiser l'espace
                var compressedFile = await CompressImageAsync(tempFile, 80, 1200);

                // Si on a pu compresser, on supprime l'original
                if (compressedFile != tempFile && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                return compressedFile;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Impossible de sélectionner une photo : {ex.Message}", "OK");
                return null;
            }
        }

        public async Task<string> UploadImageAsync(string localFilePath, string targetFolder, string fileName = null)
        {
            // Pour le moment, comme il n'y a pas de serveur, nous allons simplement simuler un téléchargement
            // Dans une implémentation réelle, vous utiliseriez un service web pour télécharger l'image

            try
            {
                if (!_connectivityService.IsConnected)
                {
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de télécharger l'image. Vérifiez votre connexion internet.", "OK");
                    return null;
                }

                // Simuler un délai de téléchargement
                await Task.Delay(1000);

                // Générer un nom de fichier aléatoire si aucun n'est fourni
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"{Guid.NewGuid():N}.jpg";
                }

                // Dans une implémentation réelle, vous retourneriez l'URL du fichier téléchargé
                return $"https://api.subexplore.com/media/{targetFolder}/{fileName}";
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Échec du téléchargement : {ex.Message}", "OK");
                return null;
            }
        }

        public async Task<string> CompressImageAsync(string filePath, int quality = 80, int maxDimension = 1200)
        {
            try
            {
                // Pour le moment, nous allons simplement retourner le chemin original
                // Dans une implémentation réelle, vous utiliseriez une bibliothèque comme SkiaSharp
                // pour redimensionner et compresser l'image

                // Note: Vous pourriez implémenter une vraie compression dans une future mise à jour
                // Cette version simplifiée n'implémente pas réellement la compression pour éviter
                // les dépendances supplémentaires

                return filePath;
            }
            catch (Exception)
            {
                // En cas d'erreur, on retourne simplement le fichier original
                return filePath;
            }
        }

        public async Task<MediaMetadata> GetImageMetadataAsync(string filePath)
        {
            try
            {
                // Dans une implémentation réelle, vous utiliseriez ExifLib ou une autre bibliothèque
                // pour extraire les métadonnées de l'image

                var fileInfo = new FileInfo(filePath);

                return new MediaMetadata
                {
                    FileSize = fileInfo.Length,
                    ContentType = GetContentType(filePath),
                    MediaType = MediaType.Photo,
                    // Note: Ces valeurs seraient extraites de l'image dans une implémentation réelle
                    Width = 1200,
                    Height = 800
                };
            }
            catch (Exception)
            {
                // En cas d'erreur, on retourne des métadonnées minimales
                return new MediaMetadata
                {
                    FileSize = 0,
                    ContentType = "image/jpeg",
                    MediaType = MediaType.Photo
                };
            }
        }

        public async Task<SpotMedia> CreateSpotMediaAsync(int spotId, string filePath, bool isPrimary, string caption = null)
        {
            try
            {
                // Récupérer les métadonnées de l'image
                var metadata = await GetImageMetadataAsync(filePath);

                // Télécharger l'image vers le serveur (simulation)
                var mediaUrl = await UploadImageAsync(filePath, $"spots/{spotId}");

                if (mediaUrl == null)
                    return null;

                // Créer l'objet SpotMedia
                return new SpotMedia
                {
                    SpotId = spotId,
                    MediaType = metadata.MediaType,
                    MediaUrl = mediaUrl,
                    Caption = caption,
                    IsPrimary = isPrimary,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    FileSize = metadata.FileSize,
                    ContentType = metadata.ContentType,
                    Status = MediaStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erreur", $"Impossible de créer le média : {ex.Message}", "OK");
                return null;
            }
        }

        // Méthode utilitaire pour déterminer le type MIME basé sur l'extension
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}