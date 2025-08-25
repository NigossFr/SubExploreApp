using SubExplore.ViewModels.Spots;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.ComponentModel;

namespace SubExplore.Views.Spots;

public partial class SpotDetailsPage : ContentPage
{
	private readonly SpotDetailsViewModel _viewModel;
	private bool _hasInitialized = false;

	public SpotDetailsPage(SpotDetailsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
		
		// ✅ ÉCOUTER LES CHANGEMENTS DE LOADING POUR CONFIGURER LA CARTE
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		
		Debug.WriteLine("[DEBUG] SpotDetailsPage constructor completed");
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		
		Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing - HasInitialized: {_hasInitialized}");
		
		// Only initialize once per page instance
		if (!_hasInitialized && _viewModel != null)
		{
			_hasInitialized = true;
			
			// ✅ FIX: Use Task.Run to avoid async void
			_ = Task.Run(async () =>
			{
				try
				{
					// Extract SpotId from query parameters
					object parameter = null;
					
					try
					{
						var uri = Shell.Current.CurrentState.Location;
						Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing - Current URI: {uri}");
						
						// ✅ FIX: Handle relative URI properly
						if (uri != null && !string.IsNullOrEmpty(uri.Query))
						{
							var query = uri.Query.TrimStart('?');
							var queryParams = System.Web.HttpUtility.ParseQueryString(query);
							var spotIdString = queryParams["spotId"];
							
							Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing - SpotId from query: {spotIdString}");
							
							if (!string.IsNullOrEmpty(spotIdString) && Guid.TryParse(spotIdString, out var spotId))
							{
								parameter = spotId;
								Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing - Parsed SpotId: {spotId}");
							}
							else
							{
								Debug.WriteLine($"[ERROR] SpotDetailsPage.OnAppearing - Invalid SpotId format: {spotIdString}");
							}
						}
						else
						{
							Debug.WriteLine("[WARNING] SpotDetailsPage.OnAppearing - No query parameters found");
						}
					}
					catch (Exception uriEx)
					{
						Debug.WriteLine($"[ERROR] SpotDetailsPage.OnAppearing - URI parsing error: {uriEx.Message}");
						
						// ✅ FIX: Try alternative approach using navigation parameters
						// For now, we'll continue without parameters and let ViewModel handle the error
						Debug.WriteLine("[INFO] SpotDetailsPage.OnAppearing - Continuing without URI parameters");
					}
					
					// ✅ FIX: Only navigate back if we have a clear parameter issue, not URI parsing issues
					if (parameter == null)
					{
						Debug.WriteLine("[WARNING] SpotDetailsPage.OnAppearing - No valid SpotId parameter found");
						// Let ViewModel handle the error gracefully instead of immediately going back
					}
					
					// Initialize ViewModel with parameter
					Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing - Calling InitializeAsync with parameter: {parameter}");
					await _viewModel.InitializeAsync(parameter);
					Debug.WriteLine("[DEBUG] SpotDetailsPage.OnAppearing - ViewModel initialization completed");
					
					// ✅ La carte sera configurée automatiquement via l'événement PropertyChanged
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[ERROR] SpotDetailsPage.OnAppearing - Initialization failed: {ex.Message}");
					Debug.WriteLine($"[ERROR] Exception: {ex}");
					
					// ✅ FIX: Ensure loading state is reset and navigate back on error
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						_viewModel.IsLoading = false;
						await Shell.Current.GoToAsync("..");
					});
				}
			});
		}
	}

	/// <summary>
	/// Gère les changements de propriétés du ViewModel
	/// </summary>
	private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(SpotDetailsViewModel.IsLoading) && !_viewModel.IsLoading)
		{
			// Le chargement est terminé, configurer la carte
			Debug.WriteLine("[DEBUG] Loading finished, configuring map...");
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ConfigureMap();
			});
		}
	}

	/// <summary>
	/// Configure la carte avec la position du spot
	/// </summary>
	private void ConfigureMap()
	{
		try
		{
			if (_viewModel?.Spot == null)
			{
				Debug.WriteLine("[DEBUG] ConfigureMap: No spot data available");
				return;
			}

			var spot = _viewModel.Spot;
			var spotLocation = new Location(Convert.ToDouble(spot.Latitude), Convert.ToDouble(spot.Longitude));
			
			Debug.WriteLine($"[DEBUG] ConfigureMap: Setting map location to {spot.Latitude}, {spot.Longitude}");

			// Centrer la carte sur le spot
			var mapSpan = MapSpan.FromCenterAndRadius(spotLocation, Distance.FromKilometers(2));
			spotMap.MoveToRegion(mapSpan);

			// Ajouter un pin pour le spot
			var pin = new Pin
			{
				Location = spotLocation,
				Label = spot.Name,
				Address = $"{spot.Type?.Name ?? "Spot"} - {spot.MaxDepth}m",
				Type = PinType.Place
			};

			spotMap.Pins.Clear();
			spotMap.Pins.Add(pin);
			
			Debug.WriteLine($"[DEBUG] ConfigureMap: Pin added for {spot.Name}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ERROR] ConfigureMap failed: {ex.Message}");
		}
	}
}