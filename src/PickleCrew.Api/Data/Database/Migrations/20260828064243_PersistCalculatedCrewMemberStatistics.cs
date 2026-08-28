using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class PersistCalculatedCrewMemberStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CrewScore",
                table: "CrewMemberStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "GamesPlayed",
                table: "CrewMemberStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PointDifferential",
                table: "CrewMemberStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "WinPercentage",
                table: "CrewMemberStatistics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "CrewMemberStatistics"
                SET
                    "GamesPlayed" = "Wins" + "Losses",
                    "WinPercentage" = CASE
                        WHEN "Wins" + "Losses" = 0 THEN 0
                        ELSE "Wins"::numeric / ("Wins" + "Losses")
                    END,
                    "PointDifferential" = "PointsScored" - "PointsConceded",
                    "CrewScore" = ("Wins" * 100)
                        + ("PointsScored" - "PointsConceded");
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrewScore",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "PointDifferential",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "WinPercentage",
                table: "CrewMemberStatistics");
        }
    }
}
