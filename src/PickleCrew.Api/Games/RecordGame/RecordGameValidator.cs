using PickleCrew.Api.Data;
using PickleCrew.Api.EndpointFilters;

namespace PickleCrew.Api.Games.RecordGame;

internal static class RecordGameValidator
{
    public static Dictionary<string, string[]> Validate(RecordGameHttpRequest request)
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

        var memberLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        var guestLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var sideIndex = 0; sideIndex < request.Sides.Length; sideIndex++)
        {
            ValidateSide(
                request.Sides[sideIndex],
                sideIndex,
                request.Format,
                formatIsSupported,
                errors,
                memberLocations,
                guestLocations
            );
        }

        if (memberLocations.Count == 0)
        {
            errors.AddError(
                nameof(request.Sides),
                "At least one participant must be an active member of the current Crew."
            );
        }

        ValidateScores(request.Sides, errors);

        return errors;
    }

    public static Dictionary<string, string[]> ValidateActiveParticipants(
        RecordGameHttpRequest request,
        HashSet<string> activeUserIds
    )
    {
        var errors = new Dictionary<string, string[]>();
        var activeParticipantCount = 0;

        for (var sideIndex = 0; sideIndex < request.Sides.Length; sideIndex++)
        {
            activeParticipantCount += ValidateActiveParticipantsOnSide(
                request.Sides[sideIndex],
                sideIndex,
                activeUserIds,
                errors
            );
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

    private static void ValidateSide(
        RecordGameSideHttpRequest? side,
        int sideIndex,
        GameFormat format,
        bool formatIsSupported,
        Dictionary<string, string[]> errors,
        Dictionary<string, string> memberLocations,
        Dictionary<string, string> guestLocations
    )
    {
        var sideKey = $"{nameof(RecordGameHttpRequest.Sides)}[{sideIndex}]";

        if (side is null)
        {
            errors.AddError(sideKey, "Enter a Side with participants and a score.");
            return;
        }

        var participantsKey = $"{sideKey}.{nameof(side.Participants)}";

        if (side.Participants is not { } participants)
        {
            errors.AddError(participantsKey, "Enter the participants for this Side.");
            return;
        }

        var expectedParticipantCount = format == GameFormat.Doubles ? 2 : 1;

        if (formatIsSupported && participants.Length != expectedParticipantCount)
        {
            var formatName = format == GameFormat.Singles ? "singles" : "doubles";
            errors.AddError(
                participantsKey,
                $"Each Side in a {formatName} game must have exactly {expectedParticipantCount} participant{(expectedParticipantCount == 1 ? string.Empty : "s")}."
            );
            return;
        }

        for (var participantIndex = 0; participantIndex < participants.Length; participantIndex++)
        {
            ValidateParticipant(
                participants[participantIndex],
                $"{participantsKey}[{participantIndex}]",
                errors,
                memberLocations,
                guestLocations
            );
        }
    }

    private static void ValidateParticipant(
        RecordGameParticipantHttpRequest? participant,
        string participantKey,
        Dictionary<string, string[]> errors,
        Dictionary<string, string> memberLocations,
        Dictionary<string, string> guestLocations
    )
    {
        if (participant is null)
        {
            errors.AddError(
                participantKey,
                "Select an active Crew member or enter a Guest."
            );
            return;
        }

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
            ValidateMember(
                participant.UserId!,
                userIdKey,
                errors,
                memberLocations
            );
            return;
        }

        ValidateGuest(
            participant.GuestName!.Trim(),
            guestNameKey,
            errors,
            guestLocations
        );
    }

    private static void ValidateMember(
        string userId,
        string userIdKey,
        Dictionary<string, string[]> errors,
        Dictionary<string, string> memberLocations
    )
    {
        if (!memberLocations.TryGetValue(userId, out var existingMemberKey))
        {
            memberLocations[userId] = userIdKey;
            return;
        }

        const string message = "A Crew member may participate only once in a Game.";
        errors.AddError(existingMemberKey, message);
        errors.AddError(userIdKey, message);
    }

    private static void ValidateGuest(
        string guestName,
        string guestNameKey,
        Dictionary<string, string[]> errors,
        Dictionary<string, string> guestLocations
    )
    {
        if (!guestLocations.TryGetValue(guestName, out var existingGuestKey))
        {
            guestLocations[guestName] = guestNameKey;
            return;
        }

        const string message = "Guest names must be distinct within a Game.";
        errors.AddError(existingGuestKey, message);
        errors.AddError(guestNameKey, message);
    }

    private static void ValidateScores(
        RecordGameSideHttpRequest[] sides,
        Dictionary<string, string[]> errors
    )
    {
        var firstSide = sides[0];
        var secondSide = sides[1];

        if (firstSide is null || secondSide is null)
        {
            return;
        }

        if (firstSide.Score < 0 || secondSide.Score < 0)
        {
            errors.AddError(
                nameof(RecordGameHttpRequest.Sides),
                "Scores must be non-negative whole numbers."
            );
            return;
        }

        if (GameScoreRules.IsValidFinalScore(firstSide.Score, secondSide.Score))
        {
            return;
        }

        errors.AddError(
            nameof(RecordGameHttpRequest.Sides),
            "Enter a completed score played to 11 and won by two."
        );
    }

    private static int ValidateActiveParticipantsOnSide(
        RecordGameSideHttpRequest side,
        int sideIndex,
        HashSet<string> activeUserIds,
        Dictionary<string, string[]> errors
    )
    {
        var activeParticipantCount = 0;

        for (var participantIndex = 0; participantIndex < side.Participants.Length; participantIndex++)
        {
            var participant = side.Participants[participantIndex];

            if (participant.UserId is null)
            {
                continue;
            }

            var userIdKey = $"{nameof(RecordGameHttpRequest.Sides)}[{sideIndex}].{nameof(RecordGameSideHttpRequest.Participants)}[{participantIndex}].{nameof(participant.UserId)}";

            if (!activeUserIds.Contains(participant.UserId))
            {
                errors.AddError(
                    userIdKey,
                    "This participant is not an active member of the current Crew."
                );
                continue;
            }

            activeParticipantCount++;
        }

        return activeParticipantCount;
    }
}
