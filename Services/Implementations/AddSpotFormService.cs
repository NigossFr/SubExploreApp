using SubExplore.Models.Domain;
using SubExplore.Models.Validation;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Implementation of Add Spot form validation and operations
    /// </summary>
    public class AddSpotFormService : IAddSpotFormService
    {
        private readonly SpotBasicInfoValidator _basicInfoValidator;
        private readonly SpotLocationValidator _locationValidator;
        private readonly SpotTypeValidator _spotTypeValidator;

        public AddSpotFormService()
        {
            _basicInfoValidator = new SpotBasicInfoValidator();
            _locationValidator = new SpotLocationValidator();
            _spotTypeValidator = new SpotTypeValidator();
        }

        public StepValidationResult ValidateBasicInfo(string name, string description)
        {
            var data = new SpotBasicInfo(name, description);
            return _basicInfoValidator.Validate(data);
        }

        public StepValidationResult ValidateLocation(double latitude, double longitude, bool isAccurate, double accuracy)
        {
            var data = new SpotLocationInfo(latitude, longitude, isAccurate, accuracy);
            return _locationValidator.Validate(data);
        }

        public StepValidationResult ValidateSpotType(SpotType? selectedSpotType, bool hasAvailableTypes)
        {
            var data = new SpotTypeInfo(selectedSpotType, hasAvailableTypes);
            return _spotTypeValidator.Validate(data);
        }

        public StepValidationResult ValidateCompleteForm(AddSpotFormData formData)
        {
            var results = new List<StepValidationResult>
            {
                ValidateBasicInfo(formData.Name, formData.Description),
                ValidateLocation(formData.Latitude, formData.Longitude, formData.IsAccurate, formData.Accuracy),
                ValidateSpotType(formData.SelectedSpotType, formData.HasAvailableTypes)
            };

            var allErrors = results.Where(r => !r.IsValid)
                                  .SelectMany(r => r.Errors)
                                  .ToList();

            return allErrors.Any()
                ? StepValidationResult.Failure("Formulaire complet", allErrors.ToArray())
                : StepValidationResult.Success("Formulaire complet");
        }

        public string CreateValidationSummary(List<StepValidationResult> results)
        {
            var invalidResults = results.Where(r => !r.IsValid).ToList();

            if (!invalidResults.Any())
                return string.Empty;

            var summary = new List<string>();

            foreach (var result in invalidResults)
            {
                summary.Add($"**{result.StepName}:**");
                summary.AddRange(result.Errors.Select(error => $"• {error}"));
            }

            return string.Join("\n", summary);
        }

        public bool CanSubmitForm(AddSpotFormData formData)
        {
            var completeValidation = ValidateCompleteForm(formData);
            return completeValidation.IsValid;
        }
    }
}