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
	private bool _isValidationMode = false;
	private string _validationMode = null;

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
		
		// ✅ VALIDATION: Check CustomNavigationBar initialization
		ValidateCustomNavigationBarSetup();
		
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
					// Create validation parameter if validation mode is enabled
					if (_isValidationMode)
					{
						parameter = new Dictionary<string, object>
						{
							["SpotId"] = querySpotId,
							["IsValidationMode"] = _isValidationMode,
							["ValidationMode"] = _validationMode ?? "Unknown"
						};
						System.Diagnostics.Debug.WriteLine($"[SUCCESS] Priority method: Using validation parameter for SpotId: {querySpotId}, ValidationMode: {_validationMode}");
					}
					else
					{
						parameter = querySpotId;
						System.Diagnostics.Debug.WriteLine($"[SUCCESS] Priority method: Using IQueryAttributable SpotId: {querySpotId}");
					}
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
				Debug.WriteLine($"[DEBUG] Query parameter: {kvp.Key} = {kvp.Value}");
			}
			
			if (query.ContainsKey("spotId"))
			{
				_spotIdFromQuery = query["spotId"]?.ToString();
				Debug.WriteLine($"[DEBUG] Extracted SpotId: {_spotIdFromQuery}");
			}
			
			if (query.ContainsKey("isValidationMode"))
			{
				if (bool.TryParse(query["isValidationMode"]?.ToString(), out bool validationMode))
				{
					_isValidationMode = validationMode;
					Debug.WriteLine($"[DEBUG] Extracted IsValidationMode: {_isValidationMode}");
				}
			}
			
			if (query.ContainsKey("validationMode"))
			{
				_validationMode = query["validationMode"]?.ToString();
				Debug.WriteLine($"[DEBUG] Extracted ValidationMode: {_validationMode}");
			}
		}
	}

	/// <summary>
	/// Custom hamburger button clicked - UNIVERSAL SOLUTION for any navigation context
	/// </summary>
	private void OnCustomHamburgerClicked(object sender, EventArgs e)
	{
		try
		{
			Debug.WriteLine("[SpotDetailsPage] 🍔 Custom hamburger button clicked - UNIVERSAL FLYOUT ACCESS");
			Debug.WriteLine($"[SpotDetailsPage] Context: Shell.Current={(Shell.Current != null ? "✅" : "❌")}, MainPage={Application.Current?.MainPage?.GetType().Name}");
			
			bool flyoutOpened = false;

			// 🎯 METHOD 1: Shell.Current (when available)
			if (Shell.Current != null)
			{
				try
				{
					Shell.Current.FlyoutIsPresented = true;
					flyoutOpened = true;
					Debug.WriteLine("[SpotDetailsPage] ✅ Method 1: Shell.Current flyout opened successfully");
				}
				catch (Exception shellEx)
				{
					Debug.WriteLine($"[SpotDetailsPage] ⚠️ Method 1 failed: {shellEx.Message}");
				}
			}

			// 🎯 METHOD 2: Application.Current.MainPage as Shell (fallback #1)
			if (!flyoutOpened && Application.Current?.MainPage is Shell appShell)
			{
				try
				{
					appShell.FlyoutIsPresented = true;
					flyoutOpened = true;
					Debug.WriteLine("[SpotDetailsPage] ✅ Method 2: Application.Current.MainPage flyout opened successfully");
				}
				catch (Exception appEx)
				{
					Debug.WriteLine($"[SpotDetailsPage] ⚠️ Method 2 failed: {appEx.Message}");
				}
			}

			// 🎯 METHOD 3: Navigate back to Shell context (fallback #2)
			if (!flyoutOpened && Application.Current?.MainPage is NavigationPage navPage)
			{
				try
				{
					Debug.WriteLine("[SpotDetailsPage] 🔄 Detected NavigationPage context - navigating back to Shell");
					
					// Navigate back to map with flyout open
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						try
						{
							await Shell.Current.GoToAsync("///map");
							// Small delay to ensure navigation completes
							await Task.Delay(300);
							if (Shell.Current != null)
							{
								Shell.Current.FlyoutIsPresented = true;
								Debug.WriteLine("[SpotDetailsPage] ✅ Method 3: Navigated to Shell and opened flyout");
							}
						}
						catch (Exception navEx)
						{
							Debug.WriteLine($"[SpotDetailsPage] ❌ Method 3 navigation failed: {navEx.Message}");
						}
					});
					flyoutOpened = true; // Assume success for async operation
				}
				catch (Exception navEx)
				{
					Debug.WriteLine($"[SpotDetailsPage] ⚠️ Method 3 failed: {navEx.Message}");
				}
			}

			// 🎯 METHOD 4: Force Shell creation/navigation (emergency fallback)
			if (!flyoutOpened)
			{
				try
				{
					Debug.WriteLine("[SpotDetailsPage] 🚨 Emergency: Creating new Shell navigation context");
					
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						try
						{
							// Force navigate back to a known Shell route
							if (Application.Current?.MainPage != null)
							{
								var shell = new AppShell();
								Application.Current.MainPage = shell;
								await Task.Delay(500); // Allow Shell initialization
								shell.FlyoutIsPresented = true;
								Debug.WriteLine("[SpotDetailsPage] ✅ Method 4: Emergency Shell creation succeeded");
							}
						}
						catch (Exception emergencyEx)
						{
							Debug.WriteLine($"[SpotDetailsPage] ❌ Method 4 emergency fallback failed: {emergencyEx.Message}");
							
							// Final fallback: Show user message
							await Application.Current?.MainPage?.DisplayAlert(
								"Menu indisponible", 
								"Le menu est temporairement indisponible. Veuillez redémarrer l'application.", 
								"OK");
						}
					});
				}
				catch (Exception emergencyEx)
				{
					Debug.WriteLine($"[SpotDetailsPage] ❌ Emergency method setup failed: {emergencyEx.Message}");
				}
			}

			// 📊 Log final status
			if (flyoutOpened)
			{
				Debug.WriteLine("[SpotDetailsPage] 🎉 SUCCESS: Flyout menu access achieved!");
			}
			else
			{
				Debug.WriteLine("[SpotDetailsPage] ❌ FAILED: All flyout access methods failed");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SpotDetailsPage] ❌ CRITICAL ERROR in OnCustomHamburgerClicked: {ex.Message}");
			Debug.WriteLine($"[SpotDetailsPage] Stack trace: {ex.StackTrace}");
		}
	}

	/// <summary>
	/// Validate CustomNavigationBar setup and event binding
	/// </summary>
	private void ValidateCustomNavigationBarSetup()
	{
		try
		{
			Debug.WriteLine("[SpotDetailsPage] Validating CustomNavigationBar setup...");
			
			if (CustomNavBar == null)
			{
				Debug.WriteLine("[SpotDetailsPage] ❌ CustomNavBar is NULL - this should not happen!");
				return;
			}
			
			Debug.WriteLine($"[SpotDetailsPage] ✅ CustomNavBar initialized: {CustomNavBar.GetType().Name}");
			Debug.WriteLine($"[SpotDetailsPage] CustomNavBar Title: '{CustomNavBar.Title}'");
			Debug.WriteLine($"[SpotDetailsPage] CustomNavBar IsVisible: {CustomNavBar.IsVisible}");
			Debug.WriteLine($"[SpotDetailsPage] CustomNavBar IsEnabled: {CustomNavBar.IsEnabled}");
			Debug.WriteLine($"[SpotDetailsPage] CustomNavBar Parent: {CustomNavBar.Parent?.GetType().Name ?? "NULL"}");
			
			// Check if the HamburgerClicked event has subscribers
			var hamburgerClickedField = typeof(Views.Controls.CustomNavigationBar)
				.GetField("HamburgerClicked", 
					System.Reflection.BindingFlags.Instance | 
					System.Reflection.BindingFlags.Public);
					
			if (hamburgerClickedField != null)
			{
				var eventValue = hamburgerClickedField.GetValue(CustomNavBar) as EventHandler;
				Debug.WriteLine($"[SpotDetailsPage] HamburgerClicked event subscribers: {(eventValue?.GetInvocationList()?.Length ?? 0)}");
			}
			
			// Validate Shell context
			Debug.WriteLine($"[SpotDetailsPage] Shell.Current available: {Shell.Current != null}");
			if (Shell.Current != null)
			{
				Debug.WriteLine($"[SpotDetailsPage] Shell.Current.FlyoutBehavior: {Shell.Current.FlyoutBehavior}");
				Debug.WriteLine($"[SpotDetailsPage] Shell.Current.FlyoutIsPresented: {Shell.Current.FlyoutIsPresented}");
			}
			
			Debug.WriteLine("[SpotDetailsPage] CustomNavigationBar validation complete");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SpotDetailsPage] ❌ CustomNavigationBar validation failed: {ex.Message}");
		}
	}
}