// ========================================
// INTERFACE POUR SERVICE API SUPABASE
// ========================================

using SubExplore.Models.Supabase;

namespace SubExplore.Services.Interfaces
{
    public interface ISupabaseApiService
    {
        /// <summary>
        /// Test simple de connexion
        /// </summary>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Récupère tous les utilisateurs
        /// </summary>
        Task<List<SupabaseUser>> GetUsersAsync();

        /// <summary>
        /// Récupère un utilisateur par email
        /// </summary>
        Task<SupabaseUser?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Récupère tous les spot types
        /// </summary>
        Task<List<SupabaseSpotType>> GetSpotTypesAsync();

        /// <summary>
        /// Crée un nouveau type de spot
        /// </summary>
        Task<SupabaseSpotType> CreateSpotTypeAsync(SupabaseSpotType spotType);

        /// <summary>
        /// Récupère tous les spots (ancien modèle, obsolète)
        /// </summary>
        [Obsolete("Utilisez GetPracticeSpotsAsync, GetOrganizationsAsync, GetBusinessesAsync pour la nouvelle architecture 3-tables")]
        Task<List<SupabaseSpot>> GetSpotsAsync();

        // ========================================
        // NOUVELLE ARCHITECTURE 3-TABLES
        // ========================================

        /// <summary>
        /// Récupère tous les spots de pratique
        /// </summary>
        Task<List<SupabasePracticeSpot>> GetPracticeSpotsAsync();

        /// <summary>
        /// Récupère toutes les organisations
        /// </summary>
        Task<List<SupabaseOrganization>> GetOrganizationsAsync();

        /// <summary>
        /// Récupère tous les commerces
        /// </summary>
        Task<List<SupabaseBusiness>> GetBusinessesAsync();

        /// <summary>
        /// Crée un nouveau spot de pratique
        /// </summary>
        Task<SupabasePracticeSpot> CreatePracticeSpotAsync(SupabasePracticeSpot practiceSpot);

        /// <summary>
        /// Crée une nouvelle organisation
        /// </summary>
        Task<SupabaseOrganization> CreateOrganizationAsync(SupabaseOrganization organization);

        /// <summary>
        /// Crée un nouveau commerce
        /// </summary>
        Task<SupabaseBusiness> CreateBusinessAsync(SupabaseBusiness business);

        /// <summary>
        /// Crée un nouveau spot
        /// </summary>
        Task<SupabaseSpot> CreateSpotAsync(SupabaseSpot spot);

        /// <summary>
        /// Met à jour un spot existant
        /// </summary>
        Task<SupabaseSpot> UpdateSpotAsync(SupabaseSpot spot);

        /// <summary>
        /// Supprime un spot
        /// </summary>
        Task<bool> DeleteSpotAsync(Guid spotId);

        /// <summary>
        /// Récupère un spot par son ID
        /// </summary>
        Task<SupabaseSpot?> GetSpotByIdAsync(Guid spotId);

        /// <summary>
        /// Crée un nouvel utilisateur
        /// </summary>
        Task<SupabaseUser> CreateUserAsync(SupabaseUser user);

        /// <summary>
        /// Met à jour un utilisateur
        /// </summary>
        Task<SupabaseUser> UpdateUserAsync(SupabaseUser user);

        /// <summary>
        /// Supprime un utilisateur
        /// </summary>
        Task DeleteUserAsync(Guid userId);

        /// <summary>
        /// Exécute un test complet avec toutes les opérations
        /// </summary>
        Task<bool> RunCompleteTestAsync();

        // ========================================
        // SPOT REPORTS & EDITING
        // ========================================

        /// <summary>
        /// Create a new spot report
        /// </summary>
        Task<SupabaseSpotReport> CreateSpotReportAsync(SupabaseSpotReport report);

        /// <summary>
        /// Get reports for a specific spot
        /// </summary>
        Task<List<SupabaseSpotReport>> GetSpotReportsAsync(int spotId);

        /// <summary>
        /// Get user's reports
        /// </summary>
        Task<List<SupabaseSpotReport>> GetUserReportsAsync(Guid userId);

