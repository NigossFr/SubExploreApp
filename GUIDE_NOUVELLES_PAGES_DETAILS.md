# 📋 GUIDE - Création de Nouvelles Pages Détails MAUI Shell

## 🎯 Objectif

Guide complet pour créer de nouvelles pages de détails dans l'architecture MAUI Shell sans reproduire les bugs de timing et de QueryProperty rencontrés avec OrganizationDetails et BusinessDetails.

## ⚠️ Problèmes à Éviter

### 1. Bug Timing ApplyQueryAttributes vs OnAppearing
**Problème** : `OnAppearing()` s'exécute avant `ApplyQueryAttributes()`, causant une initialisation avec QueryProperty vide.

### 2. Bug QueryProperty avec ObservableProperty  
**Problème** : `[ObservableProperty]` incompatible avec `[QueryProperty]` de MAUI Shell.

### 3. Bug Loading State Infini
**Problème** : Absence de `finally { IsLoading = false; }` dans les méthodes de chargement.

### 4. Bug InitializeAsync Jamais Appelé
**Problème** : Oubli d'appeler `InitializeAsync()` dans le cycle de vie de la page.

## ✅ Architecture Recommandée

### 📁 Structure de Fichiers
```
Views/
└── [TypeEntity]/
    ├── [TypeEntity]DetailsPage.xaml
    ├── [TypeEntity]DetailsPage.xaml.cs
ViewModels/
└── [TypeEntity]/
    └── [TypeEntity]DetailsViewModel.cs
Models/Supabase/
└── Supabase[TypeEntity].cs
```

