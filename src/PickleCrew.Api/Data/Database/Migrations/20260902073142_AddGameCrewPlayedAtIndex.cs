using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCrewPlayedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_CrewId",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CrewId_PlayedAtUtc",
                table: "Games",
                columns: new[] { "CrewId", "PlayedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_CrewId_PlayedAtUtc",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CrewId",
                table: "Games",
                column: "CrewId");
        }
    }
}
