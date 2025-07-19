using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        /// <summary>
        /// Récupère un utilisateur par son email
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Récupère un utilisateur par son nom d'utilisateur
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get user by email for authentication
        /// </summary>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Get user by username for authentication
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Vérifie si un email est déjà utilisé
        /// </summary>
        Task<bool> IsEmailTakenAsync(string email);

        /// <summary>
        /// Vérifie si un nom d'utilisateur est déjà utilisé
        /// </summary>
        Task<bool> IsUsernameTakenAsync(string username);

        /// <summary>
        /// Récupère les utilisateurs par type de compte
        /// </summary>
        Task<IEnumerable<User>> GetByAccountTypeAsync(AccountType accountType);

        /// <summary>
        /// Récupère les utilisateurs qui ont contribué à la création de spots
        /// </summary>
        Task<IEnumerable<User>> GetTopContributorsAsync(int count = 10);

        /// <summary>
        /// Récupère les préférences d'un utilisateur
        /// </summary>
        Task<UserPreferences> GetUserPreferencesAsync(int userId);

        /// <summary>
        /// Met à jour les préférences d'un utilisateur
        /// </summary>
        Task UpdateUserPreferencesAsync(UserPreferences preferences);
    }
}