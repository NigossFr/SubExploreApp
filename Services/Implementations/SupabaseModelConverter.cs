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
    }
}