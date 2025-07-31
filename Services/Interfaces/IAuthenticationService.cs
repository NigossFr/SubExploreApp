using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Core authentication service for user login, logout, and session management
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Current authenticated user
        /// </summary>
        User? CurrentUser { get; }

        /// <summary>
        /// Current user ID if authenticated
        /// </summary>
        int? CurrentUserId { get; }

        /// <summary>
        /// Check if user is currently authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Event raised when authentication state changes
        /// </summary>
        event EventHandler<AuthenticationStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        /// <param name="email">User email address</param>
        /// <param name="password">User password</param>
        /// <returns>Authentication result with user data and tokens</returns>
        Task<AuthenticationResult> LoginAsync(string email, string password);

        /// <summary>
        /// Register new user account
        /// </summary>
        /// <param name="registerRequest">User registration data</param>
        /// <returns>Registration result</returns>
        Task<AuthenticationResult> RegisterAsync(UserRegistrationRequest registerRequest);

        /// <summary>
        /// Refresh expired access token using refresh token
        /// </summary>
        /// <returns>True if token refresh successful</returns>
        Task<bool> RefreshTokenAsync();

        /// <summary>
        /// Logout current user and clear session
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Initialize authentication service and restore session if valid
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Validate current authentication state
        /// </summary>
        /// <returns>True if authentication is valid</returns>
        Task<bool> ValidateAuthenticationAsync();

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if password change successful</returns>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);

        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <param name="email">User email address</param>
        /// <returns>True if reset email sent</returns>
        Task<bool> RequestPasswordResetAsync(string email);

        /// <summary>
        /// Elevate user to expert moderator status
        /// </summary>
        /// <param name="userId">User ID to elevate</param>
        /// <param name="specialization">Moderator specialization</param>
        /// <returns>True if elevation successful</returns>
        Task<bool> ElevateToModeratorAsync(int userId, ModeratorSpecialization specialization);

        /// <summary>
        /// Update moderator status (for admin use)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="status">New moderator status</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateModeratorStatusAsync(int userId, ModeratorStatus status);

        /// <summary>
        /// Verify professional account with organization
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="organizationId">Organization ID (future Organizations table)</param>
        /// <returns>True if verification successful</returns>
        Task<bool> VerifyProfessionalAccountAsync(int userId, int? organizationId = null);

        /// <summary>
        /// Update user permissions (admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="permissions">New permissions</param>
        /// <returns>True if update successful</returns>
        Task<bool> UpdateUserPermissionsAsync(int userId, UserPermissions permissions);
    }

    /// <summary>
    /// Authentication state change event arguments
    /// </summary>
    public class AuthenticationStateChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; set; }
        public User? User { get; set; }
        public string? Reason { get; set; }
    }
}