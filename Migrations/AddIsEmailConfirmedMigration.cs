using Microsoft.EntityFrameworkCore.Migrations;

namespace SubExplore.Migrations
{
    /// <summary>
    /// Migration to add IsEmailConfirmed column to Users table
    /// This column was added to the User model but was missing from the database schema
    /// </summary>
    public partial class AddIsEmailConfirmedMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add IsEmailConfirmed column to Users table
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Set existing admin user as email confirmed
            migrationBuilder.Sql(
                "UPDATE Users SET IsEmailConfirmed = 1 WHERE Email = 'admin@subexplore.com'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove IsEmailConfirmed column
            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "Users");
        }
    }
}