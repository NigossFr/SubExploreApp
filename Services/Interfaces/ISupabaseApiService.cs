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
        /// Récupère tous les spots
        /// </summary>
        Task<List<SupabaseSpot>> GetSpotsAsync();

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