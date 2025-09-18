// ========================================
// SPOT TYPE ITEM - Modèle pour sélection des types
// ========================================
// Classe simple pour remplacer celle supprimée avec AddSpotViewModel

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using SubExplore.Models.Domain;
using Microsoft.Extensions.Logging;

namespace SubExplore.Models.ViewModels
{
    /// <summary>
    /// Item pour la sélection des types de spot
    /// Version simplifiée pour compatibilité avec SimpleApiAddSpotViewModel
    /// </summary>
    public partial class SpotTypeItem : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        partial void OnIsSelectedChanged(bool value)
        {
            // Force visible diagnostic that won't be dropped
            System.Diagnostics.Debug.WriteLine($"🚨 FORCE DEBUG: SpotTypeItem.OnIsSelectedChanged called for {Name}, IsSelected: {value}");
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(TextColor));
            System.Diagnostics.Debug.WriteLine($"🚨 FORCE DEBUG: SpotTypeItem BackgroundColor notification sent for {Name}");
        }

        [ObservableProperty]
        private SpotType _spotType = new();

        partial void OnSpotTypeChanged(SpotType value)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
        }

        public string Name => SpotType?.Name ?? "Unknown";
        public string Icon => "📍"; // Icône par défaut temporaire
        public string Description => SpotType?.Description ?? "";

        public Color BackgroundColor => IsSelected ? Colors.Blue : Colors.LightGray;
        public Color TextColor => IsSelected ? Colors.White : Colors.Black;

        public SpotTypeItem()
        {
            System.Diagnostics.Debug.WriteLine("🚨 FORCE DEBUG: SpotTypeItem() default constructor called");
        }

        public SpotTypeItem(SpotType spotType)
        {
            System.Diagnostics.Debug.WriteLine($"🚨 FORCE DEBUG: SpotTypeItem(SpotType) constructor called for {spotType?.Name ?? "NULL"}");
            SpotType = spotType;
            System.Diagnostics.Debug.WriteLine($"🚨 FORCE DEBUG: SpotTypeItem constructed successfully - Name: {Name}, Description: {Description}");
        }
    }
}