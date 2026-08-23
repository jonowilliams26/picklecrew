using PickleCrew.Api.Data;
using PickleCrew.IntegrationTests.Testing;

namespace PickleCrew.IntegrationTests.Statistics;

internal static class StatisticsTestData
{
    public static CrewMemberStatistics CreateStatistics(
        BuiltCrewTestData context,
        long crewId,
        string userId,
        long wins,
        long losses,
        long pointsScored,
        long pointsConceded)
    {
        return new CrewMemberStatistics
        {
            CrewId = crewId,
            CrewUuid = context.Uuid,
            UserId = userId,
            Wins = wins,
            Losses = losses,
            PointsScored = pointsScored,
            PointsConceded = pointsConceded,
        };
    }

    public static Game CreateSinglesGame(
        BuiltCrewTestData crew,
        string firstUserId,
        string? secondUserId,
        int firstScore,
        int secondScore,
        DateTimeOffset playedAt,
        string recorderUserId,
        string firstCapturedName = "Captured First",
        string secondCapturedName = "Captured Second"
    )
    {
        return new Game
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            RecorderUserId = recorderUserId,
            Format = GameFormat.Singles,
            PlayedAtUtc = playedAt,
            Sides = [
                new GameSide
                {
                    SideNumber = 1,
                    Score = firstScore,
                    Participants = [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = firstUserId,
                            MemberDisplayName = firstCapturedName,
                        },
                    ],
                },
                new GameSide
                {
                    SideNumber = 2,
                    Score = secondScore,
                    Participants = secondUserId is null
                        ? [
                            new GameParticipant
                            {
                                SlotNumber = 1,
                                GuestName = secondCapturedName,
                            },
                        ]
                        : [
                            new GameParticipant
                            {
                                SlotNumber = 1,
                                UserId = secondUserId,
                                MemberDisplayName = secondCapturedName,
                            },
                        ],
                },
            ],
        };
    }
}
