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
using SubExplore.Services.Validation;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spots
{
    public partial class SpotCharacteristicsViewModel : ViewModelBase
    {
        private readonly ISpotTypeRepository _spotTypeRepository;
        private readonly IValidationService _validationService;

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

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private bool _hasValidationErrors;

        [ObservableProperty]
        private string _validationSummary = string.Empty;

        // Alias pour la compatibilité XAML
        public ObservableCollection<SpotType> AvailableSpotTypes => SpotTypes;

        public SpotCharacteristicsViewModel(
            ISpotTypeRepository spotTypeRepository,
            IValidationService validationService,
            IDialogService dialogService)
            : base(dialogService)
        {
            _spotTypeRepository = spotTypeRepository;
            _validationService = validationService;

            SpotTypes = new ObservableCollection<SpotType>();
            CurrentStrengths = new ObservableCollection<CurrentStrength>
            {
                CurrentStrength.None,
                CurrentStrength.Weak,
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

            SelectedCurrentStrength = CurrentStrength.Weak;
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
                SelectedCurrentStrength = spot.CurrentStrength ?? CurrentStrength.Weak;
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
                await DialogService.ShowAlertAsync("Erreur", "Impossible de charger les types de spots.", "D'accord");
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
        private async Task ValidateCharacteristics()
        {
            var validationResult = _validationService.ValidateSpotCharacteristics(
                SpotName, 
                Description, 
                MaxDepth > 0 ? MaxDepth : null, 
                SelectedDifficultyLevel.ToString(),
                SelectedSpotType?.Id.ToString());
            
            HasValidationErrors = !validationResult.IsValid;
            
            if (!validationResult.IsValid)
            {
                ValidationSummary = validationResult.GetErrorsText();
                await DialogService.ShowAlertAsync("Erreurs de validation", ValidationSummary, "D'accord");
                return;
            }
            
            if (validationResult.HasWarnings)
            {
                var shouldContinue = await DialogService.ShowConfirmationAsync(
                    "Avertissements", 
                    $"Des avertissements ont été détectés:\n{validationResult.GetWarningsText()}\n\nVoulez-vous continuer ?", 
                    "Continuer", 
                    "Corriger");
                    
                if (!shouldContinue)
                {
                    ValidationSummary = validationResult.GetWarningsText();
                    return;
                }
            }
            
            HasValidationErrors = false;
            ValidationSummary = string.Empty;
            await DialogService.ShowToastAsync("Caractéristiques validées avec succès");
        }
        
        // Auto-validate when key properties change
        partial void OnSpotNameChanged(string value)
        {
            ValidateRealTime();
        }
        
        partial void OnDescriptionChanged(string value)
        {
            ValidateRealTime();
        }
        
        partial void OnMaxDepthChanged(int value)
        {
            ValidateRealTime();
        }
        
        private void ValidateRealTime()
        {
            if (string.IsNullOrEmpty(SpotName) || string.IsNullOrEmpty(Description))
            {
                HasValidationErrors = true;
                return;
            }
            
            // Quick validation for immediate feedback
            var hasBasicErrors = string.IsNullOrWhiteSpace(SpotName) || 
                               SpotName.Length < 3 || 
                               string.IsNullOrWhiteSpace(Description) || 
                               Description.Length < 20;
            
            HasValidationErrors = hasBasicErrors;
        }
    }
}
