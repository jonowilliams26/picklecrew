using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Games.Services;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Games;

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
            .WithRequestValidation<RecordGameHttpRequest>(Validate);
    }

    internal static Dictionary<string, string[]> Validate(RecordGameHttpRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var formatIsSupported = Enum.IsDefined(request.Format);

        if (!formatIsSupported)
        {
            errors.AddError(nameof(request.Format), "Select singles or doubles.");
        }

        if (request.Sides is not { Length: 2 })
        {
            errors.AddError(nameof(request.Sides), "A game must have exactly two Sides.");
            return errors;
        }

        var expectedParticipantCount = request.Format == GameFormat.Doubles ? 2 : 1;
        var memberLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        var guestLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var validMemberCount = 0;

        for (var sideIndex = 0; sideIndex < request.Sides.Length; sideIndex++)
        {
            var side = request.Sides[sideIndex];
            var sideKey = $"{nameof(request.Sides)}[{sideIndex}]";

            if (side is null)
            {
                errors.AddError(sideKey, "Enter a Side with participants and a score.");
                continue;
            }

            var participantsKey = $"{sideKey}.{nameof(side.Participants)}";

            if (side.Participants is not { } participants)
            {
                errors.AddError(participantsKey, "Enter the participants for this Side.");
                continue;
            }

            if (formatIsSupported && participants.Length != expectedParticipantCount)
            {
                var formatName = request.Format == GameFormat.Singles ? "singles" : "doubles";
                errors.AddError(
                    participantsKey,
                    $"Each Side in a {formatName} game must have exactly {expectedParticipantCount} participant{(expectedParticipantCount == 1 ? string.Empty : "s")}."
                );
                continue;
            }

            for (var participantIndex = 0; participantIndex < participants.Length; participantIndex++)
            {
                var participant = participants[participantIndex];
                var participantKey = $"{participantsKey}[{participantIndex}]";

                if (participant is null)
                {
                    errors.AddError(
                        participantKey,
                        "Select an active Crew member or enter a Guest."
                    );
                    continue;
                }

                ValidateParticipant(
                    participant,
                    participantKey,
                    errors,
                    memberLocations,
                    guestLocations,
                    ref validMemberCount
                );
            }
        }

        if (validMemberCount == 0)
        {
            errors.AddError(
                nameof(request.Sides),
                "At least one participant must be an active member of the current Crew."
            );
        }

        var sidesArePresent = request.Sides.All(side => side is not null);

        if (sidesArePresent)
        {
            var hasNegativeScore = request.Sides.Any(side => side.Score < 0);

            if (hasNegativeScore)
            {
                errors.AddError(
                    nameof(request.Sides),
                    "Scores must be non-negative whole numbers."
                );
            }
            else if (!GameScoreRules.IsValidFinalScore(
                request.Sides[0].Score,
                request.Sides[1].Score
            ))
            {
                errors.AddError(
                    nameof(request.Sides),
                    "Enter a completed score played to 11 and won by two."
                );
            }
        }

        return errors;
    }

    private static async Task<Results<
        Ok<RecordGameHttpResponse>,
        ValidationProblem,
        ForbidHttpResult>> HandleAsync(
        RecordGameHttpRequest request,
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CrewScopedDbContext database,
        GameStatisticsService gameStatisticsService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var activeMembers = await workOs.ListActiveCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );

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

        var membershipErrors = ValidateActiveParticipants(request, activeUserIds);

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

        var statisticsChanges = await gameStatisticsService.CalculateStatistics(
            game,
            cancellationToken
        );

        database.CrewMemberStatistics.AddRange(statisticsChanges.Created);
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

    private static void ValidateParticipant(
        RecordGameParticipantHttpRequest participant,
        string participantKey,
        Dictionary<string, string[]> errors,
        Dictionary<string, string> memberLocations,
        Dictionary<string, string> guestLocations,
        ref int validMemberCount)
    {
        var userIdKey = $"{participantKey}.{nameof(participant.UserId)}";
        var guestNameKey = $"{participantKey}.{nameof(participant.GuestName)}";
        var hasUserId = !string.IsNullOrWhiteSpace(participant.UserId);
        var hasGuestName = !string.IsNullOrWhiteSpace(participant.GuestName);

        if (participant.UserId is not null && !hasUserId)
        {
            errors.AddError(userIdKey, "Select an active Crew member.");
        }

        if (participant.GuestName is not null && !hasGuestName)
        {
            errors.AddError(guestNameKey, "Enter a nonblank Guest name.");
        }

        if (hasUserId == hasGuestName)
        {
            errors.AddError(
                participantKey,
                "Choose exactly one participant type: an active Crew member or a Guest."
            );
            return;
        }

        if (hasUserId)
        {
            validMemberCount++;
            var userId = participant.UserId!;

            if (memberLocations.TryGetValue(userId, out var existingMemberKey))
            {
                const string message = "A Crew member may participate only once in a Game.";
                errors.AddError(existingMemberKey, message);
                errors.AddError(userIdKey, message);
            }
            else
            {
                memberLocations[userId] = userIdKey;
            }

            return;
        }

        var guestName = NormalizeGuestName(participant.GuestName!);

        if (guestLocations.TryGetValue(guestName, out var existingGuestKey))
        {
            const string message = "Guest names must be distinct within a Game.";
            errors.AddError(existingGuestKey, message);
            errors.AddError(guestNameKey, message);
        }
        else
        {
            guestLocations[guestName] = guestNameKey;
        }
    }

    private static Dictionary<string, string[]> ValidateActiveParticipants(
        RecordGameHttpRequest request,
        HashSet<string> activeUserIds)
    {
        var errors = new Dictionary<string, string[]>();
        var activeParticipantCount = 0;

        for (var sideIndex = 0; sideIndex < request.Sides.Length; sideIndex++)
        {
            var participants = request.Sides[sideIndex].Participants;

            for (var participantIndex = 0; participantIndex < participants.Length; participantIndex++)
            {
                var participant = participants[participantIndex];

                if (participant.UserId is null)
                {
                    continue;
                }

                var key = $"{nameof(request.Sides)}[{sideIndex}].{nameof(RecordGameSideHttpRequest.Participants)}[{participantIndex}].{nameof(participant.UserId)}";

                if (activeUserIds.Contains(participant.UserId))
                {
                    activeParticipantCount++;
                }
                else
                {
                    errors.AddError(
                        key,
                        "This participant is not an active member of the current Crew."
                    );
                }
            }
        }

        if (activeParticipantCount == 0)
        {
            errors.AddError(
                nameof(request.Sides),
                "At least one participant must be an active member of the current Crew."
            );
        }

        return errors;
    }

    private static string NormalizeGuestName(string guestName) => guestName.Trim();
}
