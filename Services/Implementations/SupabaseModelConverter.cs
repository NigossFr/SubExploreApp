// ========================================
// CONVERTISSEUR MODÈLES EF ↔ SUPABASE API
// ========================================
// Service pour convertir entre modèles Entity Framework et Supabase API

using SubExplore.Models.Domain;
using SubExplore.Models.Supabase;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Implementations
{
    public static class SupabaseModelConverter
    {
        // ========================================
        // CONVERSION USER : EF → SUPABASE
        // ========================================
        
        public static SupabaseUser ToSupabaseModel(User efUser)
        {
            return new SupabaseUser
            {
                Id = efUser.Id,
                Email = efUser.Email,
                PasswordHash = efUser.PasswordHash,
                Username = efUser.Username,
                FirstName = efUser.FirstName,
                LastName = efUser.LastName,
                AvatarUrl = efUser.AvatarUrl,
                
                // ✅ Conversion enum → string (pas de problème mapping)
                AccountType = efUser.AccountType.ToString(),
                SubscriptionStatus = efUser.SubscriptionStatus.ToString(),
                ExpertiseLevel = efUser.ExpertiseLevel?.ToString(),
                
                // Propriétés JSON/objet
                Certifications = efUser.Certifications,
                
                // Dates et autres
                CreatedAt = efUser.CreatedAt,
                UpdatedAt = efUser.UpdatedAt ?? DateTime.UtcNow,
                LastLogin = efUser.LastLogin,
                IsEmailConfirmed = efUser.IsEmailConfirmed,
                
                // Propriétés modérateur (converties en int)
                ModeratorSpecialization = (int)efUser.ModeratorSpecialization,
                ModeratorStatus = (int)efUser.ModeratorStatus,
                Permissions = (int)efUser.Permissions,
                ModeratorSince = efUser.ModeratorSince,
                OrganizationId = efUser.OrganizationId
            };
        }
        
        // ========================================
        // CONVERSION USER : SUPABASE → EF
        // ========================================
        
        public static User ToEfModel(SupabaseUser supabaseUser)
        {
            return new User
            {
                Id = supabaseUser.Id,
                Email = supabaseUser.Email,
                PasswordHash = supabaseUser.PasswordHash,
                Username = supabaseUser.Username,
                FirstName = supabaseUser.FirstName,
                LastName = supabaseUser.LastName,
                AvatarUrl = supabaseUser.AvatarUrl,
                
                // ✅ Conversion string → enum avec sécurité
                AccountType = ParseEnum<AccountType>(supabaseUser.AccountType, AccountType.Standard),
                SubscriptionStatus = ParseEnum<SubscriptionStatus>(supabaseUser.SubscriptionStatus, SubscriptionStatus.Free),
                ExpertiseLevel = ParseEnumNullable<ExpertiseLevel>(supabaseUser.ExpertiseLevel),
                
                // Propriétés JSON/objet
                Certifications = supabaseUser.Certifications as Dictionary<string, object>,
                
                // Dates et autres
                CreatedAt = supabaseUser.CreatedAt,
                UpdatedAt = supabaseUser.UpdatedAt,
                LastLogin = supabaseUser.LastLogin,
                IsEmailConfirmed = supabaseUser.IsEmailConfirmed,
                
                // Propriétés modérateur (conversion sécurisée)
                ModeratorSpecialization = 0, // TODO: Mapper correctement si nécessaire
                ModeratorStatus = 0, // TODO: Mapper correctement si nécessaire  
                Permissions = 0, // TODO: Mapper correctement si nécessaire
                ModeratorSince = supabaseUser.ModeratorSince,
                OrganizationId = supabaseUser.OrganizationId
            };
        }
        
        // ========================================
        // CONVERSION SPOT TYPE : EF → SUPABASE
        // ========================================
        
        public static SupabaseSpotType ToSupabaseModel(SpotType efSpotType)
        {
            return new SupabaseSpotType
            {
                Id = efSpotType.Id,
                Name = efSpotType.Name,
                IconPath = efSpotType.IconPath,
                ColorCode = efSpotType.ColorCode,
                RequiresExpertValidation = efSpotType.RequiresExpertValidation,
                ValidationCriteria = efSpotType.ValidationCriteria,
                
                // ✅ Conversion enum → string
                Category = efSpotType.Category.ToString(),
                
                Description = efSpotType.Description,
                IsActive = efSpotType.IsActive,
                CreatedAt = efSpotType.CreatedAt,
                UpdatedAt = efSpotType.UpdatedAt ?? DateTime.UtcNow
            };
        }
        
        // ========================================
        // CONVERSION SPOT TYPE : SUPABASE → EF
        // ========================================
        
        public static SpotType ToEfModel(SupabaseSpotType supabaseSpotType)
        {
            return new SpotType
            {
                Id = supabaseSpotType.Id,
                Name = supabaseSpotType.Name,
                IconPath = supabaseSpotType.IconPath,
                ColorCode = supabaseSpotType.ColorCode,
                RequiresExpertValidation = supabaseSpotType.RequiresExpertValidation,
                ValidationCriteria = supabaseSpotType.ValidationCriteria as Dictionary<string, object>,
                
                // ✅ Conversion string → enum avec sécurité
                Category = ParseEnum<ActivityCategory>(supabaseSpotType.Category, ActivityCategory.Activity),
                
                Description = supabaseSpotType.Description,
                IsActive = supabaseSpotType.IsActive,
                CreatedAt = supabaseSpotType.CreatedAt,
                UpdatedAt = supabaseSpotType.UpdatedAt
            };
        }
        
        // ========================================
        // MÉTHODES UTILITAIRES DE CONVERSION
        // ========================================
        
        /// <summary>
        /// Parse un enum depuis une string avec valeur par défaut sécurisée
        /// </summary>
        private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
                return result;
                
            return defaultValue;
        }
        
        /// <summary>
        /// Parse un enum nullable depuis une string
        /// </summary>
        private static TEnum? ParseEnumNullable<TEnum>(string? value) where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
                return null;
                
            if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
                return result;
                
            return null;
        }
        
        // ========================================
        // MÉTHODES DE CONVERSION EN LOTS
        // ========================================
        
        /// <summary>
        /// Convertit une liste d'utilisateurs EF vers Supabase
        /// </summary>
        public static List<SupabaseUser> ToSupabaseModels(IEnumerable<User> efUsers)
        {
            return efUsers.Select(ToSupabaseModel).ToList();
        }
        
        /// <summary>
        /// Convertit une liste d'utilisateurs Supabase vers EF
        /// </summary>
        public static List<User> ToEfModels(IEnumerable<SupabaseUser> supabaseUsers)
        {
            return supabaseUsers.Select(ToEfModel).ToList();
        }
        
        /// <summary>
        /// Convertit une liste de spot types EF vers Supabase
        /// </summary>
        public static List<SupabaseSpotType> ToSupabaseModels(IEnumerable<SpotType> efSpotTypes)
        {
            return efSpotTypes.Select(ToSupabaseModel).ToList();
        }
        
        /// <summary>
        /// Convertit une liste de spot types Supabase vers EF
        /// </summary>
        public static List<SpotType> ToEfModels(IEnumerable<SupabaseSpotType> supabaseSpotTypes)
        {
            return supabaseSpotTypes.Select(ToEfModel).ToList();
        }

        // ========================================
        // CONVERSIONS POUR NOUVELLE ARCHITECTURE 3-TABLES
        // ========================================

        // ========================================
        // CONVERSION PRACTICE SPOT : EF → SUPABASE
        // ========================================

        public static SupabasePracticeSpot ToSupabaseModel(PracticeSpot efPracticeSpot)
        {
            return new SupabasePracticeSpot
            {
                Id = efPracticeSpot.Id,
                Name = efPracticeSpot.Name,
                Description = efPracticeSpot.Description,
                Latitude = efPracticeSpot.Latitude,
                Longitude = efPracticeSpot.Longitude,
                CreatorId = efPracticeSpot.CreatorId,

                // Difficulty level enum to string
                DifficultyLevel = efPracticeSpot.DifficultyLevel.ToString(),

                MaxDepth = efPracticeSpot.MaxDepth,

                // Current strength enum to string
                CurrentStrength = efPracticeSpot.CurrentStrength.ToString(),

                BottomType = efPracticeSpot.BottomType,
                HasMooring = efPracticeSpot.HasMooring,
                RequiredEquipment = efPracticeSpot.RequiredEquipment,
                SafetyNotes = efPracticeSpot.SafetyNotes,
                BestConditions = efPracticeSpot.BestConditions,

                // Validation status enum to string
                ValidationStatus = efPracticeSpot.ValidationStatus.ToString(),

                LastSafetyReview = efPracticeSpot.LastSafetyReview,
                SafetyFlags = efPracticeSpot.SafetyFlags,

                CreatedAt = efPracticeSpot.CreatedAt,
                UpdatedAt = efPracticeSpot.UpdatedAt
            };
        }

        // ========================================
        // CONVERSION PRACTICE SPOT : SUPABASE → EF
        // ========================================

        public static PracticeSpot ToEfModel(SupabasePracticeSpot supabasePracticeSpot)
        {
            return new PracticeSpot
            {
                Id = supabasePracticeSpot.Id,
                Name = supabasePracticeSpot.Name,
                Description = supabasePracticeSpot.Description ?? string.Empty,
                Latitude = supabasePracticeSpot.Latitude,
                Longitude = supabasePracticeSpot.Longitude,
                CreatorId = supabasePracticeSpot.CreatorId,

                // String to enum conversions with safe parsing
                DifficultyLevel = ParseEnum<DifficultyLevel>(supabasePracticeSpot.DifficultyLevel, DifficultyLevel.Beginner),

                MaxDepth = (int?)supabasePracticeSpot.MaxDepth,

                CurrentStrength = ParseEnum<CurrentStrength>(supabasePracticeSpot.CurrentStrength, CurrentStrength.None),

                BottomType = supabasePracticeSpot.BottomType,
                HasMooring = supabasePracticeSpot.HasMooring,
                RequiredEquipment = supabasePracticeSpot.RequiredEquipment ?? string.Empty,
                SafetyNotes = supabasePracticeSpot.SafetyNotes ?? string.Empty,
                BestConditions = supabasePracticeSpot.BestConditions ?? string.Empty,

                ValidationStatus = ParseEnum<SpotValidationStatus>(supabasePracticeSpot.ValidationStatus, SpotValidationStatus.Pending),

                LastSafetyReview = supabasePracticeSpot.LastSafetyReview,
                SafetyFlags = supabasePracticeSpot.SafetyFlags?.ToString(),

                CreatedAt = supabasePracticeSpot.CreatedAt,
                UpdatedAt = supabasePracticeSpot.UpdatedAt ?? DateTime.UtcNow
            };
        }

        // ========================================
        // CONVERSION ORGANIZATION : EF → SUPABASE
        // ========================================

        public static SupabaseOrganization ToSupabaseModel(Organization efOrganization)
        {
            return new SupabaseOrganization
            {
                Id = efOrganization.Id,
                Name = efOrganization.Name,
                Description = efOrganization.Description,
                Latitude = efOrganization.Latitude,
                Longitude = efOrganization.Longitude,
                Address = efOrganization.Address,
                City = efOrganization.City,
                PostalCode = efOrganization.PostalCode,
                Country = efOrganization.Country,
                Phone = efOrganization.Phone,
                Email = efOrganization.Email,
                Website = efOrganization.Website,

                // Organization type enum to string
                OrganizationType = efOrganization.OrganizationType.ToString(),

                // JSON fields
                Services = efOrganization.ServicesOffered,
                Certifications = efOrganization.Certifications,
                OperatingHours = efOrganization.BusinessHours,

                CreatedAt = efOrganization.CreatedAt,
                UpdatedAt = efOrganization.UpdatedAt
            };
        }

        // ========================================
        // CONVERSION ORGANIZATION : SUPABASE → EF
        // ========================================

        public static Organization ToEfModel(SupabaseOrganization supabaseOrganization)
        {
            return new Organization
            {
                Id = supabaseOrganization.Id,
                Name = supabaseOrganization.Name,
                Description = supabaseOrganization.Description,
                Latitude = supabaseOrganization.Latitude,
                Longitude = supabaseOrganization.Longitude,
                Address = supabaseOrganization.Address,
                City = supabaseOrganization.City,
                PostalCode = supabaseOrganization.PostalCode,
                Country = supabaseOrganization.Country,
                Phone = supabaseOrganization.Phone,
                Email = supabaseOrganization.Email,
                Website = supabaseOrganization.Website,

                // String to enum conversion
                OrganizationType = ParseEnum<OrganizationType>(supabaseOrganization.OrganizationType, OrganizationType.ClubFFESSM),

                // JSON fields
                ServicesOffered = supabaseOrganization.Services?.ToString(),
                Certifications = supabaseOrganization.Certifications?.ToString(),
                BusinessHours = supabaseOrganization.OperatingHours?.ToString(),

                CreatedAt = supabaseOrganization.CreatedAt,
                UpdatedAt = supabaseOrganization.UpdatedAt ?? DateTime.UtcNow
            };
        }

        // ========================================
        // CONVERSION BUSINESS : EF → SUPABASE
        // ========================================

        public static SupabaseBusiness ToSupabaseModel(Business efBusiness)
        {
            return new SupabaseBusiness
            {
                Id = efBusiness.Id,
                Name = efBusiness.Name,
                Description = efBusiness.Description,
                Latitude = efBusiness.Latitude,
                Longitude = efBusiness.Longitude,
                Address = efBusiness.Address,
                City = efBusiness.City,
                PostalCode = efBusiness.PostalCode,
                Country = efBusiness.Country,
                Phone = efBusiness.Phone,
                Email = efBusiness.Email,
                Website = efBusiness.Website,

                // Business type enum to string
                BusinessType = efBusiness.BusinessType.ToString(),

                // JSON fields
                Services = efBusiness.ProductsServices,
                PriceRange = efBusiness.PriceRange.ToString(),
                PaymentMethods = efBusiness.AcceptsCreditCards ? "cards" : "cash",
                OperatingHours = efBusiness.BusinessHours,

                CreatedAt = efBusiness.CreatedAt,
                UpdatedAt = efBusiness.UpdatedAt
            };
        }

        // ========================================
        // CONVERSION BUSINESS : SUPABASE → EF
        // ========================================

        public static Business ToEfModel(SupabaseBusiness supabaseBusiness)
        {
            return new Business
            {
                Id = supabaseBusiness.Id,
                Name = supabaseBusiness.Name,
                Description = supabaseBusiness.Description,
                Latitude = supabaseBusiness.Latitude,
                Longitude = supabaseBusiness.Longitude,
                Address = supabaseBusiness.Address,
                City = supabaseBusiness.City,
                PostalCode = supabaseBusiness.PostalCode,
                Country = supabaseBusiness.Country,
                Phone = supabaseBusiness.Phone,
                Email = supabaseBusiness.Email,
                Website = supabaseBusiness.Website,

                // String to enum conversions
                BusinessType = ParseEnum<BusinessType>(supabaseBusiness.BusinessType, BusinessType.DiveShop),
                PriceRange = ParseEnum<PriceRange>(supabaseBusiness.PriceRange, PriceRange.MidRange),

                // JSON fields
                ProductsServices = supabaseBusiness.Services?.ToString(),
                AcceptsCreditCards = supabaseBusiness.PaymentMethods?.ToString()?.Contains("cards") ?? true,
                BusinessHours = supabaseBusiness.OperatingHours?.ToString(),

                CreatedAt = supabaseBusiness.CreatedAt,
                UpdatedAt = supabaseBusiness.UpdatedAt ?? DateTime.UtcNow
            };
        }

        // ========================================
        // MÉTHODES DE CONVERSION EN LOTS - NOUVELLE ARCHITECTURE
        // ========================================

        /// <summary>
        /// Convertit une liste de practice spots EF vers Supabase
        /// </summary>
        public static List<SupabasePracticeSpot> ToSupabaseModels(IEnumerable<PracticeSpot> efPracticeSpots)
        {
            return efPracticeSpots.Select(ToSupabaseModel).ToList();
        }

        /// <summary>
        /// Convertit une liste de practice spots Supabase vers EF
        /// </summary>
        public static List<PracticeSpot> ToEfModels(IEnumerable<SupabasePracticeSpot> supabasePracticeSpots)
        {
            return supabasePracticeSpots.Select(ToEfModel).ToList();
        }

        /// <summary>
        /// Convertit une liste d'organizations EF vers Supabase
        /// </summary>
        public static List<SupabaseOrganization> ToSupabaseModels(IEnumerable<Organization> efOrganizations)
        {
            return efOrganizations.Select(ToSupabaseModel).ToList();
        }

        /// <summary>
        /// Convertit une liste d'organizations Supabase vers EF
        /// </summary>
        public static List<Organization> ToEfModels(IEnumerable<SupabaseOrganization> supabaseOrganizations)
        {
            return supabaseOrganizations.Select(ToEfModel).ToList();
        }

        /// <summary>
        /// Convertit une liste de businesses EF vers Supabase
        /// </summary>
        public static List<SupabaseBusiness> ToSupabaseModels(IEnumerable<Business> efBusinesses)
        {
            return efBusinesses.Select(ToSupabaseModel).ToList();
        }

        /// <summary>
        /// Convertit une liste de businesses Supabase vers EF
        /// </summary>
        public static List<Business> ToEfModels(IEnumerable<SupabaseBusiness> supabaseBusinesses)
        {
            return supabaseBusinesses.Select(ToEfModel).ToList();
        }
    }
}