# Spot System Diagnostic Script
# Run this from the project directory: powershell -File spotdiagnostic.ps1

Write-Host "=== SubExplore Spot System Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

# Check if the app builds
Write-Host "1. Checking if the app builds..." -ForegroundColor Yellow
$buildResult = dotnet build -f net8.0-android --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "2. Next steps to diagnose:" -ForegroundColor Yellow
Write-Host "   a) Run the SQL queries in MySQL Workbench to check database state"
Write-Host "   b) Start the app and check debug console output"
Write-Host "   c) Test the filters on the map"

Write-Host ""
Write-Host "SQL Diagnostic Queries:" -ForegroundColor Cyan
Write-Host @"
-- 1. Check SpotTypes status
SELECT Id, Name, Category, IsActive, CreatedAt 
FROM SpotTypes 
ORDER BY Category, Name;

-- 2. Count spots by validation status
SELECT ValidationStatus, COUNT(*) as Count
FROM Spots 
GROUP BY ValidationStatus;

-- 3. Check spots with their types
SELECT 
    s.Id, 
    s.Name as SpotName, 
    s.ValidationStatus,
    st.Id as TypeId,
    st.Name as TypeName, 
    st.Category,
    st.IsActive as TypeIsActive
FROM Spots s 
LEFT JOIN SpotTypes st ON s.TypeId = st.Id 
LIMIT 10;

-- 4. Count spots by category (only active types and approved spots)
SELECT 
    st.Category,
    COUNT(s.Id) as SpotCount,
    st.Name as TypeName
FROM Spots s 
JOIN SpotTypes st ON s.TypeId = st.Id 
WHERE st.IsActive = 1 AND s.ValidationStatus = 'Approved'
GROUP BY st.Category, st.Name
ORDER BY st.Category;
"@

Write-Host ""
Write-Host "Run these SQL queries in MySQL Workbench and send me the results." -ForegroundColor Green
Write-Host "This will help identify exactly what's wrong." -ForegroundColor Green