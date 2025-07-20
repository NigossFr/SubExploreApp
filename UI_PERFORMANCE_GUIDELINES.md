# Guidelines de performance UI pour MAUI

## ✅ **Bonnes pratiques (À faire)**

### Layouts
- Utiliser `StackLayout` et `Grid` simples
- Éviter les layouts imbriqués >3 niveaux
- Préférer `FlexLayout` pour des layouts complexes

### Styling
- Utiliser des couleurs hardcodées pour les éléments critiques
- Limiter les `StaticResource` dans les pages de première importance
- Éviter les `DynamicResource` sauf nécessité absolue

### Performance
```xml
<!-- ✅ Bon -->
<Frame BackgroundColor="#FFFFFF" 
       HasShadow="False">
</Frame>

<!-- ❌ Éviter -->
<Frame BackgroundColor="{StaticResource PrimaryColor}" 
       HasShadow="True">
</Frame>
```

## ❌ **À éviter sur émulateur**

### Éléments coûteux
- `HasShadow="True"` sur Frame/Button
- `ScrollView` avec contenu complexe
- Trop de `Entry` avec validation en temps réel
- `CollectionView` avec ItemTemplate complexe

### Bindings
- Éviter les `Converter` complexes dans l'UI critique
- Limiter les `MultiBinding`
- Préférer `x:Bind` à `Binding` quand possible

## 🎯 **Pattern de fallback UI**

```csharp
// Dans vos ViewModels
public bool IsEmulatorMode => DeviceInfo.DeviceType == DeviceType.Virtual;

// Dans vos Pages
public void ConfigureForPerformance()
{
    if (DeviceInfo.DeviceType == DeviceType.Virtual)
    {
        // Version simplifiée pour émulateur
        ComplexFrame.HasShadow = false;
        AnimationView.IsVisible = false;
    }
}
```

## 📱 **Test sur device physique**

Pour les fonctionnalités critiques, toujours tester sur :
1. Android physique (recommandé)
2. iPhone physique si cross-platform
3. Émulateur seulement pour tests fonctionnels