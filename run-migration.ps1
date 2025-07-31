# SubExplore Database Migration Script
# This PowerShell script applies the role hierarchy migration using .NET MySqlConnector

param(
    [string]$ConnectionString = "server=localhost;port=3306;database=subexplore_dev;user=subexplore_user;password=seb09081980;CharSet=utf8mb4;"
)

Write-Host "🔧 SubExplore Database Migration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

try {
    # Load required assemblies
    Add-Type -Path "C:\Users\nigos\.nuget\packages\mysqlconnector\2.4.0\lib\net8.0\MySqlConnector.dll"

    Write-Host "📡 Connecting to database..." -ForegroundColor Yellow
    $connection = New-Object MySqlConnector.MySqlConnection($ConnectionString)
    $connection.Open()
    Write-Host "✅ Database connection successful" -ForegroundColor Green

    # Check if migration is needed
    Write-Host "🔍 Checking existing schema..." -ForegroundColor Yellow
    $checkSql = @"
        SELECT COUNT(*) 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = 'subexplore_dev' 
        AND TABLE_NAME = 'Users' 
        AND COLUMN_NAME = 'ModeratorSince'
"@

    $checkCommand = New-Object MySqlConnector.MySqlCommand($checkSql, $connection)
    $columnExists = [int]$checkCommand.ExecuteScalar() -gt 0

    if ($columnExists) {
        Write-Host "✅ Role hierarchy columns already exist - migration previously applied" -ForegroundColor Green
    } else {
        Write-Host "⚠️  ModeratorSince column not found - applying migration..." -ForegroundColor Yellow
        
        # Apply migration
        $migrationSql = @"
            -- Add role hierarchy columns
            ALTER TABLE Users 
            ADD COLUMN ModeratorSpecialization INT NOT NULL DEFAULT 0,
            ADD COLUMN ModeratorStatus INT NOT NULL DEFAULT 0,
            ADD COLUMN Permissions INT NOT NULL DEFAULT 1,
            ADD COLUMN ModeratorSince DATETIME(6) NULL,
            ADD COLUMN OrganizationId INT NULL;

            -- Update existing users
            UPDATE Users SET Permissions = 1 WHERE Permissions = 0;

            -- Create indexes
            CREATE INDEX IX_Users_OrganizationId ON Users (OrganizationId);
            CREATE INDEX IX_Users_ModeratorSpecialization_ModeratorStatus ON Users (ModeratorSpecialization, ModeratorStatus);
            CREATE INDEX IX_Users_Permissions ON Users (Permissions);
"@

        $migrationCommand = New-Object MySqlConnector.MySqlCommand($migrationSql, $connection)
        $result = $migrationCommand.ExecuteNonQuery()
        Write-Host "✅ Migration applied successfully" -ForegroundColor Green
    }

    # Verify admin user
    Write-Host "🔍 Verifying admin user..." -ForegroundColor Yellow
    $adminSql = "SELECT Id, Email, AccountType, Permissions FROM Users WHERE Email = 'admin@subexplore.com'"
    $adminCommand = New-Object MySqlConnector.MySqlCommand($adminSql, $connection)
    $reader = $adminCommand.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "✅ Admin user found:" -ForegroundColor Green
        Write-Host "   - ID: $($reader['Id'])" -ForegroundColor White
        Write-Host "   - Email: $($reader['Email'])" -ForegroundColor White
        Write-Host "   - Account Type: $($reader['AccountType'])" -ForegroundColor White
        Write-Host "   - Permissions: $($reader['Permissions'])" -ForegroundColor White
    } else {
        Write-Host "⚠️  Admin user not found in database" -ForegroundColor Red
    }
    $reader.Close()

    $connection.Close()
    Write-Host ""
    Write-Host "🎉 Migration process completed successfully!" -ForegroundColor Green
    Write-Host "The admin login issue should now be resolved." -ForegroundColor Green

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "   Inner: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
    exit 1
}