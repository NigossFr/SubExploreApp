using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;

namespace SubExplore.ViewModels.Spot
{
    public partial class AddSpotViewModel : ViewModelBase
    {
        public AddSpotViewModel(
            IDialogService dialogService = null,
            INavigationService navigationService = null)
            : base(dialogService, navigationService)
        {
            Title = "Nouveau spot";
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            await Task.CompletedTask;
        }
    }
}
