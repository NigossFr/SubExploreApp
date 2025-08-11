# Script simple pour executer la migration MySQL
Write-Host "Migration des types de spots SubExplore" -ForegroundColor Green

# Parametres de connexion
$server = "localhost"  
$port = 3306
$database = "subexplore_dev"
$username = "subexplore_user"
$password = "seb09081980"
$sqlFile = "migrate_spot_types.sql"

# Chercher mysql.exe
$mysqlPaths = @(
    "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
    "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe", 
    "C:\xampp\mysql\bin\mysql.exe",
    "mysql.exe"
)

$mysqlExe = $null
foreach ($path in $mysqlPaths) {
    if (Test-Path $path) {
        $mysqlExe = $path
        Write-Host "MySQL trouve: $mysqlExe" -ForegroundColor Yellow
        break
    }
}

if ($mysqlExe -eq $null) {
    try {
        $mysqlExe = (Get-Command mysql -ErrorAction Stop).Source
        Write-Host "MySQL trouve dans PATH: $mysqlExe" -ForegroundColor Yellow
    }
    catch {
        Write-Host "ERREUR: MySQL non trouve" -ForegroundColor Red
        exit 1
    }
}

# Test de connexion rapide
Write-Host "Test de connexion..." -ForegroundColor Yellow
$connectionString = "-h $server -P $port -u $username -p$password"

# Execution de la migration
Write-Host "Execution de la migration..." -ForegroundColor Green
$arguments = "$connectionString $database"

try {
    Start-Process -FilePath $mysqlExe -ArgumentList $arguments -RedirectStandardInput $sqlFile -Wait -WindowStyle Hidden
    Write-Host "Migration executee!" -ForegroundColor Green
    
    # Verification
    $checkArgs = "$connectionString -e `"SELECT COUNT(*) as TypesActifs FROM SpotTypes WHERE IsActive = 1;`" $database"
    Start-Process -FilePath $mysqlExe -ArgumentList $checkArgs -Wait
    
} catch {
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Terminé!" -ForegroundColor Green