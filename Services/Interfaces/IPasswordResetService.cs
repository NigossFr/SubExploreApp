using SubExplore.Models.Domain;
using SubExplore.Models.DTOs;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Password reset service for secure password recovery
    /// </summary>
    public interface IPasswordResetService
    {
        /// <summary>
        /// Request password reset for email address
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="ipAddress">IP address of request</param>
        /// <returns>Password reset result</returns>
        Task<PasswordResetResult> RequestPasswordResetAsync(string email, string? ipAddress = null);

        /// <summary>
        /// Validate password reset token
        /// </summary>
        /// <param name="token">Reset token</param>
        /// <param name="email">Email address</param>
        /// <returns>Token validation result</returns>
        Task<PasswordResetTokenValidation> ValidateResetTokenAsync(string token, string email);

        /// <summary>
        /// Reset password using valid token
        /// </summary>
        /// <param name="token">Reset token</param>
        /// <param name="email">Email address</param>
        /// <param name="newPassword">New password</param>
        /// <param name="ipAddress">IP address of reset attempt</param>
        /// <returns>Password reset result</returns>
        Task<PasswordResetResult> ResetPasswordAsync(string token, string email, string newPassword, string? ipAddress = null);

        /// <summary>
        /// Check if email has reached daily reset limit
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if limit reached</returns>
        Task<bool> HasReachedDailyLimitAsync(string email);

        /// <summary>
        /// Get password reset statistics
        /// </summary>
        /// <returns>Reset statistics</returns>
        Task<PasswordResetStatistics> GetResetStatisticsAsync();

        /// <summary>
        /// Clean up expired reset tokens
        /// </summary>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync();
    }

    // Models moved to Models/DTOs/AuthenticationModels.cs to avoid duplication
}