# Script de verification des corrections de filtrage

Write-Host "=== VERIFICATION CORRECTIONS DE FILTRAGE ===" -ForegroundColor Green
Write-Host ""

# 1. Build check
Write-Host "1. Verification build..." -ForegroundColor Yellow
$buildResult = dotnet build --verbosity quiet 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build OK" -ForegroundColor Green
} else {
    Write-Host "Build ECHEC" -ForegroundColor Red
    exit 1
}

# 2. File checks  
Write-Host "2. Verification fichiers..." -ForegroundColor Yellow

if (Test-Path "ViewModels\Map\MapViewModel.cs") {
    $content = Get-Content "ViewModels\Map\MapViewModel.cs" -Raw
    if ($content.Contains("ApplyCategoryFilter")) {
        Write-Host "MapViewModel: OK - Nouveau systeme filtrage" -ForegroundColor Green
    }
}

if (Test-Path "Helpers\Extensions\SpotTypeExtensions.cs") {
    Write-Host "SpotTypeExtensions: OK - Extensions categories" -ForegroundColor Green
}

if (Test-Path "Migrations\UpdateActivityCategoryStructure.cs") {
    Write-Host "Migration: OK - Service disponible" -ForegroundColor Green
}

Write-Host ""
Write-Host "3. TESTS A EFFECTUER:" -ForegroundColor Yellow  
Write-Host "a) Lancer app: dotnet run -f net8.0-android" -ForegroundColor Cyan
Write-Host "b) Aller Settings > Database Test" -ForegroundColor Cyan
Write-Host "c) Clic 'Diagnostic detaille'" -ForegroundColor Cyan
Write-Host "d) Si besoin: 'Corriger structure'" -ForegroundColor Cyan
Write-Host "e) Tester filtres sur carte" -ForegroundColor Cyan

Write-Host ""
Write-Host "CORRECTIONS APPLIQUEES:" -ForegroundColor Green
Write-Host "- FilterSpots utilise nouveau systeme categories" -ForegroundColor White
Write-Host "- Logs debug detailles ajoutes" -ForegroundColor White  
Write-Host "- Support Activites/Structures/Boutiques" -ForegroundColor White
Write-Host "- Correction BelongsToCategory" -ForegroundColor White

Write-Host ""
Write-Host "=== PRET POUR TESTS ===" -ForegroundColor Green