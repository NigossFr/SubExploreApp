using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Enhanced authentication service with complete role management and professional features
    /// Consolidates all authentication functionality into a single, comprehensive interface
    /// </summary>
    public interface IEnhancedAuthenticationService
    {
        #region Core Authentication
        
        /// <summary>
        /// Current authenticated user
        /// </summary>
        User? CurrentUser { get; }

        /// <summary>
        /// Current user ID if authenticated
        /// </summary>
        Guid? CurrentUserId { get; }

        /// <summary>
        /// Check if user is currently authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Event raised when authentication state changes
        /// </summary>
        event EventHandler<SubExplore.Models.DTOs.AuthenticationStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Initialize authentication service and restore session if valid
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        Task<AuthenticationResult> LoginAsync(string email, string password);

        /// <summary>
        /// Register new user account with enhanced validation
        /// </summary>
        Task<AuthenticationResult> RegisterAsync(UserRegistrationRequest request);

        /// <summary>
        /// Logout current user and clear session
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Refresh expired access token using refresh token
        /// </summary>
        Task<bool> RefreshTokenAsync();

        /// <summary>
        /// Validate current authentication state
        /// </summary>
        Task<bool> ValidateAuthenticationAsync();

        #endregion

        #region Password Management

        /// <summary>
        /// Change user password with enhanced security validation
        /// </summary>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);

        /// <summary>
        /// Request password reset email with rate limiting
        /// </summary>
        Task<SubExplore.Models.DTOs.PasswordResetResult> RequestPasswordResetAsync(string email);

        /// <summary>
        /// Reset password using token
        /// </summary>
        Task<bool> ResetPasswordAsync(string token, string email, string newPassword);

        /// <summary>
        /// Validate password strength against requirements
        /// </summary>
        Task<PasswordValidationResult> ValidatePasswordStrengthAsync(string password);

        #endregion

        #region Role Management

        /// <summary>
        /// Elevate user to expert moderator status
        /// </summary>
        Task<bool> ElevateToModeratorAsync(Guid userId, ModeratorSpecialization specialization, string justification);

        /// <summary>
        /// Update moderator status (for admin use)
        /// </summary>
        Task<bool> UpdateModeratorStatusAsync(Guid userId, ModeratorStatus status, string reason);

        /// <summary>
        /// Nominate user for moderator elevation
        /// </summary>
        Task<bool> NominateForModeratorAsync(Guid userId, ModeratorSpecialization specialization, string recommendation);

        /// <summary>
        /// Get moderator application status
        /// </summary>
        Task<ModeratorApplicationStatus> GetModeratorApplicationStatusAsync(Guid userId);

        /// <summary>
        /// Update user permissions (admin only)
        /// </summary>
        Task<bool> UpdateUserPermissionsAsync(Guid userId, UserPermissions permissions);

        #endregion

        #region Professional Verification

        /// <summary>
        /// Submit professional account verification request
        /// </summary>
        Task<bool> RequestProfessionalVerificationAsync(Guid userId, ProfessionalVerificationRequest request);

        /// <summary>
        /// Verify professional account with organization
        /// </summary>
        Task<bool> VerifyProfessionalAccountAsync(Guid userId, Guid verificationRequestId, bool approved, string notes);

        /// <summary>
        /// Get professional verification status
        /// </summary>
        Task<ProfessionalVerificationStatus> GetProfessionalVerificationStatusAsync(Guid userId);

        /// <summary>
        /// Update organization association
        /// </summary>
        Task<bool> UpdateOrganizationAssociationAsync(Guid userId, Guid? organizationId);

        #endregion

        #region Profile Management

        /// <summary>
        /// Update user profile with comprehensive validation
        /// </summary>
        Task<bool> UpdateProfileAsync(User user);

        /// <summary>
        /// Update user expertise level with validation
        /// </summary>
        Task<bool> UpdateExpertiseLevelAsync(Guid userId, ExpertiseLevel expertiseLevel, Dictionary<string, object>? certifications);

        /// <summary>
        /// Verify user email address
        /// </summary>
        Task<bool> VerifyEmailAsync(Guid userId, string verificationToken);

        /// <summary>
        /// Request email verification resend
        /// </summary>
        Task<bool> ResendEmailVerificationAsync(Guid userId);

        /// <summary>
        /// Update user avatar
        /// </summary>
        Task<bool> UpdateAvatarAsync(Guid userId, string avatarUrl);

        /// <summary>
        /// Get user profile by ID
        /// </summary>
        Task<User?> GetUserProfileAsync(Guid userId);

        /// <summary>
        /// Search users with advanced filters
        /// </summary>
        Task<IEnumerable<User>> SearchUsersAsync(UserSearchCriteria criteria);

        #endregion

        #region Account Management

        /// <summary>
        /// Suspend user account
        /// </summary>
        Task<bool> SuspendAccountAsync(Guid userId, string reason, DateTime? suspendUntil = null);

        /// <summary>
        /// Reactivate suspended account
        /// </summary>
        Task<bool> ReactivateAccountAsync(Guid userId, string reason);

        /// <summary>
        /// Delete user account (GDPR compliance)
        /// </summary>
        Task<bool> DeleteAccountAsync(Guid userId, string reason);

        /// <summary>
        /// Get account activity log
        /// </summary>
        Task<IEnumerable<AccountActivity>> GetAccountActivityAsync(Guid userId, int pageSize = 50, int page = 0);

        #endregion

        #region Analytics & Reporting

        /// <summary>
        /// Get user statistics
        /// </summary>
        Task<UserStatistics> GetUserStatisticsAsync(Guid userId);

        /// <summary>
        /// Get authentication metrics (admin only)
        /// </summary>
        Task<AuthenticationMetrics> GetAuthenticationMetricsAsync(DateTime from, DateTime to);

        /// <summary>
        /// Get moderator performance metrics
        /// </summary>
        Task<ModeratorMetrics> GetModeratorMetricsAsync(Guid moderatorId, DateTime from, DateTime to);

        #endregion
    }

    // AuthenticationStateChangedEventArgs and AuthenticationEventType moved to AuthenticationModels.cs to avoid duplication
}