        /// <summary>
        /// Get pending reports for moderation
        /// </summary>
        Task<List<SupabaseSpotReport>> GetPendingReportsAsync();

        /// <summary>
        /// Update report status
        /// </summary>
        Task<bool> UpdateSpotReportAsync(int reportId, int status, string reviewNotes, Guid reviewerId);

        /// <summary>
        /// Update spot basic information
        /// </summary>
        Task<bool> UpdateSpotBasicInfoAsync(Guid spotId, string name, string description, 
            string requiredEquipment, string safetyNotes, string bestConditions);

        /// <summary>
        /// Update spot location
        /// </summary>
        Task<bool> UpdateSpotLocationAsync(Guid spotId, decimal latitude, decimal longitude);

        /// <summary>
        /// Update spot technical details
        /// </summary>
        Task<bool> UpdateSpotTechnicalDetailsAsync(Guid spotId, int? maxDepth, int difficultyLevel, int? currentStrength);

        // ========================================
        // SPOT MEDIA MANAGEMENT
        // ========================================

        /// <summary>
        /// Upload image to Supabase Storage
        /// </summary>
        Task<string?> UploadImageAsync(Stream imageStream, string fileName, string bucketPath);

        /// <summary>
        /// Delete image from Supabase Storage
        /// </summary>
        Task<bool> DeleteImageAsync(string imagePath);

        /// <summary>
        /// Create spot media record
        /// </summary>
        Task<SupabaseSpotMedia> CreateSpotMediaAsync(SupabaseSpotMedia media);

        /// <summary>
        /// Get spot media
        /// </summary>
        Task<List<SupabaseSpotMedia>> GetSpotMediaAsync(int spotId);

        /// <summary>
        /// Delete spot media
        /// </summary>
        Task<bool> DeleteSpotMediaAsync(int mediaId);

        /// <summary>
        /// Update spot media metadata
        /// </summary>
        Task<bool> UpdateSpotMediaAsync(int mediaId, string? caption, bool? isPrimary);

        /// <summary>
        /// Set primary photo for spot
        /// </summary>
        Task<bool> SetPrimarySpotPhotoAsync(int spotId, int photoId);

        /// <summary>
        /// Upload image to Supabase Storage
        /// </summary>
        Task<(bool Success, string PublicUrl)> UploadImageAsync(string bucket, string fileName, byte[] imageData);


        // ========================================
        // FAVORITES MANAGEMENT
        // ========================================

        /// <summary>
        /// Récupère tous les favoris d'un utilisateur
        /// </summary>
        Task<List<SupabaseUserFavoriteSpot>> GetUserFavoritesAsync(Guid userId);

        /// <summary>
        /// Vérifie si un spot est en favoris pour un utilisateur
        /// </summary>
        Task<bool> IsSpotFavoriteAsync(Guid userId, Guid spotId);

        /// <summary>
        /// Ajoute un spot aux favoris d'un utilisateur
        /// </summary>
        Task<SupabaseUserFavoriteSpot> AddToFavoritesAsync(Guid userId, Guid spotId, int priority = 5, string? notes = null, bool notificationEnabled = true);

        /// <summary>
        /// Retire un spot des favoris d'un utilisateur
        /// </summary>
        Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid spotId);

        /// <summary>
        /// Met à jour les notes d'un favori
        /// </summary>
        Task<bool> UpdateFavoriteNotesAsync(Guid userId, Guid spotId, string? notes);

        /// <summary>
        /// Met à jour la priorité d'un favori
        /// </summary>
        Task<bool> UpdateFavoritePriorityAsync(Guid userId, Guid spotId, int priority);

        /// <summary>
        /// Met à jour les notifications d'un favori
        /// </summary>
        Task<bool> UpdateFavoriteNotificationAsync(Guid userId, Guid spotId, bool enabled);

        /// <summary>
        /// Compte le nombre de favoris pour un spot
        /// </summary>
        Task<int> GetSpotFavoritesCountAsync(Guid spotId);
    }
}