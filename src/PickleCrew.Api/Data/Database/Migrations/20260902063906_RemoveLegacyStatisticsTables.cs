using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyStatisticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewDoublesPairingStatistics");

            migrationBuilder.DropTable(
                name: "CrewMatchupStatistics");

            migrationBuilder.DropTable(
                name: "CrewMemberStatistics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrewDoublesPairingStatistics",
                columns: table => new
                {
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    Player1Id = table.Column<string>(type: "text", nullable: false),
                    Player2Id = table.Column<string>(type: "text", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    GamesPlayed = table.Column<long>(type: "bigint", nullable: false),
                    Losses = table.Column<long>(type: "bigint", nullable: false),
                    PointDifferential = table.Column<long>(type: "bigint", nullable: false),
                    PointsConceded = table.Column<long>(type: "bigint", nullable: false),
                    PointsScored = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    WinPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Wins = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewDoublesPairingStatistics", x => new { x.CrewId, x.Player1Id, x.Player2Id });
                    table.ForeignKey(
                        name: "FK_CrewDoublesPairingStatistics_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewMatchupStatistics",
                columns: table => new
                {
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OpponentUserId = table.Column<string>(type: "text", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    GamesPlayed = table.Column<long>(type: "bigint", nullable: false),
                    Losses = table.Column<long>(type: "bigint", nullable: false),
                    PointDifferential = table.Column<long>(type: "bigint", nullable: false),
                    PointsConceded = table.Column<long>(type: "bigint", nullable: false),
                    PointsScored = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    WinPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Wins = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMatchupStatistics", x => new { x.CrewId, x.UserId, x.OpponentUserId });
                    table.ForeignKey(
                        name: "FK_CrewMatchupStatistics_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewMemberStatistics",
                columns: table => new
                {
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CrewScore = table.Column<long>(type: "bigint", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentWinStreak = table.Column<long>(type: "bigint", nullable: false),
                    CurrentWinStreakStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GamesPlayed = table.Column<long>(type: "bigint", nullable: false),
                    LongestWinStreak = table.Column<long>(type: "bigint", nullable: false),
                    LongestWinStreakEndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LongestWinStreakStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Losses = table.Column<long>(type: "bigint", nullable: false),
                    PointDifferential = table.Column<long>(type: "bigint", nullable: false),
                    PointsConceded = table.Column<long>(type: "bigint", nullable: false),
                    PointsScored = table.Column<long>(type: "bigint", nullable: false),
                    ThatsAPaddlin = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    WinPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Wins = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewMemberStatistics", x => new { x.CrewId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CrewMemberStatistics_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
