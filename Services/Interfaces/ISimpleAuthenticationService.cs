using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service d'authentification simplifié pour l'API Supabase uniquement
    /// Ne dépend pas des repositories Entity Framework
    /// </summary>
    public interface ISimpleAuthenticationService
    {
        /// <summary>
        /// Initialise le service d'authentification
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Indique si l'utilisateur est authentifié
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Obtient l'utilisateur actuel
        /// </summary>
        User? CurrentUser { get; }
        
        /// <summary>
        /// Connexion avec email et mot de passe
        /// </summary>
        /// <param name="email">Email de l'utilisateur</param>
        /// <param name="password">Mot de passe</param>
        /// <returns>True si la connexion réussit</returns>
        Task<bool> LoginSimpleAsync(string email, string password);
        
        /// <summary>
        /// Inscription d'un nouvel utilisateur
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Mot de passe</param>
        /// <param name="firstName">Prénom</param>
        /// <param name="lastName">Nom</param>
        /// <returns>True si l'inscription réussit</returns>
        Task<bool> RegisterSimpleAsync(string email, string password, string firstName, string lastName);
        
        /// <summary>
        /// Déconnexion de l'utilisateur actuel
        /// </summary>
        Task LogoutAsync();
    }
}