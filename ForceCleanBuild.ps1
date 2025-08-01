# Script PowerShell pour nettoyer complètement le projet MAUI
Write-Host "=== FORCE CLEAN BUILD SCRIPT ===" -ForegroundColor Yellow

# Arrêter tous les processus liés
Write-Host "Arrêt des processus..." -ForegroundColor Cyan
Get-Process | Where-Object { $_.ProcessName -like "*devenv*" -or $_.ProcessName -like "*MSBuild*" -or $_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*emulator*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Attendre que les processus se terminent
Start-Sleep -Seconds 3

# Supprimer les dossiers bin et obj
Write-Host "Suppression des dossiers de build..." -ForegroundColor Cyan
if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force -ErrorAction SilentlyContinue }

# Nettoyer les caches NuGet (avec retry)
Write-Host "Nettoyage des caches NuGet..." -ForegroundColor Cyan
$maxRetries = 3
for ($i = 1; $i -le $maxRetries; $i++) {
    try {
        dotnet nuget locals all --clear
        Write-Host "Cache NuGet nettoyé avec succès" -ForegroundColor Green
        break
    }
    catch {
        Write-Host "Tentative $i échouée, nouvelle tentative..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

# Nettoyer le cache MAUI
Write-Host "Nettoyage du cache MAUI..." -ForegroundColor Cyan
$userProfile = $env:USERPROFILE
$mauiCachePaths = @(
    "$userProfile\.nuget\packages\microsoft.maui*",
    "$userProfile\.nuget\packages\microsoft.windowsappsdk*",
    "$userProfile\.android",
    "$env:LOCALAPPDATA\Xamarin",
    "$env:LOCALAPPDATA\Temp\Xamarin"
)

foreach ($path in $mauiCachePaths) {
    if (Test-Path $path) {
        try {
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "Supprimé: $path" -ForegroundColor Green
        }
        catch {
            Write-Host "Impossible de supprimer: $path" -ForegroundColor Red
        }
    }
}

Write-Host "=== NETTOYAGE TERMINÉ ===" -ForegroundColor Green
Write-Host "Vous pouvez maintenant essayer:" -ForegroundColor Yellow
Write-Host "1. dotnet restore" -ForegroundColor White
Write-Host "2. dotnet build -f net8.0-android" -ForegroundColor White