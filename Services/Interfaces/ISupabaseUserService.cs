// ========================================
// INTERFACE POUR SERVICE USERS SUPABASE
// ========================================

using SubExplore.Models.Supabase;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des utilisateurs via l'API Supabase
    /// </summary>
    public interface ISupabaseUserService
    {
        /// <summary>
        /// Récupère un utilisateur par email
        /// </summary>
        Task<SupabaseUser?> GetUserByEmailAsync(string email);
        
        /// <summary>
        /// Récupère un utilisateur par ID
        /// </summary>
        Task<SupabaseUser?> GetUserByIdAsync(Guid userId);
        
        /// <summary>
        /// Récupère un utilisateur par nom d'utilisateur
        /// </summary>
        Task<SupabaseUser?> GetUserByUsernameAsync(string username);
        
        /// <summary>
        /// Crée un nouvel utilisateur
        /// </summary>
        Task<SupabaseUser> CreateUserAsync(SupabaseUser user);
        
        /// <summary>
        /// Met à jour un utilisateur existant
        /// </summary>
        Task<SupabaseUser> UpdateUserAsync(SupabaseUser user);
        
        /// <summary>
        /// Met à jour la date de dernière connexion
        /// </summary>
        Task<bool> UpdateLastLoginAsync(Guid userId);
        
        /// <summary>
        /// Confirme l'email d'un utilisateur
        /// </summary>
        Task<bool> ConfirmEmailAsync(Guid userId);
        
        /// <summary>
        /// Vérifie si un email existe déjà
        /// </summary>
        Task<bool> EmailExistsAsync(string email);
        
        /// <summary>
        /// Vérifie si un nom d'utilisateur existe déjà
        /// </summary>
        Task<bool> UsernameExistsAsync(string username);
        
        /// <summary>
        /// Met à jour le mot de passe d'un utilisateur
        /// </summary>
        Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash);
        
        /// <summary>
        /// Convertit un SupabaseUser vers le modèle Domain
        /// </summary>
        User ConvertToDomainModel(SupabaseUser supabaseUser);
        
        /// <summary>
        /// Convertit un modèle Domain vers SupabaseUser
        /// </summary>
        SupabaseUser ConvertFromDomainModel(User user);
    }
}