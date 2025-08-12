using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Models.Validation;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Strategy interface for spot validation logic
    /// </summary>
    public interface IValidationStrategy
    {
        ModeratorSpecialization Specialization { get; }
        Task<ValidationResult> CanValidateSpotAsync(Spot spot, User validator);
        Task<ValidationResult> ValidateSpotContentAsync(Spot spot);
        Task<List<SafetyRecommendation>> GetSafetyRecommendationsAsync(Spot spot);
        int Priority { get; }
    }

    /// <summary>
    /// Abstract base class for validation strategies
    /// </summary>
    public abstract class ValidationStrategyBase : IValidationStrategy
    {
        public abstract ModeratorSpecialization Specialization { get; }
        public virtual int Priority => 1;

        public virtual async Task<ValidationResult> CanValidateSpotAsync(Spot spot, User validator)
        {
            // Basic permission check
            if (validator.AccountType != AccountType.Administrator && 
                validator.AccountType != AccountType.ExpertModerator)
            {
                return ValidationResult.CreateError("User does not have validation permissions");
            }

            // Check specialization match
            if (validator.ModeratorSpecialization != Specialization && 
                validator.ModeratorSpecialization != ModeratorSpecialization.CommunityManagement &&
                validator.ModeratorSpecialization != ModeratorSpecialization.SafetyAndRegulations)
            {
                return ValidationResult.CreateError($"Validator specialization {validator.ModeratorSpecialization} does not match required {Specialization}");
            }

            return ValidationResult.CreateSuccess();
        }

        public abstract Task<ValidationResult> ValidateSpotContentAsync(Spot spot);
        public abstract Task<List<SafetyRecommendation>> GetSafetyRecommendationsAsync(Spot spot);

        protected ValidationResult CreateContentValidationError(List<string> errors)
        {
            return ValidationResult.CreateValidationError(errors);
        }

        protected List<string> ValidateBasicSpotInfo(Spot spot)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(spot.Name))
                errors.Add("Spot name is required");

            if (string.IsNullOrWhiteSpace(spot.Description))
                errors.Add("Spot description is required");

            if (spot.Latitude < -90 || spot.Latitude > 90)
                errors.Add("Invalid latitude value");

            if (spot.Longitude < -180 || spot.Longitude > 180)
                errors.Add("Invalid longitude value");

            if (spot.MaxDepth <= 0)
                errors.Add("Maximum depth must be greater than 0");

            return errors;
        }
    }

    /// <summary>
    /// Validation strategy for diving spots
    /// </summary>
    public class DiveSpotValidationStrategy : ValidationStrategyBase
    {
        public override ModeratorSpecialization Specialization => ModeratorSpecialization.DiveSpots;
        public override int Priority => 2;

        public override async Task<ValidationResult> ValidateSpotContentAsync(Spot spot)
        {
            var errors = ValidateBasicSpotInfo(spot);

            // Diving-specific validations
            if (spot.Type?.Category != ActivityCategory.Diving)
            {
                errors.Add("Spot type must be diving category for dive spot validation");
            }

            if (spot.MaxDepth > 100 && spot.DifficultyLevel < DifficultyLevel.Advanced)
            {
                errors.Add("Spots deeper than 100m require Advanced or Expert difficulty level");
            }

            if (spot.CurrentStrength == CurrentStrength.Extreme && spot.DifficultyLevel < DifficultyLevel.Expert)
            {
                errors.Add("Spots with extreme currents require Expert difficulty level");
            }

            if (string.IsNullOrWhiteSpace(spot.RequiredEquipment))
            {
                errors.Add("Required equipment specification is mandatory for diving spots");
            }

            // Check for technical diving requirements
            if (spot.MaxDepth > 40 || spot.DifficultyLevel >= DifficultyLevel.Expert)
            {
                if (!spot.RequiredEquipment.ToLower().Contains("nitrox") && 
                    !spot.RequiredEquipment.ToLower().Contains("technical"))
                {
                    errors.Add("Deep or expert spots should specify technical equipment requirements");
                }
            }

            return errors.Any() ? CreateContentValidationError(errors) : ValidationResult.CreateSuccess();
        }

        public override async Task<List<SafetyRecommendation>> GetSafetyRecommendationsAsync(Spot spot)
        {
            var recommendations = new List<SafetyRecommendation>();

            if (spot.MaxDepth > 30)
            {
                recommendations.Add(new SafetyRecommendation
                {
                    Title = "Deep Diving Certification",
                    Description = "Ensure divers have appropriate deep diving certification",
                    Type = SafetyRecommendationType.Training,
                    IsMandatory = true
                });
            }

            if (spot.CurrentStrength >= CurrentStrength.Strong)
            {
                recommendations.Add(new SafetyRecommendation
                {
                    Title = "Current Experience Required",
                    Description = "Divers should have experience with strong currents",
                    Type = SafetyRecommendationType.Training,
                    IsMandatory = true
                });
            }

            if (spot.MaxDepth > 40)
            {
                recommendations.Add(new SafetyRecommendation
                {
                    Title = "Technical Diving Equipment",
                    Description = "Consider technical diving equipment for optimal safety",
                    Type = SafetyRecommendationType.Equipment,
                    IsMandatory = false
                });
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Validation strategy for freediving spots
    /// </summary>
    public class FreediveSpotValidationStrategy : ValidationStrategyBase
    {
        public override ModeratorSpecialization Specialization => ModeratorSpecialization.FreediveSpots;

        public override async Task<ValidationResult> ValidateSpotContentAsync(Spot spot)
        {
            var errors = ValidateBasicSpotInfo(spot);

            if (spot.Type?.Category != ActivityCategory.Freediving)
            {
                errors.Add("Spot type must be freediving category");
            }

            // Freediving-specific validations
            if (spot.MaxDepth > 20 && string.IsNullOrWhiteSpace(spot.SafetyNotes))
            {
                errors.Add("Safety notes are required for freediving spots deeper than 20m");
            }

            if (spot.CurrentStrength >= CurrentStrength.Strong)
            {
                errors.Add("Strong currents are not recommended for freediving spots");
            }

            // Check for buddy system mention
            if (!spot.SafetyNotes.ToLower().Contains("buddy") && 
                !spot.Description.ToLower().Contains("buddy"))
            {
                errors.Add("Buddy system should be mentioned for freediving safety");
            }

            return errors.Any() ? CreateContentValidationError(errors) : ValidationResult.CreateSuccess();
        }

        public override async Task<List<SafetyRecommendation>> GetSafetyRecommendationsAsync(Spot spot)
        {
            var recommendations = new List<SafetyRecommendation>();

            recommendations.Add(new SafetyRecommendation
            {
                Title = "Buddy System Mandatory",
                Description = "Never freedive alone - always use buddy system",
                Type = SafetyRecommendationType.Supervision,
                IsMandatory = true
            });

            if (spot.MaxDepth > 15)
            {
                recommendations.Add(new SafetyRecommendation
                {
                    Title = "Advanced Freediving Training",
                    Description = "Recommended for depths over 15m",
                    Type = SafetyRecommendationType.Training,
                    IsMandatory = false
                });
            }

            recommendations.Add(new SafetyRecommendation
            {
                Title = "Emergency Procedures",
                Description = "Ensure knowledge of freediving emergency procedures",
                Type = SafetyRecommendationType.Emergency,
                IsMandatory = true
            });

            return recommendations;
        }
    }

    /// <summary>
    /// Validation strategy for snorkeling spots
    /// </summary>
    public class SnorkelSpotValidationStrategy : ValidationStrategyBase
    {
        public override ModeratorSpecialization Specialization => ModeratorSpecialization.SnorkelSpots;

        public override async Task<ValidationResult> ValidateSpotContentAsync(Spot spot)
        {
            var errors = ValidateBasicSpotInfo(spot);

            if (spot.Type?.Category != ActivityCategory.Snorkeling)
            {
                errors.Add("Spot type must be snorkeling category");
            }

            // Snorkeling-specific validations
            if (spot.MaxDepth > 5)
            {
                errors.Add("Snorkeling spots should typically not exceed 5m depth");
            }

            if (spot.DifficultyLevel > DifficultyLevel.Intermediate)
            {
                errors.Add("Snorkeling spots should not exceed Intermediate difficulty");
            }

            if (spot.CurrentStrength > CurrentStrength.Moderate)
            {
                errors.Add("Strong currents are not suitable for snorkeling spots");
            }

            return errors.Any() ? CreateContentValidationError(errors) : ValidationResult.CreateSuccess();
        }

        public override async Task<List<SafetyRecommendation>> GetSafetyRecommendationsAsync(Spot spot)
        {
            var recommendations = new List<SafetyRecommendation>();

            recommendations.Add(new SafetyRecommendation
            {
                Title = "Swimming Ability Required",
                Description = "Ensure adequate swimming skills for snorkeling",
                Type = SafetyRecommendationType.Training,
                IsMandatory = true
            });

            if (spot.CurrentStrength >= CurrentStrength.Moderate)
            {
                recommendations.Add(new SafetyRecommendation
                {
                    Title = "Current Awareness",
                    Description = "Be aware of current conditions and strength",
                    Type = SafetyRecommendationType.Timing,
                    IsMandatory = true
                });
            }

            recommendations.Add(new SafetyRecommendation
            {
                Title = "Stay Close to Shore",
                Description = "Maintain reasonable distance from shore or boat",
                Type = SafetyRecommendationType.Route,
                IsMandatory = false
            });

            return recommendations;
        }
    }

    /// <summary>
    /// Factory for creating validation strategies
    /// </summary>
    public interface IValidationStrategyFactory
    {
        IValidationStrategy GetStrategy(ActivityCategory category);
        IValidationStrategy GetStrategy(ModeratorSpecialization specialization);
        List<IValidationStrategy> GetAllStrategies();
    }

    /// <summary>
    /// Implementation of validation strategy factory
    /// </summary>
    public class ValidationStrategyFactory : IValidationStrategyFactory
    {
        private readonly Dictionary<ActivityCategory, IValidationStrategy> _categoryStrategies;
        private readonly Dictionary<ModeratorSpecialization, IValidationStrategy> _specializationStrategies;

        public ValidationStrategyFactory()
        {
            var strategies = new List<IValidationStrategy>
            {
                new DiveSpotValidationStrategy(),
                new FreediveSpotValidationStrategy(),
                new SnorkelSpotValidationStrategy()
            };

            // Since all categories now map to Activity, use the first strategy as default
            _categoryStrategies = new Dictionary<ActivityCategory, IValidationStrategy>
            {
                { ActivityCategory.Activity, strategies.First() },
                { ActivityCategory.Structure, strategies.First() },
                { ActivityCategory.Shop, strategies.First() },
                { ActivityCategory.Other, strategies.First() }
            };
            _specializationStrategies = strategies.ToDictionary(s => s.Specialization, s => s);
        }

        public IValidationStrategy GetStrategy(ActivityCategory category)
        {
            return _categoryStrategies.TryGetValue(category, out var strategy) 
                ? strategy 
                : new DiveSpotValidationStrategy(); // Default fallback
        }

        public IValidationStrategy GetStrategy(ModeratorSpecialization specialization)
        {
            return _specializationStrategies.TryGetValue(specialization, out var strategy) 
                ? strategy 
                : new DiveSpotValidationStrategy(); // Default fallback
        }

        public List<IValidationStrategy> GetAllStrategies()
        {
            return _specializationStrategies.Values.OrderByDescending(s => s.Priority).ToList();
        }

        private ActivityCategory GetCategoryForSpecialization(ModeratorSpecialization specialization)
        {
            return specialization switch
            {
                ModeratorSpecialization.DiveSpots => ActivityCategory.Activity,
                ModeratorSpecialization.FreediveSpots => ActivityCategory.Activity,
                ModeratorSpecialization.SnorkelSpots => ActivityCategory.Activity,
                ModeratorSpecialization.UnderwaterPhotography => ActivityCategory.Activity,
                ModeratorSpecialization.TechnicalDiving => ActivityCategory.Activity,
                _ => ActivityCategory.Activity
            };
        }
    }
}