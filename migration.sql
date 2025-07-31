-- Role Hierarchy Migration for SubExplore
-- This SQL script adds the necessary columns for the user role hierarchy system

USE subexplore_dev;

-- Check if migration is needed
SELECT COUNT(*) as ModeratorSinceExists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'subexplore_dev' 
AND TABLE_NAME = 'Users' 
AND COLUMN_NAME = 'ModeratorSince';

-- Add role hierarchy columns if they don't exist
-- Note: Run this only if the above query returns 0

-- Add moderator specialization column
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS ModeratorSpecialization INT NOT NULL DEFAULT 0;

-- Add moderator status column
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS ModeratorStatus INT NOT NULL DEFAULT 0;

-- Add permissions flags column
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS Permissions INT NOT NULL DEFAULT 1;

-- Add moderator since date column
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS ModeratorSince DATETIME(6) NULL;

-- Add organization ID column for professional users
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS OrganizationId INT NULL;

-- Update existing users to have CreateSpots permission (value 1)
UPDATE Users SET Permissions = 1 WHERE Permissions = 0;

-- Create indexes for performance optimization
CREATE INDEX IF NOT EXISTS IX_Users_OrganizationId ON Users (OrganizationId);
CREATE INDEX IF NOT EXISTS IX_Users_ModeratorSpecialization_ModeratorStatus ON Users (ModeratorSpecialization, ModeratorStatus);
CREATE INDEX IF NOT EXISTS IX_Users_Permissions ON Users (Permissions);

-- Verify the admin user exists and show its current state
SELECT 
    Id,
    Email,
    Username,
    AccountType,
    Permissions,
    ModeratorSpecialization,
    ModeratorStatus,
    ModeratorSince,
    OrganizationId,
    CreatedAt
FROM Users 
WHERE Email = 'admin@subexplore.com';

-- Show summary of migration
SELECT 
    'Migration completed successfully' as Status,
    COUNT(*) as TotalUsers,
    SUM(CASE WHEN AccountType = 3 THEN 1 ELSE 0 END) as AdminUsers,
    SUM(CASE WHEN Permissions > 0 THEN 1 ELSE 0 END) as UsersWithPermissions
FROM Users;