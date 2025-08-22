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
    }
}