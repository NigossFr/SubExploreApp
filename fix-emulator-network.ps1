# =====================================
# CORRECTIF RESEAU EMULATEUR ANDROID
# =====================================

Write-Host "=====================================" -ForegroundColor Green
Write-Host "   CORRECTIF RESEAU EMULATEUR ANDROID" -ForegroundColor Green  
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

Write-Host "Etape 1: Verification de la connectivite de l'hote..." -ForegroundColor Yellow
try {
    $result = nslookup iguvwnyehojvxkyqzaoi.supabase.co
    Write-Host "✅ Hote peut resoudre Supabase domain" -ForegroundColor Green
    Write-Host $result
} catch {
    Write-Host "❌ Erreur de resolution DNS sur l'hote" -ForegroundColor Red
}
Write-Host ""

Write-Host "Etape 2: Verification de l'emulateur connecte..." -ForegroundColor Yellow
try {
    $devices = adb devices
    Write-Host $devices
    if ($devices -match "emulator") {
        Write-Host "✅ Emulateur Android detecte" -ForegroundColor Green
    } else {
        Write-Host "❌ Aucun emulateur Android detecte" -ForegroundColor Red
        Write-Host "Veuillez demarrer l'emulateur Android d'abord" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ ADB non trouve ou erreur" -ForegroundColor Red
}
Write-Host ""

Write-Host "Etape 3: Test de connectivite depuis l'emulateur..." -ForegroundColor Yellow
Write-Host "Testing Google DNS..." -ForegroundColor Cyan
try {
    $pingResult = adb shell ping -c 1 8.8.8.8
    if ($pingResult -match "1 packets transmitted, 1 received") {
        Write-Host "✅ Connectivite Internet OK" -ForegroundColor Green
    } else {
        Write-Host "❌ Pas de connectivite Internet" -ForegroundColor Red
        Write-Host $pingResult
    }
} catch {
    Write-Host "❌ Erreur test ping Internet" -ForegroundColor Red
}

Write-Host "Testing Supabase domain..." -ForegroundColor Cyan
try {
    $supabaseTest = adb shell ping -c 1 iguvwnyehojvxkyqzaoi.supabase.co
    if ($supabaseTest -match "1 packets transmitted, 1 received") {
        Write-Host "✅ Supabase accessible" -ForegroundColor Green
    } else {
        Write-Host "❌ Supabase inaccessible" -ForegroundColor Red
        Write-Host $supabaseTest
    }
} catch {
    Write-Host "❌ Erreur test Supabase" -ForegroundColor Red
}
Write-Host ""

Write-Host "Etape 4: Tentative de configuration DNS emulateur..." -ForegroundColor Yellow
Write-Host "Configuration DNS Google (8.8.8.8)..." -ForegroundColor Cyan
try {
    adb shell "su -c 'setprop net.dns1 8.8.8.8'"
    adb shell "su -c 'setprop net.dns2 8.8.4.4'"
    Write-Host "✅ Configuration DNS tentee" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Configuration DNS a echoue (normal sans root)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "Etape 5: Test final..." -ForegroundColor Yellow
try {
    $finalTest = adb shell ping -c 1 iguvwnyehojvxkyqzaoi.supabase.co
    if ($finalTest -match "1 packets transmitted, 1 received") {
        Write-Host "✅ SUCCESS: Supabase maintenant accessible!" -ForegroundColor Green
    } else {
        Write-Host "❌ Supabase toujours inaccessible" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Test final a echoue" -ForegroundColor Red
}
Write-Host ""

Write-Host "=====================================" -ForegroundColor Green
Write-Host "SOLUTIONS ALTERNATIVES:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Redemarrer l'emulateur avec DNS custom:" -ForegroundColor White
Write-Host "   emulator @your_avd_name -dns-server 8.8.8.8,8.8.4.4" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Dans Android Studio:" -ForegroundColor White
Write-Host "   - Ouvrir AVD Manager" -ForegroundColor Gray
Write-Host "   - Editer l'emulateur" -ForegroundColor Gray
Write-Host "   - Advanced Settings > Network > DNS Server: 8.8.8.8" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Solution dans le code (DEJA IMPLEMENTEE):" -ForegroundColor White
Write-Host "   - Utilisation IP directe (104.18.38.10) automatiquement" -ForegroundColor Gray
Write-Host "   - EmulatorNetworkFix.cs detecte et corrige" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Si probleme persiste:" -ForegroundColor White
Write-Host "   - Redemarrer l'emulateur completement" -ForegroundColor Gray
Write-Host "   - Redemarrer Android Studio" -ForegroundColor Gray
Write-Host "   - Verifier les parametres firewall/antivirus" -ForegroundColor Gray
Write-Host "=====================================" -ForegroundColor Green