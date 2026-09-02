using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticsQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerStatistics_CrewId",
                table: "PlayerStatistics");

            migrationBuilder.DropIndex(
                name: "IX_MatchupStatistics_CrewId",
                table: "MatchupStatistics");

            migrationBuilder.DropIndex(
                name: "IX_DoublesPairingStatistics_CrewId",
                table: "DoublesPairingStatistics");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_CrewId_UserId",
                table: "PlayerStatistics",
                columns: new[] { "CrewId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchupStatistics_CrewId_UserId_OpponentUserId",
                table: "MatchupStatistics",
                columns: new[] { "CrewId", "UserId", "OpponentUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DoublesPairingStatistics_CrewId_Player1Id_Player2Id",
                table: "DoublesPairingStatistics",
                columns: new[] { "CrewId", "Player1Id", "Player2Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DoublesPairingStatistics_CrewId_Player2Id_Player1Id",
                table: "DoublesPairingStatistics",
                columns: new[] { "CrewId", "Player2Id", "Player1Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerStatistics_CrewId_UserId",
                table: "PlayerStatistics");

            migrationBuilder.DropIndex(
                name: "IX_MatchupStatistics_CrewId_UserId_OpponentUserId",
                table: "MatchupStatistics");

            migrationBuilder.DropIndex(
                name: "IX_DoublesPairingStatistics_CrewId_Player1Id_Player2Id",
                table: "DoublesPairingStatistics");

            migrationBuilder.DropIndex(
                name: "IX_DoublesPairingStatistics_CrewId_Player2Id_Player1Id",
                table: "DoublesPairingStatistics");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_CrewId",
                table: "PlayerStatistics",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchupStatistics_CrewId",
                table: "MatchupStatistics",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_DoublesPairingStatistics_CrewId",
                table: "DoublesPairingStatistics",
                column: "CrewId");
        }
    }
}
