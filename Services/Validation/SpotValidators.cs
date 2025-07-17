using SubExplore.Models.Validation;

namespace SubExplore.Services.Validation
{
    /// <summary>
    /// Validator for location step data
    /// </summary>
    public class LocationStepValidator : IStepValidator<LocationStepData>
    {
        public string StepName => "Location";

        public ValidationResult Validate(LocationStepData data)
        {
            var errors = new List<string>();

            // Validate coordinates
            if (data.Latitude == 0 && data.Longitude == 0)
            {
                errors.Add("La localisation est requise.");
            }

            if (data.Latitude < -90 || data.Latitude > 90)
            {
                errors.Add("La latitude doit être comprise entre -90 et 90 degrés.");
            }

            if (data.Longitude < -180 || data.Longitude > 180)
            {
                errors.Add("La longitude doit être comprise entre -180 et 180 degrés.");
            }

            // Validate access description
            if (string.IsNullOrWhiteSpace(data.AccessDescription))
            {
                errors.Add("La description de l'accès est requise.");
            }
            else if (data.AccessDescription.Length < 10)
            {
                errors.Add("La description de l'accès doit contenir au moins 10 caractères.");
            }

            return errors.Count == 0 
                ? ValidationResult.Success(StepName)
                : ValidationResult.Failure(StepName, errors.ToArray());
        }
    }

    /// <summary>
    /// Validator for characteristics step data
    /// </summary>
    public class CharacteristicsStepValidator : IStepValidator<CharacteristicsStepData>
    {
        public string StepName => "Characteristics";

        public ValidationResult Validate(CharacteristicsStepData data)
        {
            var errors = new List<string>();

            // Validate spot name
            if (string.IsNullOrWhiteSpace(data.SpotName))
            {
                errors.Add("Le nom du spot est requis.");
            }
            else if (data.SpotName.Length < 3)
            {
                errors.Add("Le nom du spot doit contenir au moins 3 caractères.");
            }

            // Validate spot type
            if (data.SelectedSpotType == null)
            {
                errors.Add("Le type de spot est requis.");
            }

            // Validate depth
            if (data.MaxDepth < 0)
            {
                errors.Add("La profondeur maximale ne peut pas être négative.");
            }
            else if (data.MaxDepth > 200)
            {
                errors.Add("La profondeur maximale ne peut pas dépasser 200 mètres.");
            }

            // Validate required equipment
            if (string.IsNullOrWhiteSpace(data.RequiredEquipment))
            {
                errors.Add("L'équipement requis est obligatoire.");
            }

            // Validate safety notes
            if (string.IsNullOrWhiteSpace(data.SafetyNotes))
            {
                errors.Add("Les notes de sécurité sont obligatoires.");
            }

            // Validate best conditions
            if (string.IsNullOrWhiteSpace(data.BestConditions))
            {
                errors.Add("Les meilleures conditions sont obligatoires.");
            }

            return errors.Count == 0 
                ? ValidationResult.Success(StepName)
                : ValidationResult.Failure(StepName, errors.ToArray());
        }
    }

    /// <summary>
    /// Validator for photos step data
    /// </summary>
    public class PhotosStepValidator : IStepValidator<PhotosStepData>
    {
        public string StepName => "Photos";

        public ValidationResult Validate(PhotosStepData data)
        {
            var errors = new List<string>();

            // Validate photo count
            if (data.PhotosPaths.Count == 0)
            {
                errors.Add("Au moins une photo est requise.");
            }
            else if (data.PhotosPaths.Count > data.MaxPhotosAllowed)
            {
                errors.Add($"Le nombre maximum de photos autorisées est {data.MaxPhotosAllowed}.");
            }

            // Validate primary photo
            if (data.PhotosPaths.Count > 0 && string.IsNullOrEmpty(data.PrimaryPhotoPath))
            {
                errors.Add("Une photo principale doit être sélectionnée.");
            }

            if (!string.IsNullOrEmpty(data.PrimaryPhotoPath) && !data.PhotosPaths.Contains(data.PrimaryPhotoPath))
            {
                errors.Add("La photo principale doit être dans la liste des photos.");
            }

            // Validate photo paths
            foreach (var photoPath in data.PhotosPaths)
            {
                if (string.IsNullOrWhiteSpace(photoPath))
                {
                    errors.Add("Un chemin de photo ne peut pas être vide.");
                }
            }

            return errors.Count == 0 
                ? ValidationResult.Success(StepName)
                : ValidationResult.Failure(StepName, errors.ToArray());
        }
    }

    /// <summary>
    /// Validator for summary step data
    /// </summary>
    public class SummaryStepValidator : IStepValidator<SummaryStepData>
    {
        private readonly LocationStepValidator _locationValidator;
        private readonly CharacteristicsStepValidator _characteristicsValidator;
        private readonly PhotosStepValidator _photosValidator;

        public string StepName => "Summary";

        public SummaryStepValidator()
        {
            _locationValidator = new LocationStepValidator();
            _characteristicsValidator = new CharacteristicsStepValidator();
            _photosValidator = new PhotosStepValidator();
        }

        public ValidationResult Validate(SummaryStepData data)
        {
            var allErrors = new List<string>();

            // Validate all previous steps
            var locationResult = _locationValidator.Validate(data.Location);
            if (!locationResult.IsValid)
            {
                allErrors.AddRange(locationResult.Errors.Select(e => $"Localisation: {e}"));
            }

            var characteristicsResult = _characteristicsValidator.Validate(data.Characteristics);
            if (!characteristicsResult.IsValid)
            {
                allErrors.AddRange(characteristicsResult.Errors.Select(e => $"Caractéristiques: {e}"));
            }

            var photosResult = _photosValidator.Validate(data.Photos);
            if (!photosResult.IsValid)
            {
                allErrors.AddRange(photosResult.Errors.Select(e => $"Photos: {e}"));
            }

            return allErrors.Count == 0 
                ? ValidationResult.Success(StepName)
                : ValidationResult.Failure(StepName, allErrors.ToArray());
        }
    }
}