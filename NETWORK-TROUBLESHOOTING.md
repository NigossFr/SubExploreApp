# Guide de résolution des problèmes réseau - SubExplore

## 🔍 Problèmes identifiés

D'après les logs de debug, l'application rencontre des problèmes de connectivité réseau sur l'émulateur Android :

```
UnknownHostException: Unable to resolve host 'www.google.com'
UnknownHostException: Unable to resolve host 'api.openweathermap.org'
```

## 🛠️ Solutions pour l'émulateur Android

### 1. **Configuration DNS de l'émulateur**

L'émulateur Android peut avoir des problèmes de résolution DNS. Essayez ces solutions :

#### Option A : Redémarrer l'émulateur avec configuration DNS
```bash
# Fermer l'émulateur actuel
# Redémarrer avec des DNS publics
emulator -avd YOUR_AVD_NAME -dns-server 8.8.8.8,8.8.4.4
```

#### Option B : Utiliser Wipe Data
```bash
# Dans Android Studio -> AVD Manager
# Clic droit sur votre émulateur -> Wipe Data
# Redémarrer l'émulateur
```

### 2. **Configuration réseau de l'émulateur**

#### Vérifier les paramètres réseau :
```bash
# Dans l'émulateur Android, aller dans :
Paramètres > Réseau et Internet > Wi-Fi
# Vérifier que le Wi-Fi est connecté à "AndroidWifi"
```

#### Test de connectivité dans l'émulateur :
```bash
# Ouvrir le navigateur dans l'émulateur
# Essayer d'aller sur : http://www.google.com
# Si ça ne marche pas, le problème est au niveau de l'émulateur
```

### 3. **Configuration de l'ordinateur hôte**

#### Windows : Vérifier les paramètres réseau
```bash
# Vérifier que Hyper-V est bien configuré
# Aller dans : Panneau de configuration > Programmes > Activer/désactiver des fonctionnalités Windows
# Vérifier que "Hyper-V" est coché
```

#### Pare-feu Windows
```bash
# Aller dans : Paramètres > Mise à jour et sécurité > Sécurité Windows > Pare-feu
# Autoriser "Android Emulator" et "qemu-system-x86_64.exe"
```

### 4. **Solutions alternatives pour le développement**

#### Option A : Utiliser un appareil Android physique
```bash
# Activer le mode développeur sur votre téléphone
# Connecter via USB
# La connectivité réseau sera celle de votre téléphone
```

#### Option B : Configurer un proxy HTTP
```bash
# Dans l'émulateur : Paramètres > Wi-Fi > AndroidWifi > Modifier
# Configurer un proxy si nécessaire
```

## 🔧 Modifications apportées au code

### 1. **Optimisation des performances**
- ✅ Chargement parallèle des données dans `SpotDetailsViewModel`
- ✅ Vérification de connectivité avant les appels réseau
- ✅ Gestion d'erreurs améliorée avec messages spécifiques

### 2. **Amélioration de la robustesse météo**
- ✅ Timeout réduit pour les tests de disponibilité (5s au lieu de 30s)
- ✅ Vérification de connectivité avant les appels API
- ✅ Messages d'erreur plus informatifs
- ✅ Fallback gracieux quand la météo n'est pas disponible

### 3. **Messages d'erreur améliorés**
```csharp
// Avant : "Erreur lors du chargement des données météo"
// Après : Messages spécifiques selon le problème :
"Pas de connexion internet - données météo indisponibles"
"Service météo temporairement indisponible" 
"Délai d'attente dépassé - données météo indisponibles"
"Problème de réseau - données météo indisponibles"
```

## 📱 Test de la solution

### 1. **Vérifier les améliorations de performance**
- La page de détails du spot devrait se charger plus rapidement
- Les données non-critiques (météo) ne bloquent plus le chargement principal

### 2. **Vérifier la gestion des erreurs météo**
- Si pas de réseau : Message clair "Pas de connexion internet"
- Si API indisponible : Message "Service météo temporairement indisponible"
- L'application reste fonctionnelle même sans données météo

### 3. **Test sur appareil physique**
- Toutes les fonctionnalités devraient marcher parfaitement
- API météo accessible avec une vraie connexion internet

## 🚀 Prochaines étapes recommandées

1. **Court terme** : Tester sur un appareil Android physique
2. **Moyen terme** : Configurer un émulateur avec meilleure connectivité réseau
3. **Long terme** : Ajouter un mode hors-ligne avec données météo en cache

## 📊 API Météo - Configuration

L'API OpenWeatherMap est correctement configurée dans `appsettings.json` :
```json
"WeatherService": {
  "ApiKey": "af95aba5c4f5e33136c5077d0d04363e",
  "CacheExpirationMinutes": 30
}
```

Le problème n'est pas la configuration mais la connectivité réseau de l'émulateur.