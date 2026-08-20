using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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
            .WithRequestValidation<RecordGameHttpRequest>(Validate)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    internal static Dictionary<string, string[]> Validate(RecordGameHttpRequest request)
    {
        var errors = new Dictionary<string, List<string>>();
        var formatIsSupported = request.Format is GameFormat.Singles or GameFormat.Doubles;

        if (!formatIsSupported)
        {
            AddError(errors, nameof(request.Format), "Select singles or doubles.");
        }

        if (request.Sides is not { Length: 2 })
        {
            AddError(errors, nameof(request.Sides), "A game must have exactly two Sides.");
            return ToValidationErrors(errors);
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
                AddError(errors, sideKey, "Enter a Side with participants and a score.");
                continue;
            }

            var participantsKey = $"{sideKey}.{nameof(side.Participants)}";

            if (side.Participants is not { } participants)
            {
                AddError(errors, participantsKey, "Enter the participants for this Side.");
                continue;
            }

            if (formatIsSupported && participants.Length != expectedParticipantCount)
            {
                var formatName = request.Format == GameFormat.Singles ? "singles" : "doubles";
                AddError(
                    errors,
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
                    AddError(
                        errors,
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
            AddError(
                errors,
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
                AddError(
                    errors,
                    nameof(request.Sides),
                    "Scores must be non-negative whole numbers."
                );
            }
            else if (!GameScoreRules.IsValidFinalScore(
                request.Sides[0].Score,
                request.Sides[1].Score
            ))
            {
                AddError(
                    errors,
                    nameof(request.Sides),
                    "Enter a completed score played to 11 and won by two."
                );
            }
        }

        return ToValidationErrors(errors);
    }

    private static async Task<Results<
        Ok<RecordGameHttpResponse>,
        ValidationProblem,
        ProblemHttpResult>> HandleAsync(
        RecordGameHttpRequest request,
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        PickleCrewDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var activeMembers = await ActiveCrewRoster.TryListAsync(
            workOs,
            crewUserContext.OrganizationId,
            cancellationToken
        );

        if (activeMembers is null)
        {
            return TypedResults.Problem(
                detail: "Crew membership could not be verified. Try saving again.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Game could not be saved"
            );
        }

        var activeUserIds = activeMembers
            .Select(member => member.WorkOsUserId)
            .ToHashSet(StringComparer.Ordinal);

        if (!activeUserIds.Contains(crewUserContext.UserId))
        {
            return TypedResults.Problem(
                detail: "Your active Crew membership could not be verified.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Crew membership required"
            );
        }

        var membershipErrors = ValidateActiveParticipants(request, activeUserIds);

        if (membershipErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(membershipErrors);
        }

        var playedAtUtc = NormalizeForPersistence(timeProvider.GetUtcNow());
        var game = CreateGame(request, crewUserContext, playedAtUtc);
        dbContext.Games.Add(game);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TypedResults.Problem(
                detail: "The Game was not saved. Try again.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Game could not be saved"
            );
        }

        return TypedResults.Ok(ToHttpResponse(game, crewUserContext.CrewUuid));
    }

    private static void ValidateParticipant(
        RecordGameParticipantHttpRequest participant,
        string participantKey,
        Dictionary<string, List<string>> errors,
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
            AddError(errors, userIdKey, "Select an active Crew member.");
        }

        if (participant.GuestName is not null && !hasGuestName)
        {
            AddError(errors, guestNameKey, "Enter a nonblank Guest name.");
        }

        if (hasUserId == hasGuestName)
        {
            AddError(
                errors,
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
                AddError(errors, existingMemberKey, message);
                AddError(errors, userIdKey, message);
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
            AddError(errors, existingGuestKey, message);
            AddError(errors, guestNameKey, message);
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
        var errors = new Dictionary<string, List<string>>();
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
                    AddError(
                        errors,
                        key,
                        "This participant is not an active member of the current Crew."
                    );
                }
            }
        }

        if (activeParticipantCount == 0)
        {
            AddError(
                errors,
                nameof(request.Sides),
                "At least one participant must be an active member of the current Crew."
            );
        }

        return ToValidationErrors(errors);
    }

    private static DateTimeOffset NormalizeForPersistence(DateTimeOffset timestamp)
    {
        var utcTimestamp = timestamp.ToUniversalTime();
        var persistedTicks = utcTimestamp.Ticks
            - utcTimestamp.Ticks % TimeSpan.TicksPerMicrosecond;

        return new DateTimeOffset(persistedTicks, TimeSpan.Zero);
    }

    private static Game CreateGame(
        RecordGameHttpRequest request,
        CrewUserContext crewUserContext,
        DateTimeOffset playedAtUtc)
    {
        var game = new Game
        {
            CrewId = crewUserContext.CrewId,
            Format = request.Format,
            RecorderUserId = crewUserContext.UserId,
            PlayedAtUtc = playedAtUtc,
        };

        for (var sideIndex = 0; sideIndex < request.Sides.Length; sideIndex++)
        {
            var requestSide = request.Sides[sideIndex];
            var side = new GameSide
            {
                SideNumber = sideIndex + 1,
                Score = requestSide.Score,
            };

            for (var participantIndex = 0; participantIndex < requestSide.Participants.Length; participantIndex++)
            {
                var requestParticipant = requestSide.Participants[participantIndex];
                side.Participants.Add(new GameParticipant
                {
                    SlotNumber = participantIndex + 1,
                    UserId = requestParticipant.UserId,
                    GuestName = requestParticipant.GuestName is null
                        ? null
                        : NormalizeGuestName(requestParticipant.GuestName),
                });
            }

            game.Sides.Add(side);
        }

        return game;
    }

    private static RecordGameHttpResponse ToHttpResponse(Game game, Guid crewUuid)
    {
        return new RecordGameHttpResponse
        {
            GameId = game.Uuid,
            CrewId = crewUuid,
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
        };
    }

    private static string NormalizeGuestName(string guestName) => guestName.Trim();

    private static void AddError(
        Dictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        if (!messages.Contains(message, StringComparer.Ordinal))
        {
            messages.Add(message);
        }
    }

    private static Dictionary<string, string[]> ToValidationErrors(
        Dictionary<string, List<string>> errors)
    {
        return errors.ToDictionary(
            error => error.Key,
            error => error.Value.ToArray(),
            StringComparer.Ordinal
        );
    }
}

internal static class GameScoreRules
{
    public static bool IsValidFinalScore(int firstScore, int secondScore)
    {
        if (firstScore < 0 || secondScore < 0 || firstScore == secondScore)
        {
            return false;
        }

        var winningScore = Math.Max(firstScore, secondScore);
        var losingScore = Math.Min(firstScore, secondScore);

        if (winningScore == 11)
        {
            return losingScore <= 9;
        }

        return losingScore >= 10 && winningScore - losingScore == 2;
    }
}
