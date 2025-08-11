# Script PowerShell pour exécuter la migration des types de spots
Write-Host "=== Migration des types de spots SubExplore ===" -ForegroundColor Green

# Paramètres de connexion
$server = "localhost"
$port = 3306
$database = "subexplore_dev"
$username = "subexplore_user"
$password = "seb09081980"
$sqlFile = "D:\Developpement\SubExploreApp\SubExplore\migrate_spot_types.sql"

# Essayer différents chemins possibles pour mysql.exe
$mysqlPaths = @(
    "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
    "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe",
    "C:\Program Files\MySQL\MySQL Server 9.0\bin\mysql.exe",
    "C:\Program Files (x86)\MySQL\MySQL Server 8.0\bin\mysql.exe",
    "C:\xampp\mysql\bin\mysql.exe",
    "C:\wamp64\bin\mysql\mysql8.0.31\bin\mysql.exe",
    "mysql.exe" # Si c'est dans le PATH
)

$mysqlExe = $null
foreach ($path in $mysqlPaths) {
    if (Test-Path $path) {
        $mysqlExe = $path
        Write-Host "MySQL trouvé : $mysqlExe" -ForegroundColor Yellow
        break
    }
}

if ($mysqlExe -eq $null) {
    Write-Host "Tentative de localisation de MySQL..." -ForegroundColor Yellow
    try {
        $mysqlExe = (Get-Command mysql -ErrorAction Stop).Source
        Write-Host "MySQL trouvé dans le PATH : $mysqlExe" -ForegroundColor Yellow
    }
    catch {
        Write-Host "ERREUR: MySQL non trouvé. Veuillez installer MySQL ou l'ajouter au PATH." -ForegroundColor Red
        Write-Host "Chemins vérifiés :" -ForegroundColor Red
        foreach ($path in $mysqlPaths) {
            Write-Host "  - $path" -ForegroundColor Red
        }
        exit 1
    }
}

# Vérifier que le fichier SQL existe
if (-not (Test-Path $sqlFile)) {
    Write-Host "ERREUR: Fichier SQL non trouvé : $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Fichier SQL trouvé : $sqlFile" -ForegroundColor Green

# Test de connexion
Write-Host "Test de connexion à la base de données..." -ForegroundColor Yellow
$testCmd = "& '$mysqlExe' -h $server -P $port -u $username -p$password -e 'SELECT VERSION();' $database"
try {
    Invoke-Expression $testCmd
    Write-Host "✅ Connexion réussie !" -ForegroundColor Green
}
catch {
    Write-Host "❌ ERREUR: Impossible de se connecter à la base de données." -ForegroundColor Red
    Write-Host "Vérifiez :" -ForegroundColor Red
    Write-Host "  - MySQL est démarré" -ForegroundColor Red
    Write-Host "  - Les paramètres de connexion sont corrects" -ForegroundColor Red
    Write-Host "  - L'utilisateur $username a les permissions" -ForegroundColor Red
    exit 1
}

# Exécution de la migration
Write-Host "" -ForegroundColor White
Write-Host "🚀 Exécution de la migration..." -ForegroundColor Green
$migrationCmd = "& '$mysqlExe' -h $server -P $port -u $username -p$password $database < '$sqlFile'"

try {
    Invoke-Expression $migrationCmd
    Write-Host "" -ForegroundColor White
    Write-Host "✅ Migration exécutée avec succès !" -ForegroundColor Green
    
    # Vérification post-migration
    Write-Host "" -ForegroundColor White
    Write-Host "🔍 Vérification post-migration..." -ForegroundColor Yellow
    $checkCmd = "& '$mysqlExe' -h $server -P $port -u $username -p$password -e ""SELECT COUNT(*) as 'Types actifs' FROM SpotTypes WHERE IsActive = 1;"" $database"
    Invoke-Expression $checkCmd
    
    Write-Host "" -ForegroundColor White
    Write-Host "📋 Nouveaux types créés :" -ForegroundColor Green
    $listCmd = "& '$mysqlExe' -h $server -P $port -u $username -p$password -e ""SELECT Name, ColorCode FROM SpotTypes WHERE IsActive = 1 ORDER BY Name;"" $database"
    Invoke-Expression $listCmd
    
    Write-Host "" -ForegroundColor White
    Write-Host "🎉 Migration terminée ! L'application SubExplore peut maintenant utiliser la nouvelle structure hiérarchique." -ForegroundColor Green
}
catch {
    Write-Host "❌ ERREUR lors de l'exécution de la migration :" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "" -ForegroundColor White
Write-Host "=== Fin de la migration ===" -ForegroundColor Green