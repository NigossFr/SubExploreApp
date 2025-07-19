using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SubExplore.Models.Domain;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// JWT token management service for secure token operations
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generate JWT access token for authenticated user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="claims">Additional claims to include</param>
        /// <returns>JWT access token</returns>
        string GenerateAccessToken(int userId, string email, IEnumerable<Claim>? claims = null);

        /// <summary>
        /// Generate secure refresh token
        /// </summary>
        /// <returns>Refresh token string</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Validate JWT token and extract claims
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>Claims principal if valid, null if invalid</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Extract user ID from JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User ID if valid token, null otherwise</returns>
        int? GetUserIdFromToken(string token);

        /// <summary>
        /// Extract email from JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Email if valid token, null otherwise</returns>
        string? GetEmailFromToken(string token);

        /// <summary>
        /// Check if token is expired
        /// </summary>
        /// <param name="token">JWT token to check</param>
        /// <returns>True if token is expired</returns>
        bool IsTokenExpired(string token);

        /// <summary>
        /// Get remaining time before token expires
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Time remaining before expiration, null if invalid token</returns>
        TimeSpan? GetTokenExpirationTime(string token);

        /// <summary>
        /// Revoke refresh token (add to blacklist)
        /// </summary>
        /// <param name="refreshToken">Refresh token to revoke</param>
        Task RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Check if refresh token is revoked
        /// </summary>
        /// <param name="refreshToken">Refresh token to check</param>
        /// <returns>True if token is revoked</returns>
        Task<bool> IsRefreshTokenRevokedAsync(string refreshToken);

        /// <summary>
        /// Revoke all tokens for a user (for password changes, account deactivation, etc.)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Reason for revocation</param>
        Task RevokeAllUserTokensAsync(int userId, string reason = RevocationReasons.PasswordChanged);
    }
}