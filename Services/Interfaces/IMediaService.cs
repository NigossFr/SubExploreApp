using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    public interface IMediaService
    {
        /// <summary>
        /// Permet de prendre une photo avec l'appareil photo
        /// </summary>
        /// <param name="quality">Qualité de l'image (0-100)</param>
        /// <returns>Le chemin vers le fichier temporaire créé</returns>
        Task<string> TakePhotoAsync(int quality = 80);

        /// <summary>
        /// Permet de sélectionner une image depuis la galerie
        /// </summary>
        /// <returns>Le chemin vers le fichier temporaire créé</returns>
        Task<string> PickPhotoAsync();

        /// <summary>
        /// Télécharge une image vers le serveur et renvoie l'URL de stockage
        /// </summary>
        /// <param name="localFilePath">Chemin local du fichier à télécharger</param>
        /// <param name="targetFolder">Dossier cible sur le serveur</param>
        /// <param name="fileName">Nom du fichier cible (optionnel, généré sinon)</param>
        /// <returns>L'URL de l'image stockée</returns>
        Task<string> UploadImageAsync(string localFilePath, string targetFolder, string fileName = null);

        /// <summary>
        /// Compresse une image pour réduire sa taille
        /// </summary>
        /// <param name="filePath">Chemin vers l'image à compresser</param>
        /// <param name="quality">Qualité de compression (0-100)</param>
        /// <param name="maxDimension">Dimension maximale (largeur ou hauteur)</param>
        /// <returns>Chemin vers l'image compressée</returns>
        Task<string> CompressImageAsync(string filePath, int quality = 80, int maxDimension = 1200);

        /// <summary>
        /// Récupère les métadonnées d'une image
        /// </summary>
        /// <param name="filePath">Chemin vers l'image</param>
        /// <returns>Les métadonnées de l'image</returns>
        Task<MediaMetadata> GetImageMetadataAsync(string filePath);

        /// <summary>
        /// Crée une entrée SpotMedia à partir d'un fichier local
        /// </summary>
        /// <param name="spotId">ID du spot associé</param>
        /// <param name="filePath">Chemin local du fichier</param>
        /// <param name="isPrimary">S'il s'agit de l'image principale</param>
        /// <param name="caption">Légende de l'image</param>
        /// <returns>L'objet SpotMedia créé</returns>
        Task<SpotMedia> CreateSpotMediaAsync(int spotId, string filePath, bool isPrimary, string caption = null);
    }

    public class MediaMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public MediaType MediaType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CaptureDate { get; set; }
    }
}
