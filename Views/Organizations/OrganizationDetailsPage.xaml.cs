using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Organizations;

namespace SubExplore.Views.Organizations;

public partial class OrganizationDetailsPage : ContentPage
{
    public OrganizationDetailsPage(OrganizationDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Subscribe to property changes to update map
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrganizationDetailsViewModel.Organization) && sender is OrganizationDetailsViewModel vm)
        {
            UpdateMapLocation(vm);
        }
    }

    private void UpdateMapLocation(OrganizationDetailsViewModel viewModel)
    {
        if (viewModel.Organization == null) return;

        try
        {
            var organization = viewModel.Organization;
            var location = new Location((double)organization.Latitude, (double)organization.Longitude);
            
            // Clear existing pins
            organizationMap.Pins.Clear();
            
            // Add pin for organization
            var pin = new Pin
            {
                Label = organization.Name ?? "Organisation",
                Address = organization.Address ?? "",
                Type = PinType.Place,
                Location = location
            };
            
            organizationMap.Pins.Add(pin);
            
            // Center map on organization location
            var mapSpan = new MapSpan(location, 0.01, 0.01); // Zoom level
            organizationMap.MoveToRegion(mapSpan);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating organization map: {ex.Message}");
        }
    }

    private void OnCustomHamburgerClicked(object sender, EventArgs e)
    {
        // Handle custom hamburger menu if needed
        // For now, we can navigate back
        if (BindingContext is OrganizationDetailsViewModel vm)
        {
            vm.BackCommand.Execute(null);
        }
    }
}