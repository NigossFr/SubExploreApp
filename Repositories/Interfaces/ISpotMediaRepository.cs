using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface ISpotMediaRepository : IGenericRepository<SpotMedia>
    {
        Task<IEnumerable<SpotMedia>> GetBySpotIdAsync(int spotId);
        Task<SpotMedia?> GetPrimaryMediaForSpotAsync(int spotId);
    }
}
