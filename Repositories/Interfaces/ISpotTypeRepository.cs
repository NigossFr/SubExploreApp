using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface ISpotTypeRepository : IGenericRepository<SpotType>
    {
        Task<IEnumerable<SpotType>> GetByActivityCategoryAsync(ActivityCategory category);
        Task<IEnumerable<SpotType>> GetActiveTypesAsync();
        Task<IEnumerable<SpotType>> GetActiveSpotTypesAsync(); // Alias optimisé pour les performances
    }
}
