using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.Spots;
using System.ComponentModel;

namespace SubExplore.Views.Spots.Components;

public partial class SpotLocationView : ContentView
{
	public SpotLocationView()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}
	
	private void OnLoaded(object sender, EventArgs e)
	{
		// Initialize map with default position
		var defaultLocation = new Location(43.2965, 5.3698); // Marseille
		var mapSpan = MapSpan.FromCenterAndRadius(defaultLocation, Distance.FromKilometers(10));
		spotMap.MoveToRegion(mapSpan);
		
		System.Diagnostics.Debug.WriteLine("[DEBUG] SpotLocationView: Map initialized with default position");
		
		// Subscribe to BindingContext changes to update pin when coordinates are ready
		if (BindingContext is AddSpotViewModel viewModel)
		{
			UpdateMapPin(viewModel);
			viewModel.PropertyChanged += OnViewModelPropertyChanged;
		}
	}
	
	private AddSpotViewModel _currentViewModel;
	
	protected override void OnBindingContextChanged()
	{
		// Unsubscribe from previous ViewModel if any
		if (_currentViewModel != null)
		{
			_currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
			_currentViewModel = null;
		}
		
		base.OnBindingContextChanged();
		
		if (BindingContext is AddSpotViewModel newViewModel)
		{
			_currentViewModel = newViewModel;
			UpdateMapPin(newViewModel);
			newViewModel.PropertyChanged += OnViewModelPropertyChanged;
		}
	}
	
	private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(AddSpotViewModel.Latitude) || 
		    e.PropertyName == nameof(AddSpotViewModel.Longitude) ||
		    e.PropertyName == nameof(AddSpotViewModel.IsLocationReady))
		{
			if (sender is AddSpotViewModel viewModel)
			{
				UpdateMapPin(viewModel);
			}
		}
	}
	
	private void UpdateMapPin(AddSpotViewModel viewModel)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotLocationView: UpdateMapPin called - Lat: {viewModel.Latitude}, Lng: {viewModel.Longitude}, Ready: {viewModel.IsLocationReady}");
			
			// Clear existing pins
			spotMap.Pins.Clear();
			
			// Only add pin if location is ready and coordinates are valid
			if (viewModel.IsLocationReady && viewModel.Latitude != 0 && viewModel.Longitude != 0)
			{
				var location = new Location((double)viewModel.Latitude, (double)viewModel.Longitude);
				var pin = new Pin
				{
					Label = "Spot",
					Type = PinType.Place,
					Location = location
				};
				
				spotMap.Pins.Add(pin);
				
				// Move map to the pin location
				var mapSpan = MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1));
				spotMap.MoveToRegion(mapSpan);
				
				System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotLocationView: Pin added and map moved to {viewModel.Latitude}, {viewModel.Longitude}");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"[DEBUG] SpotLocationView: Pin not added - location not ready or invalid coordinates");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ERROR] SpotLocationView: UpdateMapPin failed: {ex.Message}");
		}
	}
}