using SubExplore.Models.Domain;

namespace SubExplore.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for managing revoked tokens
    /// </summary>
    public interface IRevokedTokenRepository : IGenericRepository<RevokedToken>
    {
        /// <summary>
        /// Check if a token is revoked by its hash
        /// </summary>
        /// <param name="tokenHash">SHA256 hash of the token</param>
        /// <returns>True if token is revoked</returns>
        Task<bool> IsTokenRevokedAsync(string tokenHash);

        /// <summary>
        /// Revoke a token with metadata
        /// </summary>
        /// <param name="tokenHash">SHA256 hash of the token</param>
        /// <param name="tokenType">Type of token (refresh_token, access_token)</param>
        /// <param name="userId">User ID who owns the token</param>
        /// <param name="expiresAt">When the token expires</param>
        /// <param name="reason">Reason for revocation</param>
        /// <param name="ipAddress">IP address of revocation request</param>
        Task RevokeTokenAsync(string tokenHash, string tokenType, int? userId = null, 
            DateTime? expiresAt = null, string? reason = null, string? ipAddress = null);

        /// <summary>
        /// Revoke all tokens for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Reason for revocation</param>
        Task RevokeAllUserTokensAsync(int userId, string reason = RevocationReasons.UserLogout);

        /// <summary>
        /// Clean up expired revoked tokens (for maintenance)
        /// </summary>
        /// <param name="olderThan">Remove revoked tokens older than this date</param>
        /// <returns>Number of tokens cleaned up</returns>
        Task<int> CleanupExpiredTokensAsync(DateTime? olderThan = null);

        /// <summary>
        /// Get revoked tokens for a user (for audit purposes)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="limit">Maximum number of records to return</param>
        Task<List<RevokedToken>> GetUserRevokedTokensAsync(int userId, int limit = 50);
    }
}