using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Validation
{
    public interface IValidationService
    {
        FieldValidationResult ValidateLocation(decimal latitude, decimal longitude, string accessDescription = null);
        FieldValidationResult ValidateSpotCharacteristics(string name, string description, decimal? maxDepth, string difficultyLevel, string spotTypeId = null);
        FieldValidationResult ValidatePhotos(IEnumerable<string> photoPaths, string primaryPhotoPath = null);
        FieldValidationResult ValidateCompleteSpot(object spotData);
    }

    public class ValidationService : IValidationService
    {
        private readonly IDialogService _dialogService;

        public ValidationService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public FieldValidationResult ValidateLocation(decimal latitude, decimal longitude, string accessDescription = null)
        {
            var result = new FieldValidationResult();

            // Validate latitude range
            if (latitude < -90 || latitude > 90)
            {
                result.AddError("Latitude", "La latitude doit être comprise entre -90 et 90 degrés");
            }

            // Validate longitude range
            if (longitude < -180 || longitude > 180)
            {
                result.AddError("Longitude", "La longitude doit être comprise entre -180 et 180 degrés");
            }

            // Validate precision (should have reasonable precision for GPS coordinates)
            var latPrecision = GetDecimalPlaces(latitude);
            var lonPrecision = GetDecimalPlaces(longitude);
            
            if (latPrecision < 4 || lonPrecision < 4)
            {
                result.AddWarning("Precision", "La précision des coordonnées semble faible. Vérifiez la position.");
            }

            // Validate access description if provided
            if (!string.IsNullOrEmpty(accessDescription))
            {
                if (accessDescription.Length < 10)
                {
                    result.AddWarning("AccessDescription", "La description d'accès est très courte. Ajoutez plus de détails.");
                }
                else if (accessDescription.Length > 500)
                {
                    result.AddError("AccessDescription", "La description d'accès est trop longue (maximum 500 caractères)");
                }
            }

            return result;
        }

        public FieldValidationResult ValidateSpotCharacteristics(string name, string description, decimal? maxDepth, string difficultyLevel, string spotTypeId = null)
        {
            var result = new FieldValidationResult();

            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("Name", "Le nom du spot est obligatoire");
            }
            else if (name.Length < 3)
            {
                result.AddError("Name", "Le nom du spot doit contenir au moins 3 caractères");
            }
            else if (name.Length > 100)
            {
                result.AddError("Name", "Le nom du spot ne peut pas dépasser 100 caractères");
            }
            else if (!IsValidSpotName(name))
            {
                result.AddError("Name", "Le nom du spot contient des caractères non valides");
            }

            // Validate description
            if (string.IsNullOrWhiteSpace(description))
            {
                result.AddError("Description", "La description du spot est obligatoire");
            }
            else if (description.Length < 20)
            {
                result.AddWarning("Description", "La description est courte. Ajoutez plus de détails pour aider les plongeurs.");
            }
            else if (description.Length > 1000)
            {
                result.AddError("Description", "La description ne peut pas dépasser 1000 caractères");
            }

            // Validate depth
            if (maxDepth.HasValue)
            {
                if (maxDepth.Value <= 0)
                {
                    result.AddError("MaxDepth", "La profondeur maximale doit être positive");
                }
                else if (maxDepth.Value > 200)
                {
                    result.AddWarning("MaxDepth", "Profondeur importante (>200m). Vérifiez la valeur et les précautions.");
                }
                else if (maxDepth.Value < 1)
                {
                    result.AddWarning("MaxDepth", "Profondeur très faible. Vérifiez la valeur.");
                }
            }

            // Validate difficulty level
            if (string.IsNullOrWhiteSpace(difficultyLevel))
            {
                result.AddError("DifficultyLevel", "Le niveau de difficulté est obligatoire");
            }
            else if (!IsValidDifficultyLevel(difficultyLevel))
            {
                result.AddError("DifficultyLevel", "Niveau de difficulté non valide");
            }

            return result;
        }

        public FieldValidationResult ValidatePhotos(IEnumerable<string> photoPaths, string primaryPhotoPath = null)
        {
            var result = new FieldValidationResult();
            var paths = photoPaths?.ToList() ?? new List<string>();

            if (!paths.Any())
            {
                result.AddWarning("Photos", "Aucune photo ajoutée. Les photos aident les autres plongeurs.");
                return result;
            }

            // Validate photo count
            if (paths.Count > 10)
            {
                result.AddError("Photos", "Nombre maximum de photos dépassé (10 maximum)");
            }

            // Validate primary photo
            if (!string.IsNullOrEmpty(primaryPhotoPath) && !paths.Contains(primaryPhotoPath))
            {
                result.AddError("PrimaryPhoto", "La photo principale doit être parmi les photos sélectionnées");
            }

            // Validate photo paths
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    result.AddError("Photos", "Chemin de photo invalide détecté");
                }
                else if (!IsValidImagePath(path))
                {
                    result.AddError("Photos", $"Format de fichier non supporté: {System.IO.Path.GetExtension(path)}");
                }
            }

            return result;
        }

        public FieldValidationResult ValidateCompleteSpot(object spotData)
        {
            var result = new FieldValidationResult();
            
            // This would be implemented based on the complete spot model
            // For now, return success if individual validations pass
            
            return result;
        }

        private int GetDecimalPlaces(decimal value)
        {
            var valueString = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var decimalIndex = valueString.IndexOf('.');
            return decimalIndex == -1 ? 0 : valueString.Length - decimalIndex - 1;
        }

        private bool IsValidSpotName(string name)
        {
            // Allow letters, numbers, spaces, and common punctuation
            var regex = new Regex(@"^[a-zA-ZÀ-ÿ0-9\s\-_'.()]+$", RegexOptions.Compiled);
            return regex.IsMatch(name);
        }

        private bool IsValidDifficultyLevel(string level)
        {
            var validLevels = new[] { "Débutant", "Intermédiaire", "Avancé", "Expert" };
            return validLevels.Contains(level, StringComparer.OrdinalIgnoreCase);
        }

        private bool IsValidImagePath(string path)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var extension = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
            return validExtensions.Contains(extension);
        }
    }

    public class FieldValidationResult
    {
        public bool IsValid => !Errors.Any();
        public bool HasWarnings => Warnings.Any();
        public List<ValidationError> Errors { get; } = new List<ValidationError>();
        public List<ValidationError> Warnings { get; } = new List<ValidationError>();

        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError(field, message, ValidationSeverity.Error));
        }

        public void AddWarning(string field, string message)
        {
            Warnings.Add(new ValidationError(field, message, ValidationSeverity.Warning));
        }

        public string GetErrorsText()
        {
            return string.Join("\n", Errors.Select(e => $"• {e.Message}"));
        }

        public string GetWarningsText()
        {
            return string.Join("\n", Warnings.Select(w => $"• {w.Message}"));
        }
    }

    public class ValidationError
    {
        public string Field { get; }
        public string Message { get; }
        public ValidationSeverity Severity { get; }

        public ValidationError(string field, string message, ValidationSeverity severity)
        {
            Field = field;
            Message = message;
            Severity = severity;
        }
    }

    public enum ValidationSeverity
    {
        Warning,
        Error
    }
}