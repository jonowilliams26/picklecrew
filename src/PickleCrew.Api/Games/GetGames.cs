using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.Shared;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Games;

public sealed record GetGamesHttpRequest : PaginatedHttpRequest;

public sealed record GameHistoryItemHttpResponse
{
    public required Guid GameId { get; init; }

    public required GameFormat Format { get; init; }

    public required DateTimeOffset PlayedAtUtc { get; init; }

    public required GameHistorySideHttpResponse[] Sides { get; init; }
}

public sealed record GameHistorySideHttpResponse
{
    public required int SideNumber { get; init; }

    public required int Score { get; init; }

    public required GameHistoryParticipantHttpResponse[] Participants { get; init; }
}

public sealed record GameHistoryParticipantHttpResponse
{
    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required bool IsGuest { get; init; }
}

internal sealed class GetGamesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(string.Empty, HandleAsync)
            .WithName("GetGames")
            .WithSummary("Gets a page of completed Games for the active Crew.")
            .WithRequestValidation<GetGamesHttpRequest>(Pagination.Validate);
    }

    private static async Task<Ok<PaginatedHttpResponse<GameHistoryItemHttpResponse>>> HandleAsync(
        [AsParameters] GetGamesHttpRequest request,
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CrewScopedDbContext database,
        CancellationToken cancellationToken)
    {
        var activeMembers = await workOs.ListActiveCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );

        var games = await database.Games
            .AsNoTracking()
            .OrderByDescending(game => game.PlayedAtUtc)
            .ThenByDescending(game => game.Id)
            .ToArrayAsync(cancellationToken);

        var activeMemberByUserId = activeMembers.ToDictionary(
            member => member.WorkOsUserId,
            StringComparer.Ordinal
        );

        var paginatedGames = games
            .Select(game => ToHttpResponse(game, activeMemberByUserId))
            .Paginate(request);

        return TypedResults.Ok(paginatedGames);
    }

    private static GameHistoryItemHttpResponse ToHttpResponse(
        Game game,
        Dictionary<string, ActiveCrewMember> activeMemberByUserId)
    {
        return new GameHistoryItemHttpResponse
        {
            GameId = game.Uuid,
            Format = game.Format,
            PlayedAtUtc = game.PlayedAtUtc,
            Sides = game.Sides
                .OrderBy(side => side.SideNumber)
                .Select(side => new GameHistorySideHttpResponse
                {
                    SideNumber = side.SideNumber,
                    Score = side.Score,
                    Participants = side.Participants
                        .OrderBy(participant => participant.SlotNumber)
                        .Select(participant => ToHttpResponse(
                            participant,
                            activeMemberByUserId
                        ))
                        .ToArray(),
                })
                .ToArray(),
        };
    }

    private static GameHistoryParticipantHttpResponse ToHttpResponse(
        GameParticipant participant,
        Dictionary<string, ActiveCrewMember> activeMemberByUserId)
    {
        ActiveCrewMember? activeMember = null;

        if (participant.UserId is { } userId)
        {
            activeMemberByUserId.TryGetValue(userId, out activeMember);
        }

        return new GameHistoryParticipantHttpResponse
        {
            DisplayName = participant.GuestName
                ?? activeMember?.DisplayName
                ?? participant.MemberDisplayName
                ?? "Former Crew member",
            ProfilePictureUrl = activeMember?.ProfilePictureUrl,
            IsGuest = participant.GuestName is not null,
        };
    }
}
