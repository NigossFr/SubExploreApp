// ========================================
// INTERFACE SERVICE DE RÉPARATION SUPABASE
// ========================================

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Interface pour le service de réparation automatique des types de spots Supabase
    /// </summary>
    public interface ISupabaseSpotTypeRepairService
    {
        /// <summary>
        /// Détecte si la base de données Supabase a des types de spots corrompus
        /// </summary>
        /// <returns>True si corruption détectée, False sinon</returns>
        Task<bool> IsSupabaseDatabaseCorruptedAsync();

        /// <summary>
        /// Répare automatiquement la base de données Supabase
        /// </summary>
        /// <returns>True si réparation réussie, False sinon</returns>
        Task<bool> RepairSupabaseDatabaseAsync();
    }
}