# Optimisation de l'émulateur Android pour MAUI

## Configuration recommandée de l'émulateur

### 1. **Spécifications minimales**
```
- RAM : 4GB minimum (8GB recommandé)
- Internal Storage : 8GB minimum
- Graphics : Hardware - GLES 2.0
- Multi-Core CPU : 4 cores minimum
```

### 2. **Paramètres AVD Manager**
```
Device Definition : Pixel 7 ou Pixel 6
Target : Android 13.0 (API 33) ou 14.0 (API 34)
CPU/ABI : x86_64 (plus rapide que ARM sur PC)
Graphics : Hardware - GLES 2.0
RAM : 6144 MB
VM Heap : 512 MB
Internal Storage : 8192 MB
```

### 3. **Options avancées dans AVD**
```
- Enable Hardware Keyboard : Oui
- Enable Device Frame : Non (améliore les performances)
- Boot Option : Cold Boot (évite les états corrompus)
```

## Configuration Windows/Visual Studio

### 1. **Paramètres Windows**
```powershell
# Activer Hyper-V et WHPX
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

### 2. **Variables d'environnement**
```
ANDROID_EMULATOR_USE_SYSTEM_LIBS=1
ANDROID_SDK_ROOT=C:\Users\[User]\AppData\Local\Android\Sdk
```

### 3. **Visual Studio 2022 optimisations**
- Tools → Options → Debugging → General : Décocher "Enable Diagnostic Tools"
- Tools → Options → Debugging → General : Décocher "Show elapsed time PerfTip"
- Build → Configuration : Release mode pour les tests de performance

## Commandes émulateur optimisées

### Lancement avec paramètres optimisés :
```bash
emulator -avd Pixel_7_API_33 -gpu host -memory 6144 -cores 4 -accel on
```

### Vérification GPU :
```bash
adb shell getprop ro.hardware.egl
adb shell getprop ro.opengles.version
```

## Diagnostic des performances

### Commandes de monitoring :
```bash
# Vérifier les FPS
adb shell dumpsys gfxinfo com.companyname.subexplore

# Monitoring GPU
adb shell cat /sys/class/kgsl/kgsl-3d0/gpubusy

# Memory usage
adb shell dumpsys meminfo com.companyname.subexplore
```

## Alternative : Device physique

Pour de meilleures performances, utilisez un appareil Android physique :
1. Activer le mode développeur
2. Activer le débogage USB
3. Connecter via USB avec Visual Studio

Les performances seront considérablement meilleures qu'avec l'émulateur.