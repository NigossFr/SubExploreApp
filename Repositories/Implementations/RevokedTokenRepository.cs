using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for managing revoked tokens
    /// </summary>
    public class RevokedTokenRepository : GenericRepository<RevokedToken>, IRevokedTokenRepository
    {
        private readonly ILogger<RevokedTokenRepository> _logger;

        public RevokedTokenRepository(SubExploreDbContext context, ILogger<RevokedTokenRepository> logger) 
            : base(context)
        {
            _logger = logger;
        }

        public async Task<bool> IsTokenRevokedAsync(string tokenHash)
        {
            try
            {
                if (string.IsNullOrEmpty(tokenHash))
                    return false;

                var isRevoked = await _context.Set<RevokedToken>()
                    .AnyAsync(rt => rt.TokenHash == tokenHash);

                return isRevoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is revoked: {TokenHash}", tokenHash);
                // Fail safe - consider token revoked if we can't check
                return true;
            }
        }

        public async Task RevokeTokenAsync(string tokenHash, string tokenType, int? userId = null, 
            DateTime? expiresAt = null, string? reason = null, string? ipAddress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(tokenHash))
                    throw new ArgumentException("Token hash cannot be null or empty", nameof(tokenHash));

                // Check if token is already revoked
                var existingRevocation = await _context.Set<RevokedToken>()
                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                if (existingRevocation != null)
                {
                    _logger.LogDebug("Token already revoked: {TokenHash}", tokenHash);
                    return;
                }

                var revokedToken = new RevokedToken
                {
                    TokenHash = tokenHash,
                    TokenType = tokenType,
                    UserId = userId,
                    RevokedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    RevocationReason = reason,
                    RevocationIpAddress = ipAddress
                };

                await AddAsync(revokedToken);
                await SaveChangesAsync();

                _logger.LogInformation("Token revoked successfully: Type={TokenType}, UserId={UserId}, Reason={Reason}", 
                    tokenType, userId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token: {TokenHash}", tokenHash);
                throw;
            }
        }

        public async Task RevokeAllUserTokensAsync(int userId, string reason = RevocationReasons.UserLogout)
        {
            try
            {
                // Note: This requires storing user tokens or implementing a different strategy
                // For now, we'll log this action for audit purposes
                _logger.LogInformation("Revoking all tokens for user: {UserId}, Reason: {Reason}", userId, reason);

                // In a more complete implementation, you might:
                // 1. Keep track of active tokens per user
                // 2. Add a "revoke all tokens before timestamp" field to User entity
                // 3. Use JWT black/whitelisting strategies

                // For immediate implementation, we can add a record indicating all tokens should be considered revoked
                var revokedToken = new RevokedToken
                {
                    TokenHash = $"ALL_TOKENS_USER_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    TokenType = "all_tokens",
                    UserId = userId,
                    RevokedAt = DateTime.UtcNow,
                    RevocationReason = reason,
                };

                await AddAsync(revokedToken);
                await SaveChangesAsync();

                _logger.LogInformation("All tokens marked for revocation for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> CleanupExpiredTokensAsync(DateTime? olderThan = null)
        {
            try
            {
                var cutoffDate = olderThan ?? DateTime.UtcNow.AddDays(-90); // Default: clean tokens older than 90 days

                var expiredTokens = await _context.Set<RevokedToken>()
                    .Where(rt => rt.RevokedAt < cutoffDate || 
                                (rt.ExpiresAt.HasValue && rt.ExpiresAt.Value < DateTime.UtcNow))
                    .ToListAsync();

                if (expiredTokens.Count > 0)
                {
                    _context.Set<RevokedToken>().RemoveRange(expiredTokens);
                    await SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} expired revoked tokens", expiredTokens.Count);
                }

                return expiredTokens.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
                throw;
            }
        }

        public async Task<List<RevokedToken>> GetUserRevokedTokensAsync(int userId, int limit = 50)
        {
            try
            {
                var revokedTokens = await _context.Set<RevokedToken>()
                    .Where(rt => rt.UserId == userId)
                    .OrderByDescending(rt => rt.RevokedAt)
                    .Take(limit)
                    .ToListAsync();

                return revokedTokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revoked tokens for user: {UserId}", userId);
                throw;
            }
        }
    }
}