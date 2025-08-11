using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task<bool> EnsureDatabaseCreatedAsync();
        Task<bool> MigrateDatabaseAsync();
        Task<bool> SeedDatabaseAsync();
        Task<bool> TestConnectionAsync();
        Task<bool> CleanupSpotTypesAsync();
        Task<bool> ImportRealSpotsAsync(string jsonFilePath = null);
        Task<bool> ExecuteSpotTypeCategoryMappingMigrationAsync();
        Task<string> AnalyzeFilteringIssuesAsync();
        Task<string> GetDatabaseDiagnosticsAsync();
    }
}
