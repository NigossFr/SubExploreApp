using Microsoft.EntityFrameworkCore.Migrations;

namespace SubExplore.Migrations
{
    /// <summary>
    /// Migration to add UserFavoriteSpot table for favorite spot system
    /// </summary>
    public partial class AddUserFavoriteSpot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserFavoriteSpots table
            migrationBuilder.CreateTable(
                name: "UserFavoriteSpots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SpotId = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    NotificationEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteSpots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavoriteSpots_Spots_SpotId",
                        column: x => x.SpotId,
                        principalTable: "Spots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoriteSpots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create unique constraint to prevent duplicate favorites
            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteSpots_User_Spot_Unique",
                table: "UserFavoriteSpots",
                columns: new[] { "UserId", "SpotId" },
                unique: true);

            // Create performance index for user favorite queries
            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteSpots_UserId_CreatedAt",
                table: "UserFavoriteSpots",
                columns: new[] { "UserId", "CreatedAt" });

            // Create performance index for priority-based ordering
            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteSpots_UserId_Priority_CreatedAt",
                table: "UserFavoriteSpots",
                columns: new[] { "UserId", "Priority", "CreatedAt" });

            // Create index for notification queries
            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteSpots_UserId_NotificationEnabled",
                table: "UserFavoriteSpots",
                columns: new[] { "UserId", "NotificationEnabled" });

            // Create index for spot favorites count queries
            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteSpots_SpotId",
                table: "UserFavoriteSpots",
                column: "SpotId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes first
            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteSpots_SpotId",
                table: "UserFavoriteSpots");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteSpots_UserId_NotificationEnabled",
                table: "UserFavoriteSpots");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteSpots_UserId_Priority_CreatedAt",
                table: "UserFavoriteSpots");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteSpots_UserId_CreatedAt",
                table: "UserFavoriteSpots");

            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteSpots_User_Spot_Unique",
                table: "UserFavoriteSpots");

            // Drop the table
            migrationBuilder.DropTable(
                name: "UserFavoriteSpots");
        }
    }
}