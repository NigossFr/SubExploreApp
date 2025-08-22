@echo off
echo =====================================
echo    CORRECTIF RESEAU EMULATEUR ANDROID
echo =====================================
echo.

echo Etape 1: Verification de la connectivite de l'hote...
nslookup iguvwnyehojvxkyqzaoi.supabase.co
echo.

echo Etape 2: Verification de l'emulateur connecte...
adb devices
echo.

echo Etape 3: Test de connectivite depuis l'emulateur...
echo Testing Google DNS...
adb shell ping -c 1 8.8.8.8
echo.

echo Testing Supabase domain...
adb shell ping -c 1 iguvwnyehojvxkyqzaoi.supabase.co
echo.

echo Etape 4: Configuration DNS emulateur...
echo Redemarrage du service reseau...
adb shell su -c "setprop net.dns1 8.8.8.8"
adb shell su -c "setprop net.dns2 8.8.4.4"
echo.

echo Etape 5: Test final...
adb shell ping -c 1 iguvwnyehojvxkyqzaoi.supabase.co
echo.

echo =====================================
echo SOLUTIONS ALTERNATIVES:
echo.
echo 1. Redemarrer l'emulateur avec:
echo    emulator @your_avd_name -dns-server 8.8.8.8,8.8.4.4
echo.
echo 2. Dans Android Studio:
echo    - Ouvrir AVD Manager
echo    - Editer l'emulateur
echo    - Advanced Settings ^> Network ^> DNS Server: 8.8.8.8
echo.
echo 3. Utiliser IP directe dans le code (temporaire):
echo    URL: "https://104.18.38.10" (avec host header)
echo.
echo 4. Configurer proxy si necessaire:
echo    emulator @your_avd_name -http-proxy http://proxy:port
echo =====================================