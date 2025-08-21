// ========================================
// SPOT TYPE ITEM - Modèle pour sélection des types
// ========================================
// Classe simple pour remplacer celle supprimée avec AddSpotViewModel

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using SubExplore.Models.Domain;

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
            OnPropertyChanged(nameof(BackgroundColor));
        }

        public SpotType SpotType { get; set; } = new();

        public string Name => SpotType?.Name ?? "Unknown";
        public string Icon => "📍"; // Icône par défaut temporaire
        public string Description => SpotType?.Description ?? "";

        public Color BackgroundColor => IsSelected ? Colors.Blue : Colors.LightGray;
        public Color TextColor => IsSelected ? Colors.White : Colors.Black;

        public SpotTypeItem()
        {
        }

        public SpotTypeItem(SpotType spotType)
        {
            SpotType = spotType;
        }
    }
}