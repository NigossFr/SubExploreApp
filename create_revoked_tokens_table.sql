-- SubExplore Database Migration: Add RevokedTokens Table
-- This SQL script creates the RevokedTokens table manually
-- Run this if EF Core migrations are not working

USE subexplore_dev;

-- Create RevokedTokens table
CREATE TABLE IF NOT EXISTS `RevokedTokens` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TokenHash` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `TokenType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `UserId` int NULL,
    `RevokedAt` datetime(6) NOT NULL,
    `ExpiresAt` datetime(6) NULL,
    `RevocationReason` varchar(200) CHARACTER SET utf8mb4 NULL,
    `RevocationIpAddress` varchar(45) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_RevokedTokens` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RevokedTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

-- Create indexes for performance
CREATE UNIQUE INDEX `IX_RevokedTokens_TokenHash` ON `RevokedTokens` (`TokenHash`);
CREATE INDEX `IX_RevokedTokens_UserId` ON `RevokedTokens` (`UserId`);
CREATE INDEX `IX_RevokedTokens_RevokedAt` ON `RevokedTokens` (`RevokedAt`);
CREATE INDEX `IX_RevokedTokens_ExpiresAt` ON `RevokedTokens` (`ExpiresAt`);

-- Insert migration record into __EFMigrationsHistory
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250719120200_AddRevokedTokensTable', '8.0.0');

-- Verify table creation
SELECT 'RevokedTokens table created successfully' as Status;
DESCRIBE RevokedTokens;