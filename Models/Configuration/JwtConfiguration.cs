using System.ComponentModel.DataAnnotations;

namespace SubExplore.Models.Configuration
{
    /// <summary>
    /// JWT configuration settings with validation
    /// </summary>
    public class JwtConfiguration
    {
        /// <summary>
        /// Secret key for JWT signing (minimum 256 bits)
        /// </summary>
        [Required]
        [MinLength(32, ErrorMessage = "JWT secret key must be at least 32 characters (256 bits)")]
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Token issuer
        /// </summary>
        [Required]
        public string Issuer { get; set; } = "SubExplore";

        /// <summary>
        /// Token audience
        /// </summary>
        [Required]
        public string Audience { get; set; } = "SubExploreApp";

        /// <summary>
        /// Access token expiration in minutes (default: 15 minutes)
        /// </summary>
        [Range(5, 1440, ErrorMessage = "Access token expiration must be between 5 minutes and 24 hours")]
        public int AccessTokenExpirationMinutes { get; set; } = 15;

        /// <summary>
        /// Refresh token expiration in days (default: 7 days)
        /// </summary>
        [Range(1, 90, ErrorMessage = "Refresh token expiration must be between 1 and 90 days")]
        public int RefreshTokenExpirationDays { get; set; } = 7;

        /// <summary>
        /// Clock skew allowance in seconds (default: 300 seconds / 5 minutes)
        /// </summary>
        [Range(0, 600, ErrorMessage = "Clock skew must be between 0 and 10 minutes")]
        public int ClockSkewSeconds { get; set; } = 300;

        /// <summary>
        /// Enable token encryption (recommended for production)
        /// </summary>
        public bool EnableTokenEncryption { get; set; } = false;

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(this, context, results, true))
            {
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Validation error"));
            }

            // Additional security validations
            if (SecretKey?.Length < 32)
            {
                errors.Add("JWT secret key is too weak. Use at least 32 characters (256 bits)");
            }

            if (AccessTokenExpirationMinutes > 60)
            {
                errors.Add("Access token expiration should not exceed 1 hour for security");
            }

            if (RefreshTokenExpirationDays > 30)
            {
                errors.Add("Refresh token expiration should not exceed 30 days for security");
            }

            return (errors.Count == 0, errors);
        }
    }
}