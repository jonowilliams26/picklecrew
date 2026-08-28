using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddThatsAPaddlin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ThatsAPaddlin",
                table: "CrewMemberStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(
                """
                WITH "Paddlins" AS (
                    SELECT
                        game."CrewId",
                        participant->>'UserId' AS "UserId",
                        COUNT(*) AS "Count"
                    FROM "Games" AS game
                    CROSS JOIN LATERAL jsonb_array_elements(game."Sides") AS winning_side
                    CROSS JOIN LATERAL jsonb_array_elements(winning_side->'Participants') AS participant
                    WHERE (winning_side->>'Score')::integer = 11
                        AND NULLIF(participant->>'UserId', '') IS NOT NULL
                        AND EXISTS (
                            SELECT 1
                            FROM jsonb_array_elements(game."Sides") AS losing_side
                            WHERE (losing_side->>'Score')::integer = 0
                        )
                    GROUP BY game."CrewId", participant->>'UserId'
                )
                UPDATE "CrewMemberStatistics" AS statistics
                SET "ThatsAPaddlin" = paddlins."Count"
                FROM "Paddlins" AS paddlins
                WHERE statistics."CrewId" = paddlins."CrewId"
                    AND statistics."UserId" = paddlins."UserId";
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThatsAPaddlin",
                table: "CrewMemberStatistics");
        }
    }
}
