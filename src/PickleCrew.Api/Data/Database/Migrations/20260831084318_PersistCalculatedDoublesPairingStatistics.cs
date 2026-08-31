using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class PersistCalculatedDoublesPairingStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GamesPlayed",
                table: "CrewDoublesPairingStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PointDifferential",
                table: "CrewDoublesPairingStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "WinPercentage",
                table: "CrewDoublesPairingStatistics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "CrewDoublesPairingStatistics"
                SET
                    "GamesPlayed" = "Wins" + "Losses",
                    "WinPercentage" = CASE
                        WHEN "Wins" + "Losses" = 0 THEN 0
                        ELSE "Wins"::numeric / ("Wins" + "Losses")
                    END,
                    "PointDifferential" = "PointsScored" - "PointsConceded";
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "CrewDoublesPairingStatistics");

            migrationBuilder.DropColumn(
                name: "PointDifferential",
                table: "CrewDoublesPairingStatistics");

            migrationBuilder.DropColumn(
                name: "WinPercentage",
                table: "CrewDoublesPairingStatistics");
        }
    }
}
