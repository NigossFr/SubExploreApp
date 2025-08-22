#!/usr/bin/env pwsh
# Test de connectivité réseau pour diagnostiquer le problème Supabase

Write-Host "🔍 DIAGNOSTIC CONNECTIVITÉ RÉSEAU" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Test 1: Résolution DNS
Write-Host "📡 Test 1: Résolution DNS..." -ForegroundColor Cyan
try {
    $hostname = "iguvwnyehojvxkyqzaoi.supabase.co"
    $result = Resolve-DnsName -Name $hostname -ErrorAction Stop
    Write-Host "✅ DNS OK: $hostname résolu vers $($result.IPAddress -join ', ')" -ForegroundColor Green
} catch {
    Write-Host "❌ DNS ÉCHEC: Impossible de résoudre $hostname" -ForegroundColor Red
    Write-Host "   Erreur: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Test 2: Connectivité IP directe
Write-Host "🌐 Test 2: Connectivité IP directe..." -ForegroundColor Cyan
try {
    $ip = "104.18.38.10"
    $result = Test-NetConnection -ComputerName $ip -Port 443 -InformationLevel Quiet
    if ($result) {
        Write-Host "✅ IP OK: $ip accessible sur port 443" -ForegroundColor Green
    } else {
        Write-Host "❌ IP ÉCHEC: $ip non accessible sur port 443" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ IP ÉCHEC: Erreur lors du test de $ip" -ForegroundColor Red
    Write-Host "   Erreur: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Test 3: Connectivité HTTPS
Write-Host "🔒 Test 3: Connectivité HTTPS..." -ForegroundColor Cyan
try {
    $url = "https://iguvwnyehojvxkyqzaoi.supabase.co/rest/v1/"
    $response = Invoke-WebRequest -Uri $url -Method HEAD -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ HTTPS OK: Code de réponse $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ HTTPS ÉCHEC: Impossible de se connecter à Supabase" -ForegroundColor Red
    Write-Host "   Erreur: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Test 4: Test avec IP directe
Write-Host "🔧 Test 4: HTTPS avec IP directe..." -ForegroundColor Cyan
try {
    $urlDirect = "https://104.18.38.10/rest/v1/"
    $headers = @{
        'Host' = 'iguvwnyehojvxkyqzaoi.supabase.co'
    }
    $response = Invoke-WebRequest -Uri $urlDirect -Method HEAD -Headers $headers -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ IP DIRECT OK: Code de réponse $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ IP DIRECT ÉCHEC: Impossible de se connecter via IP" -ForegroundColor Red
    Write-Host "   Erreur: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🏁 DIAGNOSTIC TERMINÉ" -ForegroundColor Green
Write-Host ""

# Recommandations
Write-Host "RECOMMANDATIONS:" -ForegroundColor Yellow
Write-Host "   1. Si DNS echoue mais IP fonctionne - Probleme DNS emulateur"
Write-Host "   2. Si IP directe fonctionne - Notre correctif devrait marcher"
Write-Host "   3. Si tout echoue - Probleme reseau general"
Write-Host ""