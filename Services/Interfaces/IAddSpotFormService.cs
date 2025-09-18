using SubExplore.Models.Domain;
using SubExplore.Models.Validation;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Service for Add Spot form operations and validation
    /// Separates form logic from ViewModel for better testability and maintainability
    /// </summary>
    public interface IAddSpotFormService
    {
        /// <summary>
        /// Validates basic spot information (name, description)
        /// </summary>
        StepValidationResult ValidateBasicInfo(string name, string description);

        /// <summary>
        /// Validates location information
        /// </summary>
        StepValidationResult ValidateLocation(double latitude, double longitude, bool isAccurate, double accuracy);

        /// <summary>
        /// Validates spot type selection
        /// </summary>
        StepValidationResult ValidateSpotType(SpotType? selectedSpotType, bool hasAvailableTypes);

        /// <summary>
        /// Validates the complete form for final submission
        /// </summary>
        StepValidationResult ValidateCompleteForm(AddSpotFormData formData);

        /// <summary>
        /// Creates a summary of validation errors for display
        /// </summary>
        string CreateValidationSummary(List<StepValidationResult> results);

        /// <summary>
        /// Checks if the form can be submitted
        /// </summary>
        bool CanSubmitForm(AddSpotFormData formData);
    }

    /// <summary>
    /// Complete form data for validation and submission
    /// </summary>
    public class AddSpotFormData
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAccurate { get; set; }
        public double Accuracy { get; set; }
        public SpotType? SelectedSpotType { get; set; }
        public bool HasAvailableTypes { get; set; }
    }
}