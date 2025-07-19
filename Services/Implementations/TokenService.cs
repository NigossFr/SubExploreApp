using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SubExplore.Models.Configuration;
using SubExplore.Models.Domain;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// JWT token service implementation with secure token management and database-backed revocation
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly ISecureConfigurationService _secureConfig;
        private readonly IRevokedTokenRepository _revokedTokenRepository;
        private readonly ILogger<TokenService> _logger;
        private JwtConfiguration? _jwtConfig;

        public TokenService(
            ISecureConfigurationService secureConfig,
            IRevokedTokenRepository revokedTokenRepository,
            ILogger<TokenService> logger)
        {
            _secureConfig = secureConfig;
            _revokedTokenRepository = revokedTokenRepository;
            _logger = logger;
        }

        public string GenerateAccessToken(int userId, string email, IEnumerable<Claim>? claims = null)
        {
            try
            {
                var config = GetJwtConfigurationAsync().GetAwaiter().GetResult();
                
                var tokenClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, 
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                        ClaimValueTypes.Integer64)
                };

                // Add additional claims if provided
                if (claims != null)
                {
                    tokenClaims.AddRange(claims);
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: config.Issuer,
                    audience: config.Audience,
                    claims: tokenClaims,
                    expires: DateTime.UtcNow.AddMinutes(config.AccessTokenExpirationMinutes),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogDebug("Access token generated for user {UserId}", userId);
                
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user {UserId}", userId);
                throw new SecurityTokenException("Failed to generate access token", ex);
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                var randomBytes = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TokenService] Error generating refresh token: {ex.Message}");
                throw new SecurityTokenException("Failed to generate refresh token", ex);
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var config = GetJwtConfigurationAsync().GetAwaiter().GetResult();
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(config.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = config.Issuer,
                    ValidateAudience = true,
                    ValidAudience = config.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(config.ClockSkewSeconds)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Ensure token is JWT and uses correct algorithm
                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Token validation failed: {Message}", ex.Message);
                return null;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);
                var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                 principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return int.TryParse(userIdClaim, out var userId) ? userId : null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetEmailFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);
                return principal?.FindFirst(ClaimTypes.Email)?.Value ??
                       principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            }
            catch
            {
                return null;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo <= DateTime.UtcNow;
            }
            catch
            {
                return true; // Consider invalid tokens as expired
            }
        }

        public TimeSpan? GetTokenExpirationTime(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var remaining = jwtToken.ValidTo - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            catch
            {
                return null;
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Attempted to revoke null or empty refresh token");
                    return;
                }

                // Hash the token for secure storage
                var tokenHash = ComputeTokenHash(refreshToken);
                
                // Get user ID if possible (for audit trail)
                int? userId = null;
                try
                {
                    userId = GetUserIdFromToken(refreshToken);
                }
                catch
                {
                    // Token might be malformed, but still revoke the hash
                }

                // Store in database with metadata
                await _revokedTokenRepository.RevokeTokenAsync(
                    tokenHash,
                    TokenTypes.RefreshToken,
                    userId,
                    DateTime.UtcNow.AddDays(30), // Refresh tokens typically have longer expiration
                    RevocationReasons.UserLogout
                );

                _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                throw;
            }
        }

        public async Task<bool> IsRefreshTokenRevokedAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                    return true;

                // Hash the token for lookup
                var tokenHash = ComputeTokenHash(refreshToken);
                
                // Check database
                var isRevoked = await _revokedTokenRepository.IsTokenRevokedAsync(tokenHash);
                
                return isRevoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token revocation status");
                // Fail safe - consider token revoked if we can't check
                return true;
            }
        }

        /// <summary>
        /// Get JWT configuration with caching
        /// </summary>
        private async Task<JwtConfiguration> GetJwtConfigurationAsync()
        {
            if (_jwtConfig == null)
            {
                _jwtConfig = await _secureConfig.GetJwtConfigurationAsync();
            }
            return _jwtConfig;
        }

        /// <summary>
        /// Compute SHA256 hash of token for secure storage
        /// </summary>
        private static string ComputeTokenHash(string token)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Revoke all tokens for a user (for password changes, account deactivation, etc.)
        /// </summary>
        public async Task RevokeAllUserTokensAsync(int userId, string reason = RevocationReasons.PasswordChanged)
        {
            try
            {
                await _revokedTokenRepository.RevokeAllUserTokensAsync(userId, reason);
                _logger.LogInformation("All tokens revoked for user {UserId}, reason: {Reason}", userId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
                throw;
            }
        }
    }
}