## 🛠️ Template ViewModel Correct

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubExplore.Models.Supabase;
using SubExplore.Services.Interfaces;
using SubExplore.ViewModels.Base;
using SubExplore.Navigation;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace SubExplore.ViewModels.[TypeEntity]
{
    [ShellRoute("[typeentity]details", FriendlyName = "📋 Détails [TypeEntity]", IsVisible = false)]
    [QueryProperty(nameof([TypeEntity]Id), "id")]
    public partial class [TypeEntity]DetailsViewModel : ViewModelBase, IQueryAttributable
    {
        private readonly ISupabaseApiService _supabaseApiService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ISharingService _sharingService;
        private readonly IConnectivityService? _connectivityService;
        private readonly ILogger<[TypeEntity]DetailsViewModel>? _logger;

        [ObservableProperty]
        private Supabase[TypeEntity]? _[typeEntity];

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _isError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Détails [TypeEntity]";

        // 🚀 CRITICAL: QueryProperty must be a public property, NOT ObservableProperty
        public string [TypeEntity]Id { get; set; } = string.Empty;

        // Property change handler for dynamic title updates
        partial void On[TypeEntity]Changed(Supabase[TypeEntity]? value)
        {
            if (value != null && !string.IsNullOrEmpty(value.Name))
            {
                PageTitle = value.Name;
            }
            else
            {
                PageTitle = "Détails [TypeEntity]";
            }
        }

        public [TypeEntity]DetailsViewModel(
            ISupabaseApiService supabaseApiService,
            INavigationService navigationService,
            IDialogService dialogService,
            ISharingService sharingService,
            IConnectivityService? connectivityService = null,
            ILogger<[TypeEntity]DetailsViewModel>? logger = null) : base(dialogService, navigationService)
        {
            _supabaseApiService = supabaseApiService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _sharingService = sharingService;
            _connectivityService = connectivityService;
            _logger = logger;

            Title = "Détails [TypeEntity]";
        }

        // 🚀 CRITICAL: IQueryAttributable implementation for Shell navigation
        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _logger?.LogInformation("ApplyQueryAttributes called with {Count} parameters", query.Count);
            
            if (query.TryGetValue("id", out var idValue))
            {
                [TypeEntity]Id = idValue?.ToString() ?? string.Empty;
                _logger?.LogInformation("ApplyQueryAttributes: [TypeEntity]Id set to {[TypeEntity]Id}", [TypeEntity]Id);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel ApplyQueryAttributes: [TypeEntity]Id set to '{[TypeEntity]Id}'");
                
                // 🚀 CRITICAL FIX: Initialize immediately when QueryProperty is received
                if (!string.IsNullOrEmpty([TypeEntity]Id))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel ApplyQueryAttributes: Triggering initialization with [TypeEntity]Id '{[TypeEntity]Id}'");
                    await InitializeAsync();
                }
            }
            
            // Support alternative parameter names if needed
            if (query.TryGetValue("[typeentity]Id", out var altIdValue))
            {
                [TypeEntity]Id = altIdValue?.ToString() ?? string.Empty;
                _logger?.LogInformation("ApplyQueryAttributes: [TypeEntity]Id (alt) set to {[TypeEntity]Id}", [TypeEntity]Id);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel ApplyQueryAttributes: [TypeEntity]Id (alt) set to '{[TypeEntity]Id}'");
                
                // 🚀 CRITICAL FIX: Initialize immediately when QueryProperty is received (alt)
                if (!string.IsNullOrEmpty([TypeEntity]Id))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel ApplyQueryAttributes: Triggering initialization (alt) with [TypeEntity]Id '{[TypeEntity]Id}'");
                    await InitializeAsync();
                }
            }
        }

        public override async Task InitializeAsync(object parameter = null)
        {
            try
            {
                IsLoading = true;
                IsError = false;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 1 - Starting initialization");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 2 - Parameter is: {parameter?.GetType().Name ?? "null"} = {parameter}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 3 - [TypeEntity]Id QueryProperty: '{[TypeEntity]Id}'");

                // Check QueryProperty first (Shell navigation)
                if (!string.IsNullOrEmpty([TypeEntity]Id))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 4A - Found [TypeEntity]Id from QueryProperty: '{[TypeEntity]Id}'");
                    
                    if (int.TryParse([TypeEntity]Id, out var query[TypeEntity]Id))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 4B - Successfully parsed [TypeEntity]Id as integer: {query[TypeEntity]Id}");
                        _logger?.LogInformation("Found [TypeEntity]Id from QueryProperty as integer: {[TypeEntity]Id}", query[TypeEntity]Id);
                        await Load[TypeEntity]ById(query[TypeEntity]Id);
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 4C - Failed to parse [TypeEntity]Id as integer: '{[TypeEntity]Id}'");
                        _logger?.LogWarning("[TypeEntity]Id from QueryProperty is not a valid integer: {[TypeEntity]Id}", [TypeEntity]Id);
                    }
                }

                // Handle direct parameter navigation (legacy/programmatic)
                if (parameter is Supabase[TypeEntity] [typeEntity])
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 5A - Parameter is Supabase[TypeEntity] object");
                    [TypeEntity] = [typeEntity];
                    await Load[TypeEntity]Details();
                    return;
                }
                else if (parameter is int [typeEntity]Id)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 5B - Parameter is integer: {[typeEntity]Id}");
                    await Load[TypeEntity]ById([typeEntity]Id);
                    return;
                }
                else if (parameter is string stringParam && int.TryParse(stringParam, out var idFromString))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 5C - Parameter is string that parses as integer: {idFromString}");
                    await Load[TypeEntity]ById(idFromString);
                    return;
                }
                else
                {
                    if (parameter == null && string.IsNullOrEmpty([TypeEntity]Id))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 6A - No parameters found");
                        IsLoading = false;
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsViewModel InitializeAsync: STEP 6B - Invalid parameter type");
                    _logger?.LogError("Invalid navigation parameter: {Parameter}", parameter);
                    await _dialogService.ShowAlertAsync("Erreur", "Paramètre de navigation invalide", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in [TypeEntity]DetailsViewModel.InitializeAsync");
                IsError = true;
                ErrorMessage = $"Erreur lors du chargement : {ex.Message}";
            }
            finally
            {
                // 🚀 CRITICAL: Always set IsLoading to false, even on errors
                IsLoading = false;
            }
        }

        private async Task Load[TypeEntity]ById(int [typeEntity]Id)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var [typeEntity]s = await _supabaseApiService.Get[TypeEntity]sAsync().WaitAsync(cts.Token);
                var target[TypeEntity] = [typeEntity]s.FirstOrDefault(o => o.Id == [typeEntity]Id);
                
                if (target[TypeEntity] == null)
                {
                    IsError = true;
                    ErrorMessage = $"[TypeEntity] non trouvé(e) (ID: {[typeEntity]Id})";
                    await _dialogService.ShowAlertAsync("Erreur", $"[TypeEntity] non trouvé(e) (ID: {[typeEntity]Id})", "OK");
                    await _navigationService.GoBackAsync();
                    return;
                }
                
                [TypeEntity] = target[TypeEntity];
                
                if ([TypeEntity] != null)
                {
                    await Load[TypeEntity]Details();
                }
                else
                {
                    IsError = true;
                    ErrorMessage = "Impossible de charger les données de [typeEntity]";
                    await _dialogService.ShowAlertAsync("Erreur", "Impossible de charger les données de [typeEntity]", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
            catch (TimeoutException)
            {
                IsError = true;
                ErrorMessage = "Le chargement a pris trop de temps. Vérifiez votre connexion réseau.";
                await _dialogService.ShowAlertAsync("Timeout", 
                    "Le chargement a pris trop de temps. Vérifiez votre connexion réseau.", "OK");
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading [typeEntity] by ID: {[TypeEntity]Id}", [typeEntity]Id);
                IsError = true;
                ErrorMessage = $"Erreur API Supabase: {ex.Message}";
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur API Supabase: {ex.Message}", "OK");
                await _navigationService.GoBackAsync();
            }
            finally
            {
                // 🚀 CRITICAL: Always set IsLoading to false, even on errors
                IsLoading = false;
            }
        }

        private async Task Load[TypeEntity]Details()
        {
            if ([TypeEntity] == null) 
            {
                IsLoading = false;
                return;
            }

            try
            {
                // Format display information specific to your entity
                // Add your specific formatting logic here
                
                // Example:
                // SomeDisplayProperty = GetSomeDisplayValue([TypeEntity].SomeProperty);
                // CoordinatesDisplay = $"{[TypeEntity].Latitude:F6}, {[TypeEntity].Longitude:F6}";
                
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error formatting [typeEntity] details");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du formatage: {ex.Message}", "OK");
            }
            finally
            {
                // 🚀 CRITICAL: Always set IsLoading to false, even on errors
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Share[TypeEntity]()
        {
            try
            {
                if ([TypeEntity] == null) return;

                // Create sharing text specific to your entity
                var shareText = $"📋 {[TypeEntity].Name}\n";
                // Add more sharing content here
                
                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new Microsoft.Maui.ApplicationModel.DataTransfer.ShareTextRequest
                {
                    Text = shareText,
                    Title = "Partager [typeEntity]"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sharing [typeEntity]");
                await _dialogService.ShowAlertAsync("Erreur", $"Erreur lors du partage : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsLoading = true;
                IsError = false;
                
                if ([TypeEntity] != null)
                {
                    await Load[TypeEntity]ById([TypeEntity].Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing [typeEntity] details");
                IsError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                // 🚀 CRITICAL: Always set IsLoading to false, even on errors
                IsLoading = false;
            }
        }

        [RelayCommand] 
        public async Task Back()
        {
            await _navigationService.GoBackAsync();
        }
    }
}
```

## 🛠️ Template Page.xaml.cs Correct

```csharp
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using SubExplore.ViewModels.[TypeEntity];

namespace SubExplore.Views.[TypeEntity];

public partial class [TypeEntity]DetailsPage : ContentPage
{
    private readonly [TypeEntity]DetailsViewModel _viewModel;
    
    public [TypeEntity]DetailsPage([TypeEntity]DetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // Subscribe to property changes for UI updates
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage.OnAppearing: Checking if initialization needed");
            
            // 🚀 CRITICAL FIX: Only initialize if ApplyQueryAttributes hasn't done it already
            // ApplyQueryAttributes is called before OnAppearing and should handle the initialization
            if (string.IsNullOrEmpty(_viewModel.[TypeEntity]Id))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage.OnAppearing: No [TypeEntity]Id, initializing as fallback");
                await _viewModel.InitializeAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsPage.OnAppearing: [TypeEntity]Id '{_viewModel.[TypeEntity]Id}' already set, ApplyQueryAttributes should have handled initialization");
            }
            
            System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage.OnAppearing: Process completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] [TypeEntity]DetailsPage.OnAppearing: Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] [TypeEntity]DetailsPage.OnAppearing: Full Exception: {ex}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof([TypeEntity]DetailsViewModel.[TypeEntity]) && sender is [TypeEntity]DetailsViewModel vm)
        {
            // Update UI elements when the entity changes
            // Example: UpdateMapLocation(vm);
        }
    }

    // Add your specific UI update methods here
    // Example: UpdateMapLocation, UpdateImages, etc.

    private async void OnCustomHamburgerClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage Custom hamburger button clicked - bypassing MAUI Shell bugs");
            
            bool flyoutOpened = false;
            
            // Method 1: Direct Shell access
            if (Shell.Current != null)
            {
                Shell.Current.FlyoutIsPresented = true;
                flyoutOpened = true;
                System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage ✅ Flyout opened successfully via Shell.Current");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage ❌ No Shell.Current available - trying MessagingCenter");
                
                // Method 2: MessagingCenter communication
                try
                {
                    MessagingCenter.Send<object>(this, "OpenFlyoutMenu");
                    System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage ✅ Flyout request sent via MessagingCenter");
                    await Task.Delay(100);
                    flyoutOpened = true;
                }
                catch (Exception msgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsPage ❌ MessagingCenter failed: {msgEx.Message}");
                }
            }
            
            if (!flyoutOpened)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] [TypeEntity]DetailsPage ⚠️ All flyout access methods failed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] [TypeEntity]DetailsPage ❌ Custom hamburger error: {ex.Message}");
        }
    }
}
```

## ⚙️ Configuration Requise

### 1. Enregistrement DI dans MauiProgram.cs

```csharp
// ViewModels
builder.Services.AddTransient<SubExplore.ViewModels.[TypeEntity].[TypeEntity]DetailsViewModel>();

