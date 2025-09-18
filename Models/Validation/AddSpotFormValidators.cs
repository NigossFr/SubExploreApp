using SubExplore.Models.Domain;
using SubExplore.Models.ViewModels;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Validates spot name and description for Add Spot form
    /// </summary>
    public class SpotBasicInfoValidator : IStepValidator<SpotBasicInfo>
    {
        public string StepName => "Informations de base";

        public StepValidationResult Validate(SpotBasicInfo data)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(data.Name))
            {
                errors.Add("Le nom du spot est obligatoire");
            }
            else if (data.Name.Length < 3)
            {
                errors.Add("Le nom du spot doit contenir au moins 3 caractères");
            }
            else if (data.Name.Length > 100)
            {
                errors.Add("Le nom du spot ne peut pas dépasser 100 caractères");
            }

            if (string.IsNullOrWhiteSpace(data.Description))
            {
                errors.Add("La description est obligatoire");
            }
            else if (data.Description.Length < 10)
            {
                errors.Add("La description doit contenir au moins 10 caractères");
            }

            return errors.Any()
                ? StepValidationResult.Failure(StepName, errors.ToArray())
                : StepValidationResult.Success(StepName);
        }
    }

    /// <summary>
    /// Validates location data for Add Spot form
    /// </summary>
    public class SpotLocationValidator : IStepValidator<SpotLocationInfo>
    {
        public string StepName => "Localisation";

        public StepValidationResult Validate(SpotLocationInfo data)
        {
            var errors = new List<string>();

            if (data.Latitude < -90 || data.Latitude > 90)
            {
                errors.Add("La latitude doit être comprise entre -90 et 90 degrés");
            }

            if (data.Longitude < -180 || data.Longitude > 180)
            {
                errors.Add("La longitude doit être comprise entre -180 et 180 degrés");
            }

            // Validate location is in reasonable range (basic sanity check)
            if (Math.Abs(data.Latitude) < 0.001 && Math.Abs(data.Longitude) < 0.001)
            {
                errors.Add("La position semble invalide (0,0)");
            }

            if (!data.IsAccurate && data.Accuracy > 100)
            {
                errors.Add("La précision de la localisation est insuffisante (>100m)");
            }

            return errors.Any()
                ? StepValidationResult.Failure(StepName, errors.ToArray())
                : StepValidationResult.Success(StepName);
        }
    }

    /// <summary>
    /// Validates spot type selection for Add Spot form
    /// </summary>
    public class SpotTypeValidator : IStepValidator<SpotTypeInfo>
    {
        public string StepName => "Type de spot";

        public StepValidationResult Validate(SpotTypeInfo data)
        {
            var errors = new List<string>();

            if (data.SelectedSpotType == null)
            {
                if (data.HasAvailableTypes)
                {
                    errors.Add("Veuillez sélectionner le type de spot");
                }
                else
                {
                    errors.Add("Aucun type de spot disponible. Vérifiez votre connexion.");
                }
            }

            return errors.Any()
                ? StepValidationResult.Failure(StepName, errors.ToArray())
                : StepValidationResult.Success(StepName);
        }
    }

    /// <summary>
    /// Data models for form validation
    /// </summary>
    public record SpotBasicInfo(string Name, string Description);
    public record SpotLocationInfo(double Latitude, double Longitude, bool IsAccurate, double Accuracy);
    public record SpotTypeInfo(SpotType? SelectedSpotType, bool HasAvailableTypes);
}