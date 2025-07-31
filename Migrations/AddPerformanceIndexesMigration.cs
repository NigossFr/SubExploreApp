using Microsoft.EntityFrameworkCore.Migrations;

namespace SubExplore.Data.Migrations
{
    /// <summary>
    /// Migration to add comprehensive performance-optimized database indexes
    /// </summary>
    public partial class AddPerformanceIndexes : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // User table performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_Users_AccountType_Subscription",
                table: "Users",
                columns: new[] { "AccountType", "SubscriptionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt_AccountType",
                table: "Users",
                columns: new[] { "CreatedAt", "AccountType" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExpertiseLevel",
                table: "Users",
                column: "ExpertiseLevel");

            // Spot table comprehensive performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_Spots_Location_Geospatial",
                table: "Spots",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Spots_CreatorId",
                table: "Spots",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_CreatedAt",
                table: "Spots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_DifficultyLevel",
                table: "Spots",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_ValidatedByType_Location",
                table: "Spots",
                columns: new[] { "ValidationStatus", "TypeId", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Spots_Creator_Status_Date",
                table: "Spots",
                columns: new[] { "CreatorId", "ValidationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Spots_Name_Search",
                table: "Spots",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_Depth_Difficulty",
                table: "Spots",
                columns: new[] { "MaxDepth", "DifficultyLevel" });

            // SpotMedia table indexes
            migrationBuilder.CreateIndex(
                name: "IX_SpotMedia_SpotId",
                table: "SpotMedia",
                column: "SpotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotMedia_Spot_Type",
                table: "SpotMedia",
                columns: new[] { "SpotId", "MediaType" });

            migrationBuilder.CreateIndex(
                name: "IX_SpotMedia_CreatedAt",
                table: "SpotMedia",
                column: "CreatedAt");

            // SpotType table indexes
            migrationBuilder.CreateIndex(
                name: "IX_SpotTypes_IsActive",
                table: "SpotTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SpotTypes_Category",
                table: "SpotTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SpotTypes_Active_Category",
                table: "SpotTypes",
                columns: new[] { "IsActive", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SpotTypes_RequiresValidation",
                table: "SpotTypes",
                column: "RequiresExpertValidation");

            // UserPreferences table indexes
            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_Language",
                table: "UserPreferences",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_Theme",
                table: "UserPreferences",
                column: "Theme");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all performance indexes in reverse order
            migrationBuilder.DropIndex(
                name: "IX_UserPreferences_Theme",
                table: "UserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_UserPreferences_Language",
                table: "UserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_SpotTypes_RequiresValidation",
                table: "SpotTypes");

            migrationBuilder.DropIndex(
                name: "IX_SpotTypes_Active_Category",
                table: "SpotTypes");

            migrationBuilder.DropIndex(
                name: "IX_SpotTypes_Category",
                table: "SpotTypes");

            migrationBuilder.DropIndex(
                name: "IX_SpotTypes_IsActive",
                table: "SpotTypes");

            migrationBuilder.DropIndex(
                name: "IX_SpotMedia_CreatedAt",
                table: "SpotMedia");

            migrationBuilder.DropIndex(
                name: "IX_SpotMedia_Spot_Type",
                table: "SpotMedia");

            migrationBuilder.DropIndex(
                name: "IX_SpotMedia_SpotId",
                table: "SpotMedia");

            migrationBuilder.DropIndex(
                name: "IX_Spots_Depth_Difficulty",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_Name_Search",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_Creator_Status_Date",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_ValidatedByType_Location",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_DifficultyLevel",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_CreatedAt",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_CreatorId",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Spots_Location_Geospatial",
                table: "Spots");

            migrationBuilder.DropIndex(
                name: "IX_Users_ExpertiseLevel",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAt_AccountType",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AccountType_Subscription",
                table: "Users");
        }
    }
}