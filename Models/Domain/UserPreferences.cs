using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubExplore.Models.Domain
{
    public class UserPreferences
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string Theme { get; set; } = "light";

        public string DisplayNamePreference { get; set; } = "username";

        [Column(TypeName = "json")]
        public string NotificationSettings { get; set; } = "{}";

        public string Language { get; set; } = "fr";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
