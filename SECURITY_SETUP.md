# 🛡️ Security Setup Guide for SubExplore

## Overview

This guide helps you set up the secure JWT configuration and credential management system for SubExplore.

## 🚨 CRITICAL: Before Running the Application

### 1. Environment Variables Setup

1. **Copy the environment template:**
   ```bash
   cp .env.example .env
   ```

2. **Generate a secure JWT secret key:**
   ```bash
   # Option 1: Using OpenSSL (recommended)
   openssl rand -base64 64
   
   # Option 2: Using PowerShell (Windows)
   [System.Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
   ```

3. **Configure your `.env` file:**
   ```env
   SUBEXPLORE_DB_HOST=localhost
   SUBEXPLORE_DB_USER=your_db_user
   SUBEXPLORE_DB_PASSWORD=your_secure_password
   SUBEXPLORE_JWT_SECRET=your_generated_512_bit_secret_key_here
   ```

### 2. Database Migration

Run the following commands to update your database schema:

```bash
# Add migration for RevokedTokens table
dotnet ef migrations add AddRevokedTokensTable

# Update database
dotnet ef database update
```

### 3. Configuration Validation

The application will validate your configuration on startup. Check the logs for any validation errors.

## 🔐 Security Improvements Implemented

### JWT Security
- ✅ **Persistent Secret Keys**: JWT secrets now stored securely, not regenerated on restart
- ✅ **Configurable Expiration**: Access tokens expire in 15 minutes (configurable)
- ✅ **Proper Clock Skew**: 5-minute clock skew allowance for token validation
- ✅ **Environment Override**: JWT settings can be overridden via environment variables

### Token Management
- ✅ **Database-Backed Revocation**: Revoked tokens stored in database, persistent across restarts
- ✅ **Token Hashing**: Tokens stored as SHA256 hashes for security
- ✅ **Audit Trail**: Full audit trail for token revocation with reasons and metadata
- ✅ **Cleanup Mechanism**: Automatic cleanup of expired revoked tokens

### Credential Security
- ✅ **Environment Variables**: Database credentials and secrets via environment variables
- ✅ **No Hardcoded Secrets**: All sensitive data externalized
- ✅ **Secure Configuration Service**: Centralized secure configuration management
- ✅ **Validation**: Comprehensive configuration validation on startup

## 🚀 Production Deployment

### Required Environment Variables
```env
# Database (Required)
SUBEXPLORE_DB_HOST=your-production-db-host
SUBEXPLORE_DB_USER=production_user
SUBEXPLORE_DB_PASSWORD=secure_production_password

# JWT (Required)
SUBEXPLORE_JWT_SECRET=your-512-bit-production-secret

# Optional Overrides
SUBEXPLORE_JWT_ISSUER=SubExplore
SUBEXPLORE_JWT_AUDIENCE=SubExploreApp
```

### Security Checklist
- [ ] JWT secret key is 512-bit (64 characters base64)
- [ ] Database credentials are not in source code
- [ ] All environment variables are set in production
- [ ] Database migration has been applied
- [ ] Application logs show "Configuration validation successful"

## 🔧 Troubleshooting

### Common Issues

1. **"JWT configuration is invalid"**
   - Check that `SUBEXPLORE_JWT_SECRET` is set and at least 32 characters
   - Verify JWT configuration in appsettings.json

2. **"Database connection string is not configured"**
   - Ensure all database environment variables are set
   - Check connection string format

3. **Token validation fails after restart**
   - This issue is now fixed with persistent JWT secrets
   - Tokens will remain valid across application restarts

### Validation Commands

```bash
# Test database connection
dotnet ef database update --dry-run

# Validate configuration (will show in application logs)
dotnet run

# Check JWT secret generation
# The app will generate and store a secure key if none exists
```

## 📊 Monitoring

The application now logs security events:
- JWT secret generation/rotation
- Token revocation events
- Configuration validation results
- Database connection status

Monitor these logs for security issues and configuration problems.

## 🔄 Maintenance

### JWT Secret Rotation (Optional)
```csharp
// Can be called programmatically for security maintenance
var secureConfig = serviceProvider.GetService<ISecureConfigurationService>();
await secureConfig.RotateJwtSecretAsync();
```

### Token Cleanup
```csharp
// Clean up expired revoked tokens (run periodically)
var tokenRepo = serviceProvider.GetService<IRevokedTokenRepository>();
var cleanedCount = await tokenRepo.CleanupExpiredTokensAsync();
```

## ⚠️ Security Notes

1. **Never commit `.env` files** - they contain sensitive credentials
2. **Rotate JWT secrets regularly** in production (quarterly recommended)
3. **Monitor revoked token table size** - implement cleanup schedule
4. **Use HTTPS in production** - never send tokens over unencrypted connections
5. **Implement rate limiting** for authentication endpoints (future enhancement)

## 📝 Migration from Old System

If upgrading from the previous insecure implementation:

1. All existing tokens will be invalidated (users need to re-login)
2. JWT secrets are now persistent - no more session invalidation on restart
3. Token revocation is now permanent and persistent
4. Database schema includes new `RevokedTokens` table

This is expected and improves security significantly.