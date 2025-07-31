using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubExplore.Data.Migrations
{
    /// <summary>
    /// Database migration to add user role hierarchy system
    /// Implements requirements from section 2.1.3 - User account types and hierarchy
    /// </summary>
    public partial class AddUserRoleHierarchy : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add moderator specialization column
            migrationBuilder.AddColumn<int>(
                name: "ModeratorSpecialization",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Add moderator status column
            migrationBuilder.AddColumn<int>(
                name: "ModeratorStatus",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Add permissions flags column
            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1); // Default to CreateSpots permission

            // Add moderator since date column
            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratorSince",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            // Add organization ID column for professional users
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Users",
                type: "int",
                nullable: true);

            // Update existing users to have CreateSpots permission
            migrationBuilder.Sql("UPDATE Users SET Permissions = 1 WHERE Permissions = 0");

            // Create index for organization lookup
            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            // Create index for moderator specialization lookup
            migrationBuilder.CreateIndex(
                name: "IX_Users_ModeratorSpecialization_ModeratorStatus",
                table: "Users",
                columns: new[] { "ModeratorSpecialization", "ModeratorStatus" });

            // Create index for permissions lookup
            migrationBuilder.CreateIndex(
                name: "IX_Users_Permissions",
                table: "Users",
                column: "Permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ModeratorSpecialization_ModeratorStatus",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Permissions",
                table: "Users");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "ModeratorSpecialization",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModeratorStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModeratorSince",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Users");
        }
    }
}