using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service hybride pour la carte utilisant Entity Framework + PostGIS pour les performances
    /// et gardant l'API Supabase pour certaines fonctionnalités spécifiques
    /// </summary>
    public interface IHybridMapService
    {
        // Méthodes utilisant Entity Framework + PostGIS pour les performances
        Task<IEnumerable<PracticeSpot>> GetNearbyPracticeSpotsAsync(decimal latitude, decimal longitude, int radiusKm = 10, Guid? spotTypeFilter = null);
        Task<IEnumerable<Organization>> GetNearbyOrganizationsAsync(decimal latitude, decimal longitude, int radiusKm = 10);
        Task<IEnumerable<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, int radiusKm = 10);
        
        // Méthodes de recherche et filtrage
        Task<IEnumerable<PracticeSpot>> SearchPracticeSpotsAsync(string query);
        Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByTypeAsync(Guid spotTypeId);
        Task<IEnumerable<PracticeSpot>> GetPracticeSpotsByDifficultyAsync(Models.Enums.DifficultyLevel difficulty);
        
        // Méthodes pour les SpotTypes (peut rester avec l'API si nécessaire)
        Task<IEnumerable<SpotType>> GetActiveSpotTypesAsync();
    }
}