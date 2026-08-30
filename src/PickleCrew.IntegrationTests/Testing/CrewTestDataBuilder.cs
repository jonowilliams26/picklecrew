using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Games.RecordGame;

namespace PickleCrew.IntegrationTests.Testing;

internal static class CrewTestDataBuilderExtensions
{
    public static CrewTestDataBuilder CreateCrewBuilder(this AppHostFixture fixture)
    {
        return new CrewTestDataBuilder(fixture);
    }
}

internal sealed class CrewTestDataBuilder(AppHostFixture fixture)
{
    private readonly List<CrewMemberDefinition> memberDefinitions = [];
    private readonly List<GameDefinition> gameDefinitions = [];
    private CrewMemberDefinition? administratorDefinition;
    private string crewName = "Kitchen Legends";

    public CrewTestDataBuilder WithName(string name)
    {
        crewName = name;

        return this;
    }

    public CrewTestDataBuilder WithAdministrator(
        string? id = null,
        string? emailAddress = null,
        string? displayName = null,
        string? profilePictureUrl = null
    )
    {
        var administratorId = id ?? $"user_test_{Guid.NewGuid():N}";
        var administratorEmailAddress = emailAddress ?? $"{administratorId}@example.test";
        administratorDefinition = new CrewMemberDefinition
        {
            Id = administratorId,
            EmailAddress = administratorEmailAddress,
            DisplayName = displayName ?? administratorEmailAddress,
            ProfilePictureUrl = profilePictureUrl,
            Role = FakeMembershipRole.Admin,
            Status = FakeMembershipStatus.Active,
        };

        return this;
    }

    public CrewTestDataBuilder WithMember(
        string? id = null,
        FakeMembershipRole role = FakeMembershipRole.Member,
        FakeMembershipStatus status = FakeMembershipStatus.Active,
        string? emailAddress = null,
        string? displayName = null,
        string? profilePictureUrl = null
    )
    {
        var memberId = id ?? $"user_test_{Guid.NewGuid():N}";
        var memberEmailAddress = emailAddress ?? $"{memberId}@example.test";
        memberDefinitions.Add(new CrewMemberDefinition
        {
            Id = memberId,
            EmailAddress = memberEmailAddress,
            DisplayName = displayName ?? memberEmailAddress,
            ProfilePictureUrl = profilePictureUrl,
            Role = role,
            Status = status,
        });

        return this;
    }

    public CrewTestDataBuilder WithSinglesGame(
        (string Player, int Score) side1,
        (string Player, int Score) side2
    )
    {
        gameDefinitions.Add(new GameDefinition
        {
            Format = GameFormat.Singles,
            Sides =
            [
                new GameSideDefinition
                {
                    Players = [side1.Player],
                    Score = side1.Score,
                },
                new GameSideDefinition
                {
                    Players = [side2.Player],
                    Score = side2.Score,
                },
            ],
        });

        return this;
    }

    public CrewTestDataBuilder WithSinglesGames(
        string side1,
        string side2,
        params (int Side1, int Side2)[] scores
    )
    {
        foreach (var score in scores)
        {
            WithSinglesGame(
                side1: (side1, score.Side1),
                side2: (side2, score.Side2)
            );
        }

        return this;
    }

