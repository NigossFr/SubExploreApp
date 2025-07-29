-- Create UserFavoriteSpots table based on AddUserFavoriteSpotMigration.cs
-- This script creates the table and indexes for the favorites system

USE subexplore_dev;

-- Create UserFavoriteSpots table
CREATE TABLE IF NOT EXISTS `UserFavoriteSpots` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `SpotId` int NOT NULL,
    `Priority` int NOT NULL DEFAULT 5,
    `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `NotificationEnabled` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_UserFavoriteSpots` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_UserFavoriteSpots_Spots_SpotId` FOREIGN KEY (`SpotId`) REFERENCES `Spots` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserFavoriteSpots_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- Create unique constraint to prevent duplicate favorites
CREATE UNIQUE INDEX `IX_UserFavoriteSpots_User_Spot_Unique` ON `UserFavoriteSpots` (`UserId`, `SpotId`);

-- Create performance index for user favorite queries
CREATE INDEX `IX_UserFavoriteSpots_UserId_CreatedAt` ON `UserFavoriteSpots` (`UserId`, `CreatedAt`);

-- Create performance index for priority-based ordering
CREATE INDEX `IX_UserFavoriteSpots_UserId_Priority_CreatedAt` ON `UserFavoriteSpots` (`UserId`, `Priority`, `CreatedAt`);

-- Create index for notification queries
CREATE INDEX `IX_UserFavoriteSpots_UserId_NotificationEnabled` ON `UserFavoriteSpots` (`UserId`, `NotificationEnabled`);

-- Create index for spot favorites count queries
CREATE INDEX `IX_UserFavoriteSpots_SpotId` ON `UserFavoriteSpots` (`SpotId`);

-- Verify table creation
SELECT 'UserFavoriteSpots table created successfully' as result;