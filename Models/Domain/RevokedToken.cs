using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Entity for tracking revoked JWT tokens
    /// </summary>
    [Table("RevokedTokens")]
    public class RevokedToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The revoked token (hashed for security)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Token type (refresh_token, access_token)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TokenType { get; set; } = string.Empty;

        /// <summary>
        /// User ID who owned the token
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// When the token was revoked
        /// </summary>
        [Required]
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the token expires (for cleanup)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Reason for revocation
        /// </summary>
        [MaxLength(200)]
        public string? RevocationReason { get; set; }

        /// <summary>
        /// IP address from which revocation was requested
        /// </summary>
        [MaxLength(45)]
        public string? RevocationIpAddress { get; set; }

        // Navigation property
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Token types enumeration
    /// </summary>
    public static class TokenTypes
    {
        public const string RefreshToken = "refresh_token";
        public const string AccessToken = "access_token";
    }

    /// <summary>
    /// Revocation reasons enumeration
    /// </summary>
    public static class RevocationReasons
    {
        public const string UserLogout = "user_logout";
        public const string TokenExpired = "token_expired";
        public const string SecurityBreach = "security_breach";
        public const string PasswordChanged = "password_changed";
        public const string AccountDeactivated = "account_deactivated";
        public const string AdminRevoked = "admin_revoked";
    }
}