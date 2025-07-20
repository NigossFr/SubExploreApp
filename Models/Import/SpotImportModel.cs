using System.Text.Json.Serialization;

namespace SubExplore.Models.Import
{
    public class SpotImportModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public decimal Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal Longitude { get; set; }

        [JsonPropertyName("maxDepth")]
        public int MaxDepth { get; set; }

        [JsonPropertyName("difficultyLevel")]
        public string DifficultyLevel { get; set; } = string.Empty;

        [JsonPropertyName("spotType")]
        public string SpotType { get; set; } = string.Empty;

        [JsonPropertyName("currentStrength")]
        public string CurrentStrength { get; set; } = string.Empty;

        [JsonPropertyName("bestConditions")]
        public string BestConditions { get; set; } = string.Empty;

        [JsonPropertyName("safetyNotes")]
        public string SafetyNotes { get; set; } = string.Empty;

        [JsonPropertyName("requiredEquipment")]
        public string RequiredEquipment { get; set; } = string.Empty;

        [JsonPropertyName("accessInfo")]
        public string? AccessInfo { get; set; }

        [JsonPropertyName("facilities")]
        public string? Facilities { get; set; }

        [JsonPropertyName("bestSeasons")]
        public List<string>? BestSeasons { get; set; }

        [JsonPropertyName("validationStatus")]
        public string ValidationStatus { get; set; } = "En attente";
    }

    public class SpotsImportFile
    {
        [JsonPropertyName("spots")]
        public List<SpotImportModel> Spots { get; set; } = new List<SpotImportModel>();
    }
}