using SubExplore.ViewModels.Admin;

namespace SubExplore.Views.Admin;

public partial class SpotValidationPage : ContentPage
{
    public SpotValidationPage(SpotValidationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is SpotValidationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        // Handle Android back button press
        if (BindingContext is SpotValidationViewModel viewModel)
        {
            // Execute the back command asynchronously
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await viewModel.GoBackCommand.ExecuteAsync(null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SpotValidationPage] OnBackButtonPressed error: {ex.Message}");
                    // Fallback to home
                    await viewModel.GoToHomeCommand.ExecuteAsync(null);
                }
            });
            
            // Return true to indicate we handled the back button press
            return true;
        }
        
        // Let the default behavior handle it
        return base.OnBackButtonPressed();
    }
}