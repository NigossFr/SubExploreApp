#!/usr/bin/env pwsh

# Script pour vérifier l'état des corrections de filtrage

Write-Host "=== VÉRIFICATION DES CORRECTIONS DE FILTRAGE ===" -ForegroundColor Green
Write-Host ""

Write-Host "1. Vérification de la compilation..." -ForegroundColor Yellow
$buildResult = dotnet build --verbosity quiet 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build réussi" -ForegroundColor Green
} else {
    Write-Host "❌ Erreur de build:" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

Write-Host ""
Write-Host "2. Vérification des fichiers modifiés..." -ForegroundColor Yellow

# Vérifier MapViewModel.cs
if (Test-Path "ViewModels\Map\MapViewModel.cs") {
    $content = Get-Content "ViewModels\Map\MapViewModel.cs" -Raw
    if ($content.Contains("ApplyCategoryFilter") -and $content.Contains("BelongsToCategory")) {
        Write-Host "✅ MapViewModel.cs: Nouveau système de filtrage par catégorie" -ForegroundColor Green
    } else {
        Write-Host "❌ MapViewModel.cs: Système de filtrage non mis à jour" -ForegroundColor Red
    }
}

# Vérifier SpotTypeExtensions.cs
if (Test-Path "Helpers\Extensions\SpotTypeExtensions.cs") {
    $content = Get-Content "Helpers\Extensions\SpotTypeExtensions.cs" -Raw
    if ($content.Contains("BelongsToCategory") -and $content.Contains('ActivityCategory.Activity')) {
        Write-Host "✅ SpotTypeExtensions.cs: Extensions de catégorie mises à jour" -ForegroundColor Green
    } else {
        Write-Host "❌ SpotTypeExtensions.cs: Extensions non configurées" -ForegroundColor Red
    }
}

# Vérifier UpdateActivityCategoryStructure.cs  
if (Test-Path "Migrations\UpdateActivityCategoryStructure.cs") {
    Write-Host "✅ Migrations: Service de migration disponible" -ForegroundColor Green
} else {
    Write-Host "❌ Migrations: Service de migration manquant" -ForegroundColor Red
}

# Vérifier SpotTypeDiagnosticService.cs
if (Test-Path "Services\Implementations\SpotTypeDiagnosticService.cs") {
    Write-Host "✅ Services: Service de diagnostic disponible" -ForegroundColor Green  
} else {
    Write-Host "❌ Services: Service de diagnostic manquant" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Instructions pour tester l'application..." -ForegroundColor Yellow
Write-Host "   a) Lancez l'application avec 'dotnet run -f net8.0-android'" -ForegroundColor Cyan
Write-Host "   b) Allez dans Settings > Database Test" -ForegroundColor Cyan
Write-Host "   c) Cliquez sur '🔍 Diagnostic détaillé'" -ForegroundColor Cyan
Write-Host "   d) Vérifiez l'état des types de spots et catégories" -ForegroundColor Cyan
Write-Host "   e) Si nécessaire, cliquez sur '🔧 Corriger structure'" -ForegroundColor Cyan
Write-Host "   f) Testez ensuite les filtres sur la carte" -ForegroundColor Cyan

Write-Host ""
Write-Host "4. Changements apportés:" -ForegroundColor Yellow
Write-Host "   • FilterSpots() utilise maintenant le nouveau système par catégorie" -ForegroundColor White
Write-Host "   • ApplyCategoryFilter() inclut des logs de debug détaillés" -ForegroundColor White  
Write-Host "   • Support complet pour 'Activités', 'Structures', 'Boutiques'" -ForegroundColor White
Write-Host "   • Correction de la liaison Type -> BelongsToCategory()" -ForegroundColor White

Write-Host ""
Write-Host "=== DIAGNOSTIC TERMINÉ ===" -ForegroundColor Green