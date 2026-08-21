using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrewMemberStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrewMemberStatistics",
                columns: table => new
                {
                    CrewId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Wins = table.Column<long>(type: "bigint", nullable: false),
                    Losses = table.Column<long>(type: "bigint", nullable: false),
                    PointsScored = table.Column<long>(type: "bigint", nullable: false),
                    PointsConceded = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewMemberStatistics");
        }
    }
}
