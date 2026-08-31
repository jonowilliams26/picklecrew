using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.Shared;

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
        ICrewService crewService,
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var cacheOption = new GamesHistoryCacheOption(
            crewUserContext.CrewUuid,
            request.Page ?? Pagination.DefaultPage,
            request.PageSize ?? Pagination.DefaultPageSize
        );
        var paginatedGames = await cache.GetOrCreateAsync(
            cacheOption,
            async factoryCancellationToken =>
            {
                var activeMembers = await crewService.ListActiveCrewMembersAsync(
                    factoryCancellationToken
                );
                var persistedGames = await database.Games
                    .AsNoTracking()
                    .OrderByDescending(game => game.PlayedAtUtc)
                    .ThenByDescending(game => game.Id)
                    .ToArrayAsync(factoryCancellationToken);
                var activeMemberByUserId = activeMembers.ToDictionary(
                    member => member.WorkOsUserId,
                    StringComparer.Ordinal
                );

                return persistedGames
                    .Select(game => ToHttpResponse(game, activeMemberByUserId))
                    .Paginate(request);
            },
            cancellationToken
        );

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
