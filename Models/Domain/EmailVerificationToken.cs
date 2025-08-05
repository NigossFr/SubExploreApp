using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Email verification token for user account activation
    /// </summary>
    public class EmailVerificationToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User ID this token belongs to
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Secure verification token (hashed for storage)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Email address this token is valid for
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration date
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether the token has been used
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Date when token was used (null if not used)
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// IP address from which token was created
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string? CreatedFromIP { get; set; }

        /// <summary>
        /// IP address from which token was used
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string? UsedFromIP { get; set; }

        /// <summary>
        /// Token creation date
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of verification attempts made with this token
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        /// <summary>
        /// Maximum allowed attempts before token is disabled
        /// </summary>
        public int MaxAttempts { get; set; } = 5;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Check if token is valid (not expired, not used, attempts not exceeded)
        /// </summary>
        [NotMapped]
        public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt && AttemptCount < MaxAttempts;

        /// <summary>
        /// Check if token is expired
        /// </summary>
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}