    public CrewTestDataBuilder WithDoublesGame(
        (string FirstPlayer, string SecondPlayer, int Score) side1,
        (string FirstPlayer, string SecondPlayer, int Score) side2
    )
    {
        gameDefinitions.Add(new GameDefinition
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                new GameSideDefinition
                {
                    Players = [side1.FirstPlayer, side1.SecondPlayer],
                    Score = side1.Score,
                },
                new GameSideDefinition
                {
                    Players = [side2.FirstPlayer, side2.SecondPlayer],
                    Score = side2.Score,
                },
            ],
        });

        return this;
    }

    public CrewTestDataBuilder WithDoublesGames(
        (string FirstPlayer, string SecondPlayer) side1,
        (string FirstPlayer, string SecondPlayer) side2,
        params (int Side1, int Side2)[] scores
    )
    {
        foreach (var score in scores)
        {
            WithDoublesGame(
                side1: (side1.FirstPlayer, side1.SecondPlayer, score.Side1),
                side2: (side2.FirstPlayer, side2.SecondPlayer, score.Side2)
            );
        }

        return this;
    }

    public async Task<BuiltCrewTestData> BuildAsync(
        CancellationToken cancellationToken = default
    )
    {
        var administrator = administratorDefinition is null
            ? TestIdentity.CreateSubjectOnly()
            : new TestIdentity
            {
                UserId = administratorDefinition.Id,
                EmailAddress = administratorDefinition.EmailAddress,
            };
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();

        if (administratorDefinition is not null)
        {
            await fakeWorkOs.CreateUserAsync(
                administratorDefinition.Id,
                administratorDefinition.EmailAddress,
                displayName: administratorDefinition.DisplayName,
                profilePictureUrl: administratorDefinition.ProfilePictureUrl,
                cancellationToken: cancellationToken
            );
        }

        var crew = await fixture.CreateCrewAsync(administrator, crewName);
        administrator = administrator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        var members = new List<TestIdentity>(memberDefinitions.Count);

        foreach (var definition in memberDefinitions)
        {
            await fakeWorkOs.CreateUserAsync(
                definition.Id,
                definition.EmailAddress,
                displayName: definition.DisplayName,
                profilePictureUrl: definition.ProfilePictureUrl,
                cancellationToken: cancellationToken
            );
            await fakeWorkOs.CreateMembershipAsync(
                $"membership_test_{Guid.NewGuid():N}",
                definition.Id,
                crew.OrganizationId,
                definition.Role,
                definition.Status,
                cancellationToken
            );
            members.Add(new TestIdentity
            {
                UserId = definition.Id,
                EmailAddress = definition.EmailAddress,
                OrganizationId = crew.OrganizationId,
                Role = definition.Role.ToString().ToLowerInvariant(),
            });
        }

        var recordedGameIds = await RecordGamesAsync(
            administrator,
            cancellationToken
        );

        await using var database = fixture.CreateDbContext();
        var databaseId = await database.Crews
            .Where(candidate => candidate.Uuid == crew.CrewId)
            .Select(candidate => candidate.Id)
            .SingleAsync(cancellationToken);
        var gamesByUuid = await database.Games
            .AsNoTracking()
            .Where(game => recordedGameIds.Contains(game.Uuid))
            .ToDictionaryAsync(game => game.Uuid, cancellationToken);

        var games = recordedGameIds
            .Select(gameId => gamesByUuid[gameId])
            .ToArray();

        return new BuiltCrewTestData
        {
            Id = databaseId,
            Uuid = crew.CrewId,
            OrganizationId = crew.OrganizationId,
            Administrator = administrator,
            Members = members.ToArray(),
            Games = games,
        };
    }

    private async Task<Guid[]> RecordGamesAsync(
        TestIdentity administrator,
        CancellationToken cancellationToken
    )
    {
        var memberUserIds = memberDefinitions
            .Select(definition => definition.Id)
            .Append(administrator.UserId)
            .ToHashSet(StringComparer.Ordinal);
        var gameIds = new List<Guid>(gameDefinitions.Count);
        using var apiClient = fixture.CreateApiClient(administrator);

        foreach (var definition in gameDefinitions)
        {
            var request = new RecordGameHttpRequest
            {
                Format = definition.Format,
                Sides = definition.Sides
                    .Select(side => new RecordGameSideHttpRequest
                    {
                        Score = side.Score,
                        Participants = side.Players
                            .Select(player => CreateParticipant(player, memberUserIds))
                            .ToArray(),
                    })
                    .ToArray(),
            };

            using var response = await apiClient.PostAsJsonAsync(
                "/games",
                request,
                TestJsonSerializerOptions.Default,
                cancellationToken
            );
            response.EnsureSuccessStatusCode();
            var game = await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
                TestJsonSerializerOptions.Default,
                cancellationToken
            ) ?? throw new InvalidOperationException(
                "The API returned an empty game recording response."
            );

            gameIds.Add(game.GameId);
        }

        return gameIds.ToArray();
    }

    private static RecordGameParticipantHttpRequest CreateParticipant(
        string player,
        HashSet<string> memberUserIds
    )
    {
        return memberUserIds.Contains(player)
            ? new RecordGameParticipantHttpRequest { UserId = player }
            : new RecordGameParticipantHttpRequest { GuestName = player };
    }

    private sealed record CrewMemberDefinition
    {
        public required string Id { get; init; }

        public required string EmailAddress { get; init; }

        public required string DisplayName { get; init; }

        public string? ProfilePictureUrl { get; init; }

        public required FakeMembershipRole Role { get; init; }

        public required FakeMembershipStatus Status { get; init; }
    }

    private sealed record GameDefinition
    {
        public required GameFormat Format { get; init; }

        public required GameSideDefinition[] Sides { get; init; }
    }

    private sealed record GameSideDefinition
    {
        public required string[] Players { get; init; }

        public required int Score { get; init; }
    }
}

internal sealed record BuiltCrewTestData
{
    public required long Id { get; init; }

    public required Guid Uuid { get; init; }

    public required string OrganizationId { get; init; }

    public required TestIdentity Administrator { get; init; }

    public required TestIdentity[] Members { get; init; }

    public required Game[] Games { get; init; }
}
