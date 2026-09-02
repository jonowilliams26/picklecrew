using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameStatisticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewDoublesPairingGameStatistics_Crews_CrewId",
                table: "CrewDoublesPairingGameStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewDoublesPairingGameStatistics_Games_GameId",
                table: "CrewDoublesPairingGameStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewMatchupGameStatistics_Crews_CrewId",
                table: "CrewMatchupGameStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewMatchupGameStatistics_Games_GameId",
                table: "CrewMatchupGameStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewMemberGameStatistics_Crews_CrewId",
                table: "CrewMemberGameStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewMemberGameStatistics_Games_GameId",
                table: "CrewMemberGameStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewMemberWinStreaks_Crews_CrewId",
                table: "CrewMemberWinStreaks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewMemberWinStreaks",
                table: "CrewMemberWinStreaks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewMemberGameStatistics",
                table: "CrewMemberGameStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewMatchupGameStatistics",
                table: "CrewMatchupGameStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewDoublesPairingGameStatistics",
                table: "CrewDoublesPairingGameStatistics");

            migrationBuilder.RenameTable(
                name: "CrewMemberWinStreaks",
                newName: "PlayerWinStreaks");

            migrationBuilder.RenameTable(
                name: "CrewMemberGameStatistics",
                newName: "PlayerStatistics");

            migrationBuilder.RenameTable(
                name: "CrewMatchupGameStatistics",
                newName: "MatchupStatistics");

            migrationBuilder.RenameTable(
                name: "CrewDoublesPairingGameStatistics",
                newName: "DoublesPairingStatistics");

            migrationBuilder.RenameIndex(
                name: "IX_CrewMemberGameStatistics_CrewId",
                table: "PlayerStatistics",
                newName: "IX_PlayerStatistics_CrewId");

            migrationBuilder.RenameIndex(
                name: "IX_CrewMatchupGameStatistics_CrewId",
                table: "MatchupStatistics",
                newName: "IX_MatchupStatistics_CrewId");

            migrationBuilder.RenameIndex(
                name: "IX_CrewDoublesPairingGameStatistics_CrewId",
                table: "DoublesPairingStatistics",
                newName: "IX_DoublesPairingStatistics_CrewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerWinStreaks",
                table: "PlayerWinStreaks",
                columns: new[] { "CrewId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerStatistics",
                table: "PlayerStatistics",
                columns: new[] { "GameId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_MatchupStatistics",
                table: "MatchupStatistics",
                columns: new[] { "GameId", "UserId", "OpponentUserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DoublesPairingStatistics",
                table: "DoublesPairingStatistics",
                columns: new[] { "GameId", "Player1Id", "Player2Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_DoublesPairingStatistics_Crews_CrewId",
                table: "DoublesPairingStatistics",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DoublesPairingStatistics_Games_GameId",
                table: "DoublesPairingStatistics",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchupStatistics_Crews_CrewId",
                table: "MatchupStatistics",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchupStatistics_Games_GameId",
                table: "MatchupStatistics",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStatistics_Crews_CrewId",
                table: "PlayerStatistics",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStatistics_Games_GameId",
                table: "PlayerStatistics",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerWinStreaks_Crews_CrewId",
                table: "PlayerWinStreaks",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoublesPairingStatistics_Crews_CrewId",
                table: "DoublesPairingStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_DoublesPairingStatistics_Games_GameId",
                table: "DoublesPairingStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchupStatistics_Crews_CrewId",
                table: "MatchupStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchupStatistics_Games_GameId",
                table: "MatchupStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStatistics_Crews_CrewId",
                table: "PlayerStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStatistics_Games_GameId",
                table: "PlayerStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerWinStreaks_Crews_CrewId",
                table: "PlayerWinStreaks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerWinStreaks",
                table: "PlayerWinStreaks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerStatistics",
                table: "PlayerStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MatchupStatistics",
                table: "MatchupStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DoublesPairingStatistics",
                table: "DoublesPairingStatistics");

            migrationBuilder.RenameTable(
                name: "PlayerWinStreaks",
                newName: "CrewMemberWinStreaks");

            migrationBuilder.RenameTable(
                name: "PlayerStatistics",
                newName: "CrewMemberGameStatistics");

            migrationBuilder.RenameTable(
                name: "MatchupStatistics",
                newName: "CrewMatchupGameStatistics");

            migrationBuilder.RenameTable(
                name: "DoublesPairingStatistics",
                newName: "CrewDoublesPairingGameStatistics");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStatistics_CrewId",
                table: "CrewMemberGameStatistics",
                newName: "IX_CrewMemberGameStatistics_CrewId");

            migrationBuilder.RenameIndex(
                name: "IX_MatchupStatistics_CrewId",
                table: "CrewMatchupGameStatistics",
                newName: "IX_CrewMatchupGameStatistics_CrewId");

            migrationBuilder.RenameIndex(
                name: "IX_DoublesPairingStatistics_CrewId",
                table: "CrewDoublesPairingGameStatistics",
                newName: "IX_CrewDoublesPairingGameStatistics_CrewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewMemberWinStreaks",
                table: "CrewMemberWinStreaks",
                columns: new[] { "CrewId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewMemberGameStatistics",
                table: "CrewMemberGameStatistics",
                columns: new[] { "GameId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewMatchupGameStatistics",
                table: "CrewMatchupGameStatistics",
                columns: new[] { "GameId", "UserId", "OpponentUserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewDoublesPairingGameStatistics",
                table: "CrewDoublesPairingGameStatistics",
                columns: new[] { "GameId", "Player1Id", "Player2Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_CrewDoublesPairingGameStatistics_Crews_CrewId",
                table: "CrewDoublesPairingGameStatistics",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewDoublesPairingGameStatistics_Games_GameId",
                table: "CrewDoublesPairingGameStatistics",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewMatchupGameStatistics_Crews_CrewId",
                table: "CrewMatchupGameStatistics",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewMatchupGameStatistics_Games_GameId",
                table: "CrewMatchupGameStatistics",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewMemberGameStatistics_Crews_CrewId",
                table: "CrewMemberGameStatistics",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewMemberGameStatistics_Games_GameId",
                table: "CrewMemberGameStatistics",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewMemberWinStreaks_Crews_CrewId",
                table: "CrewMemberWinStreaks",
                column: "CrewId",
                principalTable: "Crews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
