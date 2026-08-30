using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;

namespace PickleCrew.Api.Games.RecordGame;

public sealed record RecordGameHttpRequest
{
    public required GameFormat Format { get; init; }

    public required RecordGameSideHttpRequest[] Sides { get; init; }
}

public sealed record RecordGameSideHttpRequest
{
    public required int Score { get; init; }

    public required RecordGameParticipantHttpRequest[] Participants { get; init; }
}

public sealed record RecordGameParticipantHttpRequest
{
    public string? UserId { get; init; }

    public string? GuestName { get; init; }
}

public sealed record RecordGameHttpResponse
{
    public required Guid GameId { get; init; }

    public required Guid CrewId { get; init; }

    public required GameFormat Format { get; init; }

    public required string RecorderUserId { get; init; }

    public required DateTimeOffset PlayedAtUtc { get; init; }

    public required RecordedGameSideHttpResponse[] Sides { get; init; }
}

public sealed record RecordedGameSideHttpResponse
{
    public required int SideNumber { get; init; }

    public required int Score { get; init; }

    public required RecordedGameParticipantHttpResponse[] Participants { get; init; }
}

public sealed record RecordedGameParticipantHttpResponse
{
    public required int SlotNumber { get; init; }

    public string? UserId { get; init; }

    public string? GuestName { get; init; }
}

internal sealed class RecordGameEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(string.Empty, HandleAsync)
            .WithName("RecordGame")
            .WithSummary("Records a completed game for the active Crew.")
            .WithRequestValidation<RecordGameHttpRequest>(RecordGameValidator.Validate);
    }

    private static async Task<Results<
        Ok<RecordGameHttpResponse>,
        ValidationProblem,
        ForbidHttpResult>> HandleAsync(
        RecordGameHttpRequest request,
        CrewUserContext crewUserContext,
        ICrewService crewService,
        CrewScopedDbContext database,
        GameStatisticsService gameStatisticsService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var activeMembers = await crewService.ListActiveCrewMembersAsync(cancellationToken);

        var activeMemberByUserId = activeMembers.ToDictionary(
            member => member.WorkOsUserId,
            StringComparer.Ordinal
        );

        var activeUserIds = activeMemberByUserId.Keys
            .ToHashSet(StringComparer.Ordinal);

        if (!activeUserIds.Contains(crewUserContext.UserId))
        {
            return TypedResults.Forbid();
        }

        var membershipErrors = RecordGameValidator.ValidateActiveParticipants(
            request,
            activeUserIds
        );

        if (membershipErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(membershipErrors);
        }

        var game = new Game
        {
            CrewId = crewUserContext.CrewId,
            CrewUuid = crewUserContext.CrewUuid,
            Format = request.Format,
            RecorderUserId = crewUserContext.UserId,
            PlayedAtUtc = timeProvider.GetUtcNow(),
            Sides = request.Sides
                .Select((side, sideIndex) => new GameSide
                {
                    SideNumber = sideIndex + 1,
                    Score = side.Score,
                    Participants = side.Participants
                        .Select((participant, participantIndex) => new GameParticipant
                        {
                            SlotNumber = participantIndex + 1,
                            UserId = participant.UserId,
                            MemberDisplayName = participant.UserId is { } userId
                                ? activeMemberByUserId[userId].DisplayName
                                : null,
                            GuestName = participant.GuestName?.Trim(),
                        })
                        .ToList(),
                })
                .ToList(),
        };

        await gameStatisticsService.CalculateStatisticsAsync(
            game,
            cancellationToken
        );

        database.Games.Add(game);
        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RecordGameHttpResponse
        {
            GameId = game.Uuid,
            CrewId = game.CrewUuid,
            Format = game.Format,
            RecorderUserId = game.RecorderUserId,
            PlayedAtUtc = game.PlayedAtUtc,
            Sides = game.Sides
                .Select(side => new RecordedGameSideHttpResponse
                {
                    SideNumber = side.SideNumber,
                    Score = side.Score,
                    Participants = side.Participants
                        .Select(participant => new RecordedGameParticipantHttpResponse
                        {
                            SlotNumber = participant.SlotNumber,
                            UserId = participant.UserId,
                            GuestName = participant.GuestName,
                        })
                        .ToArray(),
                })
                .ToArray(),
        });
    }
}
