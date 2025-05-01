using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using SubExplore.Models.Domain;

namespace SubExplore.ViewModels.Spot
{
    public class SpotDetailsViewModel : ViewModelBase
    {
        private int _spotId;

        public int SpotId
        {
            get => _spotId;
            set => SetProperty(ref _spotId, value);
        }

        public SpotDetailsViewModel(
            IDialogService dialogService,
            INavigationService navigationService)
            : base(dialogService, navigationService)
        {
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            if (parameter is int spotId)
            {
                SpotId = spotId;
                // Load spot details
            }

            await Task.CompletedTask;
        }
    }
}
