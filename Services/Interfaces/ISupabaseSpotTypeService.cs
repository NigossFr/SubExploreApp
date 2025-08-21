// ========================================
// INTERFACE POUR SERVICE SPOT TYPES SUPABASE
// ========================================

using SubExplore.Models.Supabase;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des types de spots via l'API Supabase
    /// </summary>
    public interface ISupabaseSpotTypeService
    {
        /// <summary>
        /// Récupère tous les types de spots actifs
        /// </summary>
        Task<List<SupabaseSpotType>> GetActiveSpotTypesAsync();
        
        /// <summary>
        /// Récupère un type de spot par ID
        /// </summary>
        Task<SupabaseSpotType?> GetSpotTypeByIdAsync(Guid typeId);
        
        /// <summary>
        /// Récupère les types de spots par catégorie
        /// </summary>
        Task<List<SupabaseSpotType>> GetSpotTypesByCategoryAsync(string category);
        
        /// <summary>
        /// Crée un nouveau type de spot
        /// </summary>
        Task<SupabaseSpotType> CreateSpotTypeAsync(SupabaseSpotType spotType);
        
        /// <summary>
        /// Met à jour un type de spot
        /// </summary>
        Task<SupabaseSpotType> UpdateSpotTypeAsync(SupabaseSpotType spotType);
        
        /// <summary>
        /// Active/désactive un type de spot
        /// </summary>
        Task<bool> SetSpotTypeActiveAsync(Guid typeId, bool isActive);
        
        /// <summary>
        /// Convertit un SupabaseSpotType vers le modèle Domain
        /// </summary>
        SpotType ConvertToDomainModel(SupabaseSpotType supabaseSpotType);
        
        /// <summary>
        /// Convertit un modèle Domain vers SupabaseSpotType
        /// </summary>
        SupabaseSpotType ConvertFromDomainModel(SpotType spotType);
    }
}