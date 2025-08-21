// ========================================
// SUPABASE CLIENT SERVICE INTERFACE
// ========================================
// Interface pour le service client Supabase unifié

using Supabase;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service client Supabase unifié
    /// Gère l'initialisation et fournit l'accès au client Supabase
    /// </summary>
    public interface ISupabaseClientService
    {
        /// <summary>
        /// Indique si le client Supabase est initialisé et prêt
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// Initialise le client Supabase
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Obtient l'instance du client Supabase
        /// </summary>
        Client GetClient();
        
        /// <summary>
        /// Obtient l'instance du client Supabase (asynchrone)
        /// </summary>
        Task<Client> GetClientAsync();
        
        /// <summary>
        /// Obtient le statut de la connexion
        /// </summary>
        string GetConnectionStatus();
    }
}