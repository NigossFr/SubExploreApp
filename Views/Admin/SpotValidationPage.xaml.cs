using SubExplore.ViewModels.Admin;

namespace SubExplore.Views.Admin
{
    public partial class SpotValidationPage : ContentPage
    {
        public SpotValidationViewModel ViewModel { get; }

        public SpotValidationPage(SpotValidationViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.InitializeAsync();
        }
    }
}