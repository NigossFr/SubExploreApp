using SubExplore.Models.Domain;
using SubExplore.Models.Enums;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Data model for location step validation
    /// </summary>
    public class LocationStepData
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string AccessDescription { get; set; } = string.Empty;
        public bool HasUserLocation { get; set; }
    }

    /// <summary>
    /// Data model for characteristics step validation
    /// </summary>
    public class CharacteristicsStepData
    {
        public string SpotName { get; set; } = string.Empty;
        public SpotType? SelectedSpotType { get; set; }
        public DifficultyLevel SelectedDifficultyLevel { get; set; }
        public int MaxDepth { get; set; }
        public string RequiredEquipment { get; set; } = string.Empty;
        public string SafetyNotes { get; set; } = string.Empty;
        public string BestConditions { get; set; } = string.Empty;
        public CurrentStrength SelectedCurrentStrength { get; set; }
    }

    /// <summary>
    /// Data model for photos step validation
    /// </summary>
    public class PhotosStepData
    {
        public List<string> PhotosPaths { get; set; } = new List<string>();
        public string? PrimaryPhotoPath { get; set; }
        public int MaxPhotosAllowed { get; set; } = 3;
    }

    /// <summary>
    /// Data model for summary step validation
    /// </summary>
    public class SummaryStepData
    {
        public LocationStepData Location { get; set; } = new();
        public CharacteristicsStepData Characteristics { get; set; } = new();
        public PhotosStepData Photos { get; set; } = new();
    }
}