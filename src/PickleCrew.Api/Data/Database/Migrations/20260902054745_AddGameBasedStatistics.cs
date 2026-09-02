using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGameBasedStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrewDoublesPairingGameStatistics",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    Player1Id = table.Column<string>(type: "text", nullable: false),
                    Player2Id = table.Column<string>(type: "text", nullable: false),
                    GameUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    PointsScored = table.Column<int>(type: "integer", nullable: false),
                    PointsConceded = table.Column<int>(type: "integer", nullable: false),
                    PointDifferential = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewDoublesPairingGameStatistics", x => new { x.GameId, x.Player1Id, x.Player2Id });
                    table.ForeignKey(
                        name: "FK_CrewDoublesPairingGameStatistics_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewDoublesPairingGameStatistics_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewMatchupGameStatistics",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OpponentUserId = table.Column<string>(type: "text", nullable: false),
                    GameUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    PointsScored = table.Column<int>(type: "integer", nullable: false),
                    PointsConceded = table.Column<int>(type: "integer", nullable: false),
                    PointDifferential = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMatchupGameStatistics", x => new { x.GameId, x.UserId, x.OpponentUserId });
                    table.ForeignKey(
                        name: "FK_CrewMatchupGameStatistics_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewMatchupGameStatistics_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewMemberGameStatistics",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GameUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    PointsScored = table.Column<int>(type: "integer", nullable: false),
                    PointsConceded = table.Column<int>(type: "integer", nullable: false),
                    PointDifferential = table.Column<int>(type: "integer", nullable: false),
                    CrewScore = table.Column<int>(type: "integer", nullable: false),
                    ThatsAPaddlin = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMemberGameStatistics", x => new { x.GameId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CrewMemberGameStatistics_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewMemberGameStatistics_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewMemberWinStreaks",
                columns: table => new
                {
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentWinStreak = table.Column<int>(type: "integer", nullable: false),
                    CurrentWinStreakStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LongestWinStreak = table.Column<int>(type: "integer", nullable: false),
                    LongestWinStreakStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LongestWinStreakEndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMemberWinStreaks", x => new { x.CrewId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CrewMemberWinStreaks_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrewDoublesPairingGameStatistics_CrewId",
                table: "CrewDoublesPairingGameStatistics",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_CrewMatchupGameStatistics_CrewId",
                table: "CrewMatchupGameStatistics",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_CrewMemberGameStatistics_CrewId",
                table: "CrewMemberGameStatistics",
                column: "CrewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewDoublesPairingGameStatistics");

            migrationBuilder.DropTable(
                name: "CrewMatchupGameStatistics");

            migrationBuilder.DropTable(
                name: "CrewMemberGameStatistics");

            migrationBuilder.DropTable(
                name: "CrewMemberWinStreaks");
        }
    }
}
