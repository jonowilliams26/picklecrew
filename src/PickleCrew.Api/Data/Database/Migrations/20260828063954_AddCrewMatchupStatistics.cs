using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrewMatchupStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrewMatchupStatistics",
                columns: table => new
                {
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OpponentUserId = table.Column<string>(type: "text", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Wins = table.Column<long>(type: "bigint", nullable: false),
                    Losses = table.Column<long>(type: "bigint", nullable: false),
                    PointsScored = table.Column<long>(type: "bigint", nullable: false),
                    PointsConceded = table.Column<long>(type: "bigint", nullable: false),
                    GamesPlayed = table.Column<long>(type: "bigint", nullable: false),
                    WinPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    PointDifferential = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewMatchupStatistics");
        }
    }
}
