using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Businesses;

namespace SubExplore.Views.Businesses;

public partial class BusinessDetailsPage : ContentPage
{
    public BusinessDetailsPage(BusinessDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Subscribe to property changes to update map
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BusinessDetailsViewModel.Business) && sender is BusinessDetailsViewModel vm)
        {
            UpdateMapLocation(vm);
        }
    }

    private void UpdateMapLocation(BusinessDetailsViewModel viewModel)
    {
        if (viewModel.Business == null) return;

        try
        {
            var business = viewModel.Business;
            var location = new Location((double)business.Latitude, (double)business.Longitude);
            
            // Clear existing pins
            businessMap.Pins.Clear();
            
            // Add pin for business
            var pin = new Pin
            {
                Label = business.Name ?? "Commerce",
                Address = business.Address ?? "",
                Type = PinType.Place,
                Location = location
            };
            
            businessMap.Pins.Add(pin);
            
            // Center map on business location
            var mapSpan = new MapSpan(location, 0.01, 0.01); // Zoom level
            businessMap.MoveToRegion(mapSpan);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating business map: {ex.Message}");
        }
    }

    private void OnCustomHamburgerClicked(object sender, EventArgs e)
    {
        // Handle custom hamburger menu if needed
        // For now, we can navigate back
        if (BindingContext is BusinessDetailsViewModel vm)
        {
            vm.BackCommand.Execute(null);
        }
    }
}