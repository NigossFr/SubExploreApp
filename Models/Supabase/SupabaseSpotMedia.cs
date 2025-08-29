using Postgrest.Attributes;
using Postgrest.Models;

namespace SubExplore.Models.Supabase
{
    [Table("spot_media")]
    public class SupabaseSpotMedia : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("spot_id")]
        public Guid SpotId { get; set; }

        [Column("media_type")]
        public int MediaType { get; set; } = 1; // Photo

        [Column("media_url")]
        public string MediaUrl { get; set; } = string.Empty;

        [Column("caption")]
        public string? Caption { get; set; }

        [Column("is_primary")]
        public bool IsPrimary { get; set; } = false;

        [Column("width")]
        public int? Width { get; set; }

        [Column("height")]
        public int? Height { get; set; }

        [Column("file_size")]
        public long? FileSize { get; set; }

        [Column("content_type")]
        public string? ContentType { get; set; }

        [Column("status")]
        public int Status { get; set; } = 1; // Processing

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public SupabaseSpot? Spot { get; set; }
    }
}