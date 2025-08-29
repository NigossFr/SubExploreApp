using SubExplore.ViewModels.Spots;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.ComponentModel;

namespace SubExplore.Views.Spots;

public partial class SpotDetailsPage : ContentPage, IQueryAttributable
{
	private readonly SpotDetailsViewModel _viewModel;
	private bool _hasInitialized = false;
	private string _spotIdFromQuery = null;

	public SpotDetailsPage(SpotDetailsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
		
		// ✅ ÉCOUTER LES CHANGEMENTS DE LOADING POUR CONFIGURER LA CARTE
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		
		Debug.WriteLine("[DEBUG] SpotDetailsPage constructor completed");
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		
		// Nettoyer les événements pour éviter les fuites mémoire
		if (_viewModel != null)
		{
			_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		
		Debug.WriteLine($"[DEBUG] SpotDetailsPage.OnAppearing - HasInitialized: {_hasInitialized}");
		
		// ✅ CORRECTION CRITIQUE: Toujours vérifier si on a un nouveau spotId
		if (_viewModel != null)
		{
			// Si c'est la première fois ou qu'on n'a pas encore initialisé
			if (!_hasInitialized)
			{
				_hasInitialized = true;
				
				// ✅ CORRECTION SÉQUENCE: Attendre que ApplyQueryAttributes soit appelé
				_ = Task.Run(async () =>
				{
					// Attendre un court délai pour permettre à ApplyQueryAttributes de s'exécuter
					await Task.Delay(100);
					
					await InitializeWithNewSpotId();
				});
			}
			else
			{
				// ✅ NOUVEAU: Page déjà initialisée, vérifier si on a un nouveau spotId
				Debug.WriteLine("[DEBUG] Page already initialized, checking for new spotId...");
				_ = Task.Run(async () =>
				{
					// Attendre un délai pour permettre à ApplyQueryAttributes de s'exécuter
					await Task.Delay(50);
					
					await InitializeWithNewSpotId();
				});
			}
		}
	}
	
	/// <summary>
	/// Méthode commune pour initialiser avec un nouveau spotId
	/// </summary>
	private async Task InitializeWithNewSpotId()
	{
		try
		{
			// Extract SpotId from query parameters with enhanced methods
			object parameter = null;
			
			// ✅ PRIORITY METHOD: Use IQueryAttributable parameter if available
			if (!string.IsNullOrEmpty(_spotIdFromQuery))
			{
				if (Guid.TryParse(_spotIdFromQuery, out var querySpotId))
				{
					parameter = querySpotId;
					System.Diagnostics.Debug.WriteLine($"[SUCCESS] Priority method: Using IQueryAttributable SpotId: {querySpotId}");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[ERROR] Invalid SpotId from IQueryAttributable: {_spotIdFromQuery}");
				}
			}
			
			// ✅ FALLBACK METHOD: Try to get parameter from URI if priority method failed
			if (parameter == null)
			{
				try
				{
					var uri = Shell.Current?.CurrentState?.Location;
					if (uri != null)
					{
						string query = null;
						
						if (!string.IsNullOrEmpty(uri.Query))
						{
							query = uri.Query.TrimStart('?');
						}
						else if (!uri.IsAbsoluteUri)
						{
							var originalString = uri.OriginalString;
							var queryIndex = originalString.IndexOf('?');
							if (queryIndex >= 0 && queryIndex < originalString.Length - 1)
							{
								query = originalString.Substring(queryIndex + 1);
							}
						}
						
						if (!string.IsNullOrEmpty(query))
						{
							var queryParams = System.Web.HttpUtility.ParseQueryString(query);
							var spotIdString = queryParams["spotId"];
							
							if (!string.IsNullOrEmpty(spotIdString) && Guid.TryParse(spotIdString, out var spotId))
							{
								parameter = spotId;
								Debug.WriteLine($"[SUCCESS] Fallback method: Parsed SpotId: {spotId}");
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[DEBUG] URI parsing failed: {ex.Message}");
				}
			}
			
			// ✅ FIX: Only navigate back if we have a clear parameter issue
			if (parameter == null)
			{
				Debug.WriteLine("[WARNING] SpotDetailsPage - No valid SpotId parameter found");
				// Let ViewModel handle the error gracefully
			}
			
			// ✅ Initialize ViewModel with parameter
			Debug.WriteLine($"[DEBUG] SpotDetailsPage - Calling InitializeAsync with parameter: {parameter}");
			await _viewModel.InitializeAsync(parameter);
			Debug.WriteLine("[DEBUG] SpotDetailsPage - ViewModel initialization completed");
			
			// ✅ Configure map after delay
			await Task.Delay(1000);
			await MainThread.InvokeOnMainThreadAsync(ConfigureMap);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ERROR] SpotDetailsPage - Initialization failed: {ex.Message}");
			Debug.WriteLine($"[ERROR] Exception: {ex}");
			
			// ✅ Reset loading state and navigate back on error
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				_viewModel.IsLoading = false;
				await Shell.Current.GoToAsync("..");
			});
		}
	}

	/// <summary>
	/// Gère les changements de propriétés du ViewModel
	/// </summary>
	private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		Debug.WriteLine($"[DEBUG] PropertyChanged: {e.PropertyName}");
		
		// Écouter les changements de IsLoading et Spot
		if (e.PropertyName == nameof(SpotDetailsViewModel.IsLoading) && !_viewModel.IsLoading)
		{
			Debug.WriteLine("[DEBUG] Loading finished, configuring map...");
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ConfigureMap();
			});
		}
		else if (e.PropertyName == nameof(SpotDetailsViewModel.Spot) && _viewModel.Spot != null)
		{
			Debug.WriteLine("[DEBUG] Spot data loaded, configuring map...");
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
		_ = ConfigureMapAsync();
	}

	/// <summary>
	/// Configuration asynchrone de la carte avec tentatives répétées
	/// </summary>
	private async Task ConfigureMapAsync()
	{
		const int MAX_RETRIES = 8;
		
		for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
		{
			try
			{
				Debug.WriteLine($"[DEBUG] ConfigureMapAsync: Attempt {attempt}/{MAX_RETRIES}");
				
				if (_viewModel?.Spot == null)
				{
					Debug.WriteLine("[DEBUG] ConfigureMapAsync: No spot data available");
					return;
				}

				if (spotMap == null)
				{
					Debug.WriteLine("[ERROR] ConfigureMapAsync: spotMap is null");
					return;
				}

				var spot = _viewModel.Spot;
				Debug.WriteLine($"[DEBUG] ConfigureMapAsync: Spot found - {spot.Name}");
				Debug.WriteLine($"[DEBUG] ConfigureMapAsync: Coordinates - Lat: {spot.Latitude}, Lon: {spot.Longitude}");
				
				// Valider les coordonnées
				if (spot.Latitude == 0 && spot.Longitude == 0)
				{
					Debug.WriteLine("[ERROR] ConfigureMapAsync: Invalid coordinates (0,0)");
					return;
				}

				var spotLocation = new Location(Convert.ToDouble(spot.Latitude), Convert.ToDouble(spot.Longitude));
				Debug.WriteLine($"[DEBUG] ConfigureMapAsync: Location created - {spotLocation.Latitude}, {spotLocation.Longitude}");

				// Attendre avec des délais progressifs et plus longs
				int delay = attempt switch
				{
					1 => 4000, // Premier essai : attendre 4 secondes
					2 => 2000, // Deuxième essai : 2 secondes
					3 => 3000, // Troisième essai : 3 secondes  
					4 => 5000, // Quatrième essai : 5 secondes
					_ => 1000 * attempt // Autres essais : progression linéaire
				};
				
				Debug.WriteLine($"[DEBUG] ConfigureMapAsync: Waiting {delay}ms for map to be ready...");
				await Task.Delay(delay);
				
				// Vérifier si le contrôle de carte est prêt en mesurant sa taille
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					Debug.WriteLine($"[DEBUG] Map dimensions check - Width: {spotMap.Width}, Height: {spotMap.Height}");
				});
				
				// Créer le pin
				var pin = new Pin
				{
					Location = spotLocation,
					Label = spot.Name ?? "Spot",
					Address = $"{spot.Type?.Name ?? "Spot de plongée"} - Profondeur: {(spot.MaxDepth?.ToString("F1") ?? "N/A")}m",
					Type = PinType.Place
				};

				// Nettoyer et ajouter le pin sur le thread principal
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					spotMap.Pins.Clear();
					spotMap.Pins.Add(pin);
					Debug.WriteLine($"[DEBUG] ConfigureMapAsync: Pin added for {spot.Name} at {spotLocation.Latitude}, {spotLocation.Longitude}");
				});
				
				// Tentative de centrage avec différentes approches sur le thread principal
				bool moveSuccess = false;
				
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					try
					{
						// Approche 1: MapSpan avec FromCenterAndRadius
						var mapSpan = MapSpan.FromCenterAndRadius(spotLocation, Distance.FromKilometers(1));
						spotMap.MoveToRegion(mapSpan);
						moveSuccess = true;
						Debug.WriteLine($"[SUCCESS] ConfigureMapAsync: Map centered on {spotLocation.Latitude}, {spotLocation.Longitude} with FromCenterAndRadius");
					}
					catch (Exception ex1)
					{
						Debug.WriteLine($"[WARNING] Attempt {attempt} - FromCenterAndRadius failed: {ex1.Message}");
						
						try
						{
							// Approche 2: MapSpan avec constructeur explicite
							var mapSpan = new MapSpan(spotLocation, 0.01, 0.01); // ~1km span
							spotMap.MoveToRegion(mapSpan);
							moveSuccess = true;
							Debug.WriteLine($"[SUCCESS] ConfigureMapAsync: Map centered using explicit MapSpan constructor");
						}
						catch (Exception ex2)
						{
							Debug.WriteLine($"[WARNING] Attempt {attempt} - Explicit MapSpan failed: {ex2.Message}");
							
							try
							{
								// Approche 3: Utilisation de VisibleRegion si disponible
								var largerSpan = new MapSpan(spotLocation, 0.05, 0.05); // ~5km span
								spotMap.MoveToRegion(largerSpan);
								moveSuccess = true;
								Debug.WriteLine($"[SUCCESS] ConfigureMapAsync: Map centered using larger span");
							}
							catch (Exception ex3)
							{
								Debug.WriteLine($"[ERROR] Attempt {attempt} - All map centering approaches failed: {ex3.Message}");
							}
						}
					}
				});
				
				if (moveSuccess)
				{
					Debug.WriteLine($"[SUCCESS] ConfigureMapAsync completed successfully on attempt {attempt}");
					
					// Petite pause puis validation finale
					await Task.Delay(500);
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						Debug.WriteLine($"[DEBUG] Final map validation - Center should be near {spotLocation.Latitude}, {spotLocation.Longitude}");
					});
					
					break; // Sortir de la boucle si succès
				}
				else if (attempt == MAX_RETRIES)
				{
					Debug.WriteLine("[ERROR] ConfigureMapAsync: All attempts failed - map may not center correctly");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] ConfigureMapAsync attempt {attempt} failed: {ex.Message}");
				
				if (attempt == MAX_RETRIES)
				{
					Debug.WriteLine($"[ERROR] ConfigureMapAsync: All {MAX_RETRIES} attempts failed");
					Debug.WriteLine($"[ERROR] Final exception: {ex}");
				}
			}
		}
	}

	/// <summary>
	/// Implémentation IQueryAttributable pour recevoir les paramètres de navigation directement
	/// </summary>
	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		
		if (query != null)
		{
			foreach (var kvp in query)
			{
			}
			
			if (query.ContainsKey("spotId"))
			{
				_spotIdFromQuery = query["spotId"]?.ToString();
			}
		}
	}
}