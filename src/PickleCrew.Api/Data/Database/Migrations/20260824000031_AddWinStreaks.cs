using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWinStreaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentWinStreak",
                table: "CrewMemberStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CurrentWinStreakStartedAtUtc",
                table: "CrewMemberStatistics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LongestWinStreak",
                table: "CrewMemberStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LongestWinStreakEndedAtUtc",
                table: "CrewMemberStatistics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LongestWinStreakStartedAtUtc",
                table: "CrewMemberStatistics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH "ParticipantResults" AS (
                    SELECT
                        game."CrewId",
                        participant->>'UserId' AS "UserId",
                        game."PlayedAtUtc",
                        game."Id",
                        (side->>'Score')::integer = game_score."WinningScore" AS "Won"
                    FROM "Games" AS game
                    CROSS JOIN LATERAL jsonb_array_elements(game."Sides") AS side
                    CROSS JOIN LATERAL jsonb_array_elements(side->'Participants') AS participant
                    CROSS JOIN LATERAL (
                        SELECT MAX((candidate_side->>'Score')::integer) AS "WinningScore"
                        FROM jsonb_array_elements(game."Sides") AS candidate_side
                    ) AS game_score
                    WHERE NULLIF(participant->>'UserId', '') IS NOT NULL
                ),
                "GroupedResults" AS (
                    SELECT
                        result.*,
                        COALESCE(
                            SUM(CASE WHEN NOT result."Won" THEN 1 ELSE 0 END) OVER (
                                PARTITION BY result."CrewId", result."UserId"
                                ORDER BY result."PlayedAtUtc", result."Id"
                                ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
                            ),
                            0
                        ) AS "StreakGroup"
                    FROM "ParticipantResults" AS result
                ),
                "Streaks" AS (
                    SELECT
                        result."CrewId",
                        result."UserId",
                        COUNT(*) FILTER (WHERE result."Won") AS "StreakLength",
                        MIN(result."PlayedAtUtc") FILTER (WHERE result."Won") AS "StartedAtUtc",
                        MAX(result."PlayedAtUtc") FILTER (WHERE NOT result."Won") AS "EndedAtUtc"
                    FROM "GroupedResults" AS result
                    GROUP BY result."CrewId", result."UserId", result."StreakGroup"
                    HAVING BOOL_OR(result."Won")
                ),
                "RankedStreaks" AS (
                    SELECT
                        streak.*,
                        ROW_NUMBER() OVER (
                            PARTITION BY streak."CrewId", streak."UserId"
                            ORDER BY streak."StreakLength" DESC, streak."StartedAtUtc"
                        ) AS "StreakRank"
                    FROM "Streaks" AS streak
                ),
                "LongestStreaks" AS (
                    SELECT *
                    FROM "RankedStreaks"
                    WHERE "StreakRank" = 1
                ),
                "CurrentStreaks" AS (
                    SELECT *
                    FROM "Streaks"
                    WHERE "EndedAtUtc" IS NULL
                )
                UPDATE "CrewMemberStatistics" AS statistics
                SET
                    "CurrentWinStreak" = COALESCE(current_streak."StreakLength", 0),
                    "CurrentWinStreakStartedAtUtc" = current_streak."StartedAtUtc",
                    "LongestWinStreak" = longest_streak."StreakLength",
                    "LongestWinStreakStartedAtUtc" = longest_streak."StartedAtUtc",
                    "LongestWinStreakEndedAtUtc" = longest_streak."EndedAtUtc"
                FROM "LongestStreaks" AS longest_streak
                LEFT JOIN "CurrentStreaks" AS current_streak
                    ON current_streak."CrewId" = longest_streak."CrewId"
                    AND current_streak."UserId" = longest_streak."UserId"
                WHERE statistics."CrewId" = longest_streak."CrewId"
                    AND statistics."UserId" = longest_streak."UserId";
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentWinStreak",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "CurrentWinStreakStartedAtUtc",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "LongestWinStreak",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "LongestWinStreakEndedAtUtc",
                table: "CrewMemberStatistics");

            migrationBuilder.DropColumn(
                name: "LongestWinStreakStartedAtUtc",
                table: "CrewMemberStatistics");
        }
    }
}
