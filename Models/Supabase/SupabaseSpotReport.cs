using Postgrest.Attributes;
using Postgrest.Models;

namespace SubExplore.Models.Supabase
{
    [Table("spot_reports")]
    public class SupabaseSpotReport : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("spot_id")]
        public Guid SpotId { get; set; }

        [Column("reporter_id")]
        public Guid ReporterId { get; set; }

        [Column("report_type")]
        public int ReportType { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("contact_email")]
        public string? ContactEmail { get; set; }

        [Column("status")]
        public int Status { get; set; } = 1; // Pending

        [Column("severity")]
        public int Severity { get; set; } = 1; // Low

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [Column("reviewed_by")]
        public Guid? ReviewedBy { get; set; }

        [Column("review_notes")]
        public string? ReviewNotes { get; set; }

        [Column("additional_data")]
        public Dictionary<string, object>? AdditionalData { get; set; }

        // Navigation properties (not stored in Supabase)
        public SupabaseSpot? Spot { get; set; }
        public SupabaseUser? Reporter { get; set; }
        public SupabaseUser? Reviewer { get; set; }
    }
}