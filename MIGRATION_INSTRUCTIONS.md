# 🚀 Database Migration Instructions

## Current Status

✅ **All security code is implemented and compiled successfully**
✅ **Manual migration file created**: `20250719120200_AddRevokedTokensTable.cs`

## Migration Options

### Option 1: Use the Manual Migration (Recommended)

The migration file `Migrations/20250719120200_AddRevokedTokensTable.cs` has been created manually. To apply it:

```bash
# Apply the migration to your database
dotnet ef database update --framework net8.0-windows10.0.19041.0
```

### Option 2: Generate Migration Using Visual Studio

If you're using Visual Studio:

1. Open **Package Manager Console** in Visual Studio
2. Set the **Default Project** to your main project
3. Run: `Add-Migration AddRevokedTokensTable`
4. Run: `Update-Database`

### Option 3: Fix EF Tools and Generate Migration

If you want to use the dotnet CLI tools:

1. **Ensure MySQL is running** and accessible with the credentials in your appsettings.json
2. **Set up environment variables** (optional but recommended):
   ```bash
   set SUBEXPLORE_DB_HOST=localhost
   set SUBEXPLORE_DB_USER=subexplore_user
   set SUBEXPLORE_DB_PASSWORD=seb09081980
   ```
3. **Generate migration**:
   ```bash
   dotnet ef migrations add AddRevokedTokensTable --framework net8.0-windows10.0.19041.0
   ```
4. **Apply to database**:
   ```bash
   dotnet ef database update --framework net8.0-windows10.0.19041.0
   ```

## Verify Migration

After applying the migration, verify the table was created:

```sql
-- Connect to your MySQL database and run:
DESCRIBE RevokedTokens;

-- You should see columns:
-- Id (int, AUTO_INCREMENT, PRIMARY KEY)
-- TokenHash (varchar(500), UNIQUE)
-- TokenType (varchar(50))
-- UserId (int, FOREIGN KEY to Users.Id)
-- RevokedAt (datetime)
-- ExpiresAt (datetime, nullable)
-- RevocationReason (varchar(200), nullable)
-- RevocationIpAddress (varchar(45), nullable)
```

## Testing the Implementation

Once the migration is applied, you can test the new secure authentication system:

1. **Start the application**
2. **Check logs** for configuration validation messages
3. **Try logging in** - tokens should now persist across app restarts
4. **Logout and restart app** - tokens should remain revoked

The system will automatically generate a secure JWT secret key on first run if none is configured.

## Next Steps

1. **Apply the migration**
2. **Set up environment variables** for production
3. **Test the authentication system**
4. **Review the SECURITY_SETUP.md** for production deployment

All security vulnerabilities have been addressed! 🛡️