using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SubExplore.Models.Domain
{
    /// <summary>
    /// Represents a user's favorite spot relationship with comprehensive tracking and validation
    /// </summary>
    [Table("UserFavoriteSpots")]
    [Index(nameof(UserId), nameof(SpotId), IsUnique = true)]
    [Index(nameof(UserId), nameof(CreatedAt))]
    [Index(nameof(UserId), nameof(Priority), nameof(CreatedAt))]
    public class UserFavoriteSpot
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID of the user who favorited the spot
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be positive")]
        public int UserId { get; set; }

        /// <summary>
        /// ID of the favorited spot
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Spot ID must be positive")]
        public int SpotId { get; set; }

        /// <summary>
        /// When the spot was added to favorites
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the favorite was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Optional notes about why this spot is favorited
        /// </summary>
        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Priority level for organizing favorites (1 = highest priority, 10 = lowest)
        /// </summary>
        [Range(1, 10, ErrorMessage = "Priority must be between 1 and 10")]
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Whether the user wants notifications about this spot
        /// </summary>
        public bool NotificationEnabled { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("SpotId")]
        public virtual Spot? Spot { get; set; }

        /// <summary>
        /// Gets the priority display text
        /// </summary>
        [NotMapped]
        public string PriorityDisplayText => Priority switch
        {
            1 => "Très haute",
            2 => "Haute", 
            3 => "Moyenne-haute",
            4 => "Moyenne",
            5 => "Normale",
            6 => "Moyenne-basse",
            7 => "Basse",
            8 => "Très basse",
            9 => "Faible",
            10 => "Très faible",
            _ => "Inconnue"
        };

        /// <summary>
        /// Gets whether this is a high priority favorite (1-3)
        /// </summary>
        [NotMapped]
        public bool IsHighPriority => Priority <= 3;

        /// <summary>
        /// Gets the age of this favorite in days
        /// </summary>
        [NotMapped]
        public int AgeInDays => (DateTime.UtcNow - CreatedAt).Days;

        /// <summary>
        /// Validates the favorite spot data
        /// </summary>
        public bool IsValid(out string[] errors)
        {
            var errorList = new List<string>();

            if (UserId <= 0)
                errorList.Add("User ID must be positive");

            if (SpotId <= 0)
                errorList.Add("Spot ID must be positive");

            if (Priority < 1 || Priority > 10)
                errorList.Add("Priority must be between 1 and 10");

            if (!string.IsNullOrEmpty(Notes) && Notes.Length > 500)
                errorList.Add("Notes cannot exceed 500 characters");

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Updates the timestamp
        /// </summary>
        public void MarkAsUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"UserFavoriteSpot(User:{UserId}, Spot:{SpotId}, Priority:{Priority})";
        }
    }
}