// Pages  
builder.Services.AddTransient<SubExplore.Views.[TypeEntity].[TypeEntity]DetailsPage>();
```

### 2. Route Shell dans AppShell.xaml

```xml
<ShellContent
    Title="[TypeEntity] Details"
    ContentTemplate="{DataTemplate views:[TypeEntity]DetailsPage}"
    Route="[typeentity]details" />
```

### 3. Navigation Service Update

Vérifier que `BuildQueryParameters` dans NavigationService gère votre nouvelle entité :

```csharp
else if (parameter is SubExplore.Models.Supabase.Supabase[TypeEntity] [typeEntity])
{
    System.Diagnostics.Debug.WriteLine($"[NavigationService] Serializing Supabase[TypeEntity] with ID: {[typeEntity].Id}");
    queryParams.Add($"id={[typeEntity].Id}");
}
```

### 4. API Service Method

Ajouter la méthode dans `ISupabaseApiService` :

```csharp
Task<List<Supabase[TypeEntity]>> Get[TypeEntity]sAsync();
```

## ✅ Checklist de Validation

### Avant de tester votre nouvelle page :

- [ ] ViewModel hérite de `ViewModelBase` et implémente `IQueryAttributable`
- [ ] `[QueryProperty]` pointe vers une propriété publique, PAS `ObservableProperty`
- [ ] `ApplyQueryAttributes` est `async void` et appelle `InitializeAsync()`
- [ ] `OnAppearing` vérifie si l'initialisation est nécessaire (fallback intelligent)
- [ ] Tous les `try/catch` ont des blocs `finally { IsLoading = false; }`
- [ ] Méthodes de chargement ont timeout et gestion d'erreurs complète
- [ ] Enregistrements DI ajoutés dans `MauiProgram.cs`
- [ ] Route Shell configurée dans `AppShell.xaml`
- [ ] NavigationService gère la sérialisation de votre entité
- [ ] Méthode API Supabase existe et fonctionne

### Tests à effectuer :

1. **Navigation depuis carte** : Cliquer sur pin → Page s'ouvre avec données
2. **Logs de debug** : Vérifier la séquence ApplyQueryAttributes → InitializeAsync
3. **Gestion d'erreurs** : Tester avec ID inexistant, problème réseau
4. **Performance** : Pas de double initialisation visible dans les logs
5. **UI responsive** : Loading, erreurs, données affichées correctement

## 🎯 Exemple d'Utilisation

Pour créer une page `EventDetailsPage` :

1. Remplacer `[TypeEntity]` par `Event`
2. Remplacer `[typeentity]` par `event`  
3. Créer le modèle `SupabaseEvent`
4. Ajouter `GetEventsAsync()` au service API
5. Suivre tous les points de la checklist

## 📋 Résumé des Points Critiques

### ✅ À FAIRE ABSOLUMENT
1. **QueryProperty** = Propriété publique, jamais ObservableProperty
2. **ApplyQueryAttributes** = async void + await InitializeAsync()
3. **Finally blocks** = IsLoading = false dans tous les try/catch
4. **OnAppearing intelligent** = Vérification avant initialisation

### ❌ À NE JAMAIS FAIRE  
1. `[ObservableProperty]` avec `[QueryProperty]`
2. InitializeAsync seulement dans OnAppearing
3. Try/catch sans finally { IsLoading = false; }
4. Double initialisation non contrôlée

En suivant ce guide, vous éviterez tous les problèmes rencontrés avec les pages OrganizationDetails et BusinessDetails !