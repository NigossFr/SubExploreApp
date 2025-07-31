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
        StepValidationResult Validate(T data);

        /// <summary>
        /// Gets the step name for this validator
        /// </summary>
        string StepName { get; }
    }

    /// <summary>
    /// Step validation result for step validation (renamed to avoid conflict)
    /// </summary>
    public class StepValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string StepName { get; set; } = string.Empty;

        public static StepValidationResult Success(string stepName) => new StepValidationResult 
        { 
            IsValid = true, 
            StepName = stepName 
        };

        public static StepValidationResult Failure(string stepName, params string[] errors) => new StepValidationResult
        {
            IsValid = false,
            StepName = stepName,
            Errors = errors.ToList()
        };
    }
}