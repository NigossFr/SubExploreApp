using System.ComponentModel.DataAnnotations;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Domain
{
    public class SpotReport
    {
        public Guid Id { get; set; }
        
        public Guid SpotId { get; set; }
        
        public Guid ReporterId { get; set; }
        
        [Required]
        public SpotReportType ReportType { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? ContactEmail { get; set; }
        
        public SpotReportStatus Status { get; set; } = SpotReportStatus.Pending;
        
        public SpotReportSeverity Severity { get; set; } = SpotReportSeverity.Low;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReviewedAt { get; set; }
        
        public Guid? ReviewedBy { get; set; }
        
        [StringLength(500)]
        public string? ReviewNotes { get; set; }
        
        public Dictionary<string, object>? AdditionalData { get; set; }
        
        // Relations
        public Spot Spot { get; set; } = null!;
        public User Reporter { get; set; } = null!;
        public User? Reviewer { get; set; }
    }
}