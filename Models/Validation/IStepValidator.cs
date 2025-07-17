using System.ComponentModel.DataAnnotations;

namespace SubExplore.Models.Validation
{
    /// <summary>
    /// Generic interface for step-specific validation in multi-step forms
    /// </summary>
    /// <typeparam name="T">The data type to validate</typeparam>
    public interface IStepValidator<T>
    {
        /// <summary>
        /// Validates the provided data
        /// </summary>
        /// <param name="data">The data to validate</param>
        /// <returns>Validation result with success status and error messages</returns>
        ValidationResult Validate(T data);

        /// <summary>
        /// Gets the step name for this validator
        /// </summary>
        string StepName { get; }
    }

    /// <summary>
    /// Validation result for step validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string StepName { get; set; } = string.Empty;

        public static ValidationResult Success(string stepName) => new ValidationResult 
        { 
            IsValid = true, 
            StepName = stepName 
        };

        public static ValidationResult Failure(string stepName, params string[] errors) => new ValidationResult
        {
            IsValid = false,
            StepName = stepName,
            Errors = errors.ToList()
        };
    }
}