using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spot
{
    public partial class SpotCharacteristicsViewModel : ViewModelBase
    {
        private readonly ISpotTypeRepository _spotTypeRepository;

        [ObservableProperty]
        private ObservableCollection<SpotType> _spotTypes;

        [ObservableProperty]
        private SpotType _selectedSpotType;

        [ObservableProperty]
        private string _spotName;

        [ObservableProperty]
        private DifficultyLevel _selectedDifficultyLevel;

        [ObservableProperty]
        private int _maxDepth;

        [ObservableProperty]
        private string _requiredEquipment;

        [ObservableProperty]
        private string _safetyNotes;

        [ObservableProperty]
        private string _bestConditions;

        [ObservableProperty]
        private ObservableCollection<CurrentStrength> _currentStrengths;

        [ObservableProperty]
        private CurrentStrength _selectedCurrentStrength;

        [ObservableProperty]
        private ObservableCollection<DifficultyLevel> _difficultyLevels;

        public SpotCharacteristicsViewModel(
            ISpotTypeRepository spotTypeRepository,
            IDialogService dialogService)
            : base(dialogService)
        {
            _spotTypeRepository = spotTypeRepository;

            SpotTypes = new ObservableCollection<SpotType>();
            CurrentStrengths = new ObservableCollection<CurrentStrength>
            {
                CurrentStrength.None,
                CurrentStrength.Light,
                CurrentStrength.Moderate,
                CurrentStrength.Strong,
                CurrentStrength.Extreme
            };

            DifficultyLevels = new ObservableCollection<DifficultyLevel>
            {
                DifficultyLevel.Beginner,
                DifficultyLevel.Intermediate,
                DifficultyLevel.Advanced,
                DifficultyLevel.Expert,
                DifficultyLevel.TechnicalOnly
            };

            SelectedCurrentStrength = CurrentStrength.Light;
            SelectedDifficultyLevel = DifficultyLevel.Beginner;
            Title = "Caractéristiques du spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await LoadSpotTypes();

            if (parameter is Models.Domain.Spot spot)
            {
                // Initialiser avec des valeurs existantes
                SpotName = spot.Name;
                SelectedSpotType = SpotTypes.FirstOrDefault(t => t.Id == spot.TypeId);
                SelectedDifficultyLevel = spot.DifficultyLevel;
                MaxDepth = spot.MaxDepth ?? 0;
                RequiredEquipment = spot.RequiredEquipment;
                SafetyNotes = spot.SafetyNotes;
                BestConditions = spot.BestConditions;
                SelectedCurrentStrength = spot.CurrentStrength ?? CurrentStrength.Light;
            }
        }

        private async Task LoadSpotTypes()
        {
            try
            {
                var types = await _spotTypeRepository.GetActiveTypesAsync();

                SpotTypes.Clear();
                foreach (var type in types)
                {
                    SpotTypes.Add(type);
                }

                if (SpotTypes.Count > 0 && SelectedSpotType == null)
                {
                    SelectedSpotType = SpotTypes[0];
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "OK");
            }
        }

        [RelayCommand]
        private void SpotTypeSelected(SpotType spotType)
        {
            SelectedSpotType = spotType;

            // Ajuster les champs requis en fonction du type
            if (spotType.Category == ActivityCategory.Snorkeling)
            {
                // Pour le snorkeling, profondeur limitée
                if (MaxDepth > 5)
                {
                    MaxDepth = 5;
                }
            }
        }

        [RelayCommand]
        private void ValidateCharacteristics()
        {
            // Cette méthode serait appelée pour valider cette étape
            // Le ViewModel parent (AddSpotViewModel) gère la transition d'étapes
        }
    }
}
