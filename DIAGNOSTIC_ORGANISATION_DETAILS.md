# 🔍 DIAGNOSTIC - Organisation Details Page Issue

## État Actuel des Corrections

✅ **Fix #1 - QueryProperty Correctement Configuré**
```csharp
// Dans OrganizationDetailsViewModel.cs ligne 64
public string OrganizationId { get; set; } = string.Empty;

// QueryProperty bien défini ligne 13
[QueryProperty(nameof(OrganizationId), "id")]
```

✅ **Fix #2 - OnAppearing Implémenté**
```csharp
// Dans OrganizationDetailsPage.xaml.cs ligne 21-39
protected override async void OnAppearing()
{
    base.OnAppearing();
    try
    {
        await _viewModel.InitializeAsync();  // ✅ PRÉSENT
    }
    catch (Exception ex) { /* Gestion erreur */ }
}
```

✅ **Fix #3 - Finally Block Correct**
```csharp
// Dans LoadOrganizationById ligne 242-245
finally
{
    IsLoading = false;  // ✅ PRÉSENT
}
```

✅ **Fix #4 - Enregistrement DI**
```csharp
// Dans MauiProgram.cs
builder.Services.AddTransient<OrganizationDetailsViewModel>();  // Ligne 423
builder.Services.AddTransient<OrganizationDetailsPage>();       // Ligne 478
```

✅ **Fix #5 - Route Shell**
```csharp
// Dans OrganizationDetailsViewModel.cs ligne 12
[ShellRoute("organizationdetails", FriendlyName = "🏢 Détails Organisation", IsVisible = false)]

// Dans AppShell.xaml ligne 345
Route="organizationdetails"
```

## Flux Navigation Théorique

### Étape 1: Navigation MapViewModel
```csharp
// MapViewModel.ShowOrganizationDetailsAsync() ligne 371
await NavigationService.NavigateToAsync<OrganizationDetailsViewModel>(org);
```

### Étape 2: NavigationService BuildQueryParameters
```csharp
// NavigationService.BuildQueryParameters() - SupabaseOrganization
queryParams.Add($"id={organization.Id}");
// Résultat: "///organizationdetails?id=3"
```

### Étape 3: Shell Navigation
```csharp
await Shell.Current.GoToAsync("///organizationdetails?id=3", true);
```

### Étape 4: QueryProperty Reception
```csharp
[QueryProperty(nameof(OrganizationId), "id")]
// OrganizationId devrait = "3"
```

### Étape 5: Page OnAppearing
```csharp
await _viewModel.InitializeAsync();
```

### Étape 6: ViewModel InitializeAsync
```csharp
// Devrait parser OrganizationId = "3" vers int 3
// Puis appeler LoadOrganizationById(3)
```

## Étapes de Test Manuel

### Test 1: Vérifier Navigation Base
1. Lancer l'app
2. Aller sur la carte (MapPage)
3. Cliquer sur un spot Organisation
4. Observer les logs dans la console de debug

**Logs Attendus:**
```
[NavigationService] Shell navigation to: ///organizationdetails?id=X
[NavigationService] ✅ Shell navigation succeeded
[DEBUG] OrganizationDetailsPage.OnAppearing: Starting ViewModel initialization
```

### Test 2: Vérifier QueryProperty
**Logs Attendus:**
```
[DEBUG] OrganizationDetailsViewModel InitializeAsync: STEP 3 - OrganizationId QueryProperty: 'X'
[DEBUG] OrganizationDetailsViewModel InitializeAsync: STEP 4A - Found OrganizationId from QueryProperty: 'X'
[DEBUG] OrganizationDetailsViewModel InitializeAsync: STEP 4B - Successfully parsed OrganizationId as integer: X
```

### Test 3: Vérifier API Supabase
**Logs Attendus:**
```
📥 Récupération des organisations...
✅ X organisation(s) récupérée(s)
[DEBUG] OrganizationDetailsPage.OnAppearing: ViewModel initialization completed successfully
```

## Diagnostic par Élimination

### Si Navigation Échoue (Pas de logs NavigationService)
❌ **Problème**: Navigation pas déclenchée
🔍 **Check**: MapViewModel.ShowOrganizationDetailsAsync appelé ?

### Si Navigation Réussit mais Page Vide (Logs NavigationService OK, pas OnAppearing)
❌ **Problème**: OnAppearing pas appelé
🔍 **Check**: OrganizationDetailsPage bien instanciée ?

### Si OnAppearing Appelé mais QueryProperty Vide
❌ **Problème**: QueryProperty pas reçu
🔍 **Check**: Route Shell configuration et paramètres URL

### Si QueryProperty Reçu mais LoadOrganizationById Échoue
❌ **Problème**: API Supabase ou données
🔍 **Check**: Logs API et base de données

### Si LoadOrganizationById Réussit mais UI Vide
❌ **Problème**: Binding ou données nulles
🔍 **Check**: Organization property et bindings XAML

## Test Rapide de Validation

Pour tester rapidement, ajoutez ces logs temporaires dans OrganizationDetailsPage.xaml.cs:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    
    System.Diagnostics.Debug.WriteLine("=== DIAGNOSTIC ORGANIZATION DETAILS ===");
    System.Diagnostics.Debug.WriteLine($"ViewModel: {_viewModel != null}");
    System.Diagnostics.Debug.WriteLine($"BindingContext: {BindingContext != null}");
    System.Diagnostics.Debug.WriteLine($"OrganizationId avant init: '{_viewModel?.OrganizationId}'");
    
    try
    {
        await _viewModel.InitializeAsync();
        System.Diagnostics.Debug.WriteLine($"OrganizationId après init: '{_viewModel?.OrganizationId}'");
        System.Diagnostics.Debug.WriteLine($"Organization loaded: {_viewModel?.Organization != null}");
        System.Diagnostics.Debug.WriteLine($"IsLoading: {_viewModel?.IsLoading}");
        System.Diagnostics.Debug.WriteLine($"IsError: {_viewModel?.IsError}");
        System.Diagnostics.Debug.WriteLine("=== FIN DIAGNOSTIC ===");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"ERREUR DIAGNOSTIC: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
    }
}
```

## Instructions Prochaine Étape

**Merci de:**
1. Tester la navigation vers une organisation
2. Copier tous les logs de debug dans la console
3. Indiquer exactement ce qui s'affiche à l'écran (page vide, chargement infini, erreur, etc.)

Cela nous permettra d'identifier précisément où le flux se casse.