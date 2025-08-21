// ========================================
// INTERFACE POUR SERVICE SPOTS SUPABASE
// ========================================

using SubExplore.Models.Supabase;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des spots via l'API Supabase
    /// </summary>
    public interface ISupabaseSpotService
    {
        /// <summary>
        /// Récupère tous les spots approuvés
        /// </summary>
        Task<List<SupabaseSpot>> GetApprovedSpotsAsync();
        
        /// <summary>
        /// Récupère un spot par ID
        /// </summary>
        Task<SupabaseSpot?> GetSpotByIdAsync(Guid spotId);
        
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
        /// Recherche des spots par nom ou description
        /// </summary>
        Task<List<SupabaseSpot>> SearchSpotsAsync(string searchText);
        
        /// <summary>
        /// Récupère les spots dans une zone géographique
        /// </summary>
        Task<List<SupabaseSpot>> GetSpotsByLocationAsync(decimal latitude, decimal longitude, double radiusKm);
        
        /// <summary>
        /// Récupère les spots d'un utilisateur
        /// </summary>
        Task<List<SupabaseSpot>> GetUserSpotsAsync(Guid userId);
        
        /// <summary>
        /// Convertit un SupabaseSpot vers le modèle Domain
        /// </summary>
        Spot ConvertToDomainModel(SupabaseSpot supabaseSpot);
        
        /// <summary>
        /// Convertit un modèle Domain vers SupabaseSpot
        /// </summary>
        SupabaseSpot ConvertFromDomainModel(Spot spot);
    }
}