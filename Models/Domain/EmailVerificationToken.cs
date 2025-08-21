// Models/Tokens - Nouveaux modèles pour la sécurité
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubExplore.Models.Domain
{
    public class EmailVerificationToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }
        
        [NotMapped]
        public string? CreatedFromIP { get; set; }
        
        [NotMapped]
        public string? UsedFromIP { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int AttemptCount { get; set; } = 0;
        public int MaxAttempts { get; set; } = 5;
        
        // Propriété calculée pour compatibilité
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        
        public User User { get; set; } = null!;
    }
}