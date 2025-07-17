using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    public interface IMapDiagnosticService
    {
        Task<bool> CheckGoogleMapsConfigurationAsync();
        Task<string> GetMapDiagnosticInfoAsync();
        Task<bool> TestMapTileLoadingAsync();
    }
}