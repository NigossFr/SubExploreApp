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
	// ✅ REMOVED: SpotId handling moved to ViewModel QueryProperty system
	// ✅ REMOVED: Validation mode handling moved to ViewModel

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
		
		// ✅ HAMBURGER FIX: Ensure event binding works
		EnsureHamburgerEventBinding();
		
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
			
			// ✅ PRIORITY METHOD: Let ViewModel handle QueryProperty parameters
			// Since we removed IQueryAttributable from Page, ViewModel will receive Shell parameters directly
			// Check if ViewModel already has SpotId from QueryProperty
			if (!string.IsNullOrEmpty(_viewModel.SpotId) && Guid.TryParse(_viewModel.SpotId, out var viewModelSpotId))
			{
				parameter = viewModelSpotId;
				System.Diagnostics.Debug.WriteLine($"[SUCCESS] Priority method: Using ViewModel QueryProperty SpotId: {viewModelSpotId}");
			}
			else if (!string.IsNullOrEmpty(_viewModel.SpotIdParam) && Guid.TryParse(_viewModel.SpotIdParam, out var viewModelSpotIdParam))
			{
				parameter = viewModelSpotIdParam;
				System.Diagnostics.Debug.WriteLine($"[SUCCESS] Priority method: Using ViewModel QueryProperty SpotIdParam: {viewModelSpotIdParam}");
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
			Debug.WriteLine($"[DEBUG] SpotDetailsPage - ViewModel type: {_viewModel.GetType().Name}");
			Debug.WriteLine($"[DEBUG] SpotDetailsPage - About to call InitializeAsync...");
			
			try
			{
				await _viewModel.InitializeAsync(parameter);
				Debug.WriteLine("[DEBUG] SpotDetailsPage - ViewModel initialization completed successfully");
			}
			catch (Exception vmEx)
			{
				Debug.WriteLine($"[ERROR] SpotDetailsPage - ViewModel InitializeAsync failed: {vmEx.Message}");
				Debug.WriteLine($"[ERROR] SpotDetailsPage - ViewModel Exception: {vmEx}");
				throw; // Re-throw to be caught by outer catch
			}
			
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
	/// ✅ REMOVED: IQueryAttributable implementation moved to ViewModel only
	/// This prevents Page from intercepting Shell navigation parameters that should go to ViewModel
	/// </summary>

	/// <summary>
	/// Custom hamburger button clicked - guaranteed flyout access with multiple fallback methods
	/// </summary>
	private async void OnCustomHamburgerClicked(object sender, EventArgs e)
	{
		try
		{
			Debug.WriteLine("[SpotDetailsPage] Custom hamburger button clicked - bypassing MAUI Shell bugs");
			
			bool flyoutOpened = false;
			
			// Method 1: Direct Shell.Current access
			if (Shell.Current != null)
			{
				Shell.Current.FlyoutIsPresented = true;
				flyoutOpened = true;
				Debug.WriteLine("[SpotDetailsPage] ✅ Flyout opened via Shell.Current");
			}
			else
			{
				Debug.WriteLine("[SpotDetailsPage] ❌ Shell.Current is null - trying alternative methods");
				
				// Method 2: Access Shell via Application.Current.MainPage
				if (Application.Current?.MainPage is Shell appShell)
				{
					appShell.FlyoutIsPresented = true;
					flyoutOpened = true;
					Debug.WriteLine("[SpotDetailsPage] ✅ Flyout opened via Application.Current.MainPage as Shell");
				}
				// Method 3: Try to find Shell in the visual tree
				else if (Application.Current?.MainPage != null)
				{
					var shell = FindShellInVisualTree(Application.Current.MainPage);
					if (shell != null)
					{
						shell.FlyoutIsPresented = true;
						flyoutOpened = true;
						Debug.WriteLine("[SpotDetailsPage] ✅ Flyout opened via Shell found in visual tree");
					}
					else
					{
						Debug.WriteLine("[SpotDetailsPage] ❌ No Shell found in visual tree");
					}
				}
				
				// Method 4: Try to access Shell via Parent hierarchy
				if (!flyoutOpened)
				{
					var shell = FindShellFromParent(this);
					if (shell != null)
					{
						shell.FlyoutIsPresented = true;
						flyoutOpened = true;
						Debug.WriteLine("[SpotDetailsPage] ✅ Flyout opened via Shell found in parent hierarchy");
					}
					else
					{
						Debug.WriteLine("[SpotDetailsPage] ❌ No Shell found in parent hierarchy");
					}
				}
				
				// Method 5: Direct messaging to main application
				if (!flyoutOpened)
				{
					Debug.WriteLine("[SpotDetailsPage] 🔄 All Shell access methods failed - trying direct messaging");
					try
					{
						// Use MessagingCenter to send flyout request to main application
						MessagingCenter.Send<object>(this, "OpenFlyoutMenu");
						Debug.WriteLine("[SpotDetailsPage] ✅ Flyout request sent via MessagingCenter");
						
						// Give MessagingCenter time to process the request
						await Task.Delay(100);
						
						flyoutOpened = true; // Assume it will work
					}
					catch (Exception msgEx)
					{
						Debug.WriteLine($"[SpotDetailsPage] ❌ MessagingCenter failed: {msgEx.Message}");
						
						// Final fallback: Navigate to main page
						try
						{
							Debug.WriteLine("[SpotDetailsPage] 🔄 Final fallback: Navigate to main page");
							Device.BeginInvokeOnMainThread(async () =>
							{
								try
								{
									// Navigate to a page that has Shell access
									await Shell.Current?.GoToAsync("///map");
									// Small delay to let navigation complete
									await Task.Delay(200);
									// Try to open flyout from there
									if (Shell.Current != null)
									{
										Shell.Current.FlyoutIsPresented = true;
										Debug.WriteLine("[SpotDetailsPage] ✅ Flyout opened after navigation to main page");
									}
								}
								catch (Exception navEx)
								{
									Debug.WriteLine($"[SpotDetailsPage] ❌ Final navigation fallback failed: {navEx.Message}");
								}
							});
						}
						catch (Exception finalEx)
						{
							Debug.WriteLine($"[SpotDetailsPage] ❌ Final fallback setup failed: {finalEx.Message}");
						}
					}
				}
			}
			
			if (!flyoutOpened)
			{
				Debug.WriteLine("[SpotDetailsPage] ⚠️ All flyout access methods failed");
			}
			
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SpotDetailsPage] ❌ Custom hamburger error: {ex.Message}");
		}
	}

	/// <summary>
	/// Find Shell in visual tree by traversing all elements
	/// </summary>
	private Shell FindShellInVisualTree(Element element)
	{
		try
		{
			if (element is Shell shell)
				return shell;

			if (element is IVisualTreeElement visualElement)
			{
				foreach (var child in visualElement.GetVisualChildren())
				{
					if (child is Element childElement)
					{
						var foundShell = FindShellInVisualTree(childElement);
						if (foundShell != null)
							return foundShell;
					}
				}
			}

			// Also try LogicalChildren for older MAUI versions
			if (element.LogicalChildren != null)
			{
				foreach (var child in element.LogicalChildren)
				{
					if (child is Element childElement)
					{
						var foundShell = FindShellInVisualTree(childElement);
						if (foundShell != null)
							return foundShell;
					}
				}
			}

			return null;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SpotDetailsPage] Error finding Shell in visual tree: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Find Shell by traversing up the Parent hierarchy
	/// </summary>
	private Shell FindShellFromParent(Element element)
	{
		try
		{
			var current = element;
			while (current != null)
			{
				if (current is Shell shell)
					return shell;
				current = current.Parent;
			}
			return null;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SpotDetailsPage] Error finding Shell from parent: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Ensure hamburger button event binding works - FIX FOR NON-WORKING HAMBURGER
	/// </summary>
	private void EnsureHamburgerEventBinding()
	{
		try
		{
			Debug.WriteLine("[SpotDetailsPage] Ensuring hamburger event binding...");
			
			if (CustomNavBar == null)
			{
				Debug.WriteLine("[SpotDetailsPage] ❌ CustomNavBar is null - cannot bind event");
				return;
			}
			
			// Remove any existing subscription to prevent duplicates
			CustomNavBar.HamburgerClicked -= OnCustomHamburgerClicked;
			// Add the subscription
			CustomNavBar.HamburgerClicked += OnCustomHamburgerClicked;
			Debug.WriteLine("[SpotDetailsPage] ✅ Manual hamburger event binding applied");
			
			// Also ensure direct button access as fallback
			var hamburgerButton = CustomNavBar.FindByName<Button>("HamburgerButton");
			if (hamburgerButton != null)
			{
				hamburgerButton.Clicked -= OnHamburgerButtonDirectClick; // Remove existing
				hamburgerButton.Clicked += OnHamburgerButtonDirectClick; // Add direct handler
				Debug.WriteLine("[SpotDetailsPage] ✅ Direct hamburger button event binding applied as fallback");
				
				// Verify button state
				Debug.WriteLine($"[SpotDetailsPage] HamburgerButton.IsVisible: {hamburgerButton.IsVisible}");
				Debug.WriteLine($"[SpotDetailsPage] HamburgerButton.IsEnabled: {hamburgerButton.IsEnabled}");
				Debug.WriteLine($"[SpotDetailsPage] HamburgerButton.Text: '{hamburgerButton.Text}'");
			}
			else
			{
				Debug.WriteLine("[SpotDetailsPage] ❌ HamburgerButton not found in CustomNavBar");
			}
			
			// Ensure CustomNavigationBar is on top of all content
			CustomNavBar.ZIndex = 1000; // Very high Z-Index
			Debug.WriteLine("[SpotDetailsPage] CustomNavBar ZIndex set to 1000");
			
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SpotDetailsPage] ❌ Hamburger event binding failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Direct hamburger button click handler - fallback method
	/// </summary>
	private void OnHamburgerButtonDirectClick(object sender, EventArgs e)
	{
		Debug.WriteLine("[SpotDetailsPage] Direct hamburger button clicked - fallback method");
		// Delegate to main handler
		OnCustomHamburgerClicked(sender, e);
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