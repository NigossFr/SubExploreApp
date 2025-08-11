using System;
using System.Collections.Generic;
using System.Linq;
using SubExplore.Models.Domain;

namespace SubExplore.Helpers.Extensions
{
    public static class SpotTypeExtensions
    {
        // Définition des catégories principales
        public static readonly Dictionary<string, List<string>> SpotCategories = new()
        {
            ["Activités"] = new List<string>
            {
                "Plongée bouteille",
                "Apnée", 
                "Randonnée sous-marine",
                "Photo sous-marine"
            },
            ["Structures"] = new List<string>
            {
                "Clubs",
                "Professionnels",
                "Bases fédérales"
            },
            ["Boutiques"] = new List<string>
            {
                "Boutiques"
            }
        };

        /// <summary>
        /// Obtient la catégorie principale d'un type de spot
        /// </summary>
        public static string GetMainCategory(this SpotType spotType)
        {
            if (spotType?.Name == null) return "Autres";

            foreach (var category in SpotCategories)
            {
                if (category.Value.Contains(spotType.Name))
                {
                    return category.Key;
                }
            }
            return "Autres";
        }

        /// <summary>
        /// Vérifie si un type de spot appartient à une catégorie donnée
        /// </summary>
        public static bool BelongsToCategory(this SpotType spotType, string categoryName)
        {
            if (spotType?.Category == null) return false;

            return categoryName switch
            {
                "Activités" => spotType.Category == Models.Enums.ActivityCategory.Activity ||
                               // Support pour l'ancienne structure (compatibilité temporaire)
                               spotType.Category == Models.Enums.ActivityCategory.Diving ||
                               spotType.Category == Models.Enums.ActivityCategory.Freediving ||
                               spotType.Category == Models.Enums.ActivityCategory.Snorkeling ||
                               spotType.Category == Models.Enums.ActivityCategory.UnderwaterPhotography,
                "Structures" => spotType.Category == Models.Enums.ActivityCategory.Structure,
                "Boutiques" => spotType.Category == Models.Enums.ActivityCategory.Shop,
                _ => spotType.Category == Models.Enums.ActivityCategory.Other
            };
        }

        /// <summary>
        /// Obtient tous les types de spots d'une catégorie donnée
        /// </summary>
        public static IEnumerable<SpotType> FilterByMainCategory(this IEnumerable<SpotType> spotTypes, string categoryName)
        {
            return spotTypes.Where(st => st.BelongsToCategory(categoryName));
        }

        /// <summary>
        /// Obtient la couleur de base d'une catégorie
        /// </summary>
        public static string GetCategoryBaseColor(string categoryName)
        {
            return categoryName switch
            {
                "Activités" => "#0077BE",    // Bleu
                "Structures" => "#228B22",   // Vert
                "Boutiques" => "#FF8C00",    // Orange
                _ => "#666666"               // Gris par défaut
            };
        }

        /// <summary>
        /// Vérifie si un type de spot est une activité sous-marine
        /// </summary>
        public static bool IsActivity(this SpotType spotType)
        {
            return spotType.BelongsToCategory("Activités");
        }

        /// <summary>
        /// Vérifie si un type de spot est une structure
        /// </summary>
        public static bool IsStructure(this SpotType spotType)
        {
            return spotType.BelongsToCategory("Structures");
        }

        /// <summary>
        /// Vérifie si un type de spot est une boutique
        /// </summary>
        public static bool IsShop(this SpotType spotType)
        {
            return spotType.BelongsToCategory("Boutiques");
        }
    }
}