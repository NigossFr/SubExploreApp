using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubExplore.Models.Domain
{
    public class RevokedToken
    {
        public Guid Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public string? RevocationReason { get; set; }
        
        [NotMapped]
        public string? RevocationIpAddress { get; set; }
        
        public User? User { get; set; }
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