using System.Net.Http.Json;
using PickleCrew.Api.Data;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetBestPartnerEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetBestPartner_WhenNoPairingQualifies_ShouldReturnNullBestPartner()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();

        var result = await GetBestPartnerAsync(context.Administrator);

        Assert.Null(result.BestPartner);
    }

    [Fact]
    public async Task GetBestPartner_WhenQualifyingDoublesHistoryExists_ShouldReturnPartnerAndPairingRecord()
    {
        const string administratorId = "user_personal_partner_admin";
        const string partnerId = "user_personal_partner_qualified";
        const string unqualifiedPartnerId = "user_personal_partner_unqualified";
        const string firstOpponentId = "user_personal_partner_opponent_one";
        const string secondOpponentId = "user_personal_partner_opponent_two";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: partnerId,
                displayName: "Current Best Partner",
                profilePictureUrl: "https://example.test/best-partner.jpg"
            )
            .WithMember(id: unqualifiedPartnerId)
            .WithMember(id: firstOpponentId)
            .WithMember(id: secondOpponentId)
            .WithDoublesGames(
                side1: (administratorId, partnerId),
                side2: (firstOpponentId, "Guest Opponent"),
                (Side1: 11, Side2: 6),
                (Side1: 9, Side2: 11),
                (Side1: 11, Side2: 8)
            )
            .WithDoublesGames(
                side1: (administratorId, unqualifiedPartnerId),
                side2: (firstOpponentId, secondOpponentId),
                (Side1: 11, Side2: 3),
                (Side1: 11, Side2: 4)
            )
            .WithDoublesGame(
                side1: (administratorId, "Guest Teammate", 11),
                side2: (firstOpponentId, secondOpponentId, 5)
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (partnerId, 2)
            )
            .BuildAsync();

        var result = await GetBestPartnerAsync(context.Administrator);

        var bestPartner = Assert.IsType<BestPartnerHttpResponse>(result.BestPartner);
        Assert.Equal(partnerId, bestPartner.UserId);
        Assert.Equal("Current Best Partner", bestPartner.DisplayName);
        Assert.Equal(
            "https://example.test/best-partner.jpg",
            bestPartner.ProfilePictureUrl
        );
        Assert.Equal(2, bestPartner.Wins);
        Assert.Equal(1, bestPartner.Losses);
        Assert.Equal(3, bestPartner.GamesPlayed);
        Assert.Equal(2m / 3m, bestPartner.WinPercentage);
        Assert.Equal(31, bestPartner.PointsScored);
        Assert.Equal(25, bestPartner.PointsConceded);
        Assert.Equal(6, bestPartner.PointDifferential);
    }

    [Fact]
    public async Task GetBestPartner_WhenPairingsDifferByEligibilityAndRank_ShouldReturnDeterministicActiveCrewPartner()
    {
        const string administratorId = "user_personal_rank_admin";
        const string winnerId = "user_personal_rank_a_winner";
        const string exactTieId = "user_personal_rank_z_exact_tie";
        const string lowerDifferentialId = "user_personal_rank_lower_differential";
        const string fewerGamesId = "user_personal_rank_fewer_games";
        const string lowerPercentageId = "user_personal_rank_lower_percentage";
        const string unqualifiedId = "user_personal_rank_unqualified";
        const string inactiveId = "user_personal_rank_inactive";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: winnerId, displayName: "Deterministic Winner")
            .WithMember(id: exactTieId)
            .WithMember(id: lowerDifferentialId)
            .WithMember(id: fewerGamesId)
            .WithMember(id: lowerPercentageId)
            .WithMember(id: unqualifiedId)
            .WithMember(
                id: inactiveId,
                status: FakeMembershipStatus.Inactive
            )
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(administratorId, winnerId, 8, 2, 110, 101),
            new PairingSeed(administratorId, exactTieId, 8, 2, 110, 101),
            new PairingSeed(administratorId, lowerDifferentialId, 8, 2, 100, 98),
            new PairingSeed(administratorId, fewerGamesId, 4, 1, 50, 40),
            new PairingSeed(administratorId, lowerPercentageId, 3, 1, 44, 20),
            new PairingSeed(administratorId, unqualifiedId, 2, 0, 22, 0),
            new PairingSeed(administratorId, inactiveId, 20, 0, 220, 0),
            new PairingSeed(winnerId, exactTieId, 100, 0, 1100, 0)
        );
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Personal Partner Crew")
            .BuildAsync();
        await SeedPairingsAsync(
            otherCrew,
            new PairingSeed(administratorId, exactTieId, 100, 0, 1100, 0)
        );

        var result = await GetBestPartnerAsync(context.Administrator);

        var bestPartner = Assert.IsType<BestPartnerHttpResponse>(result.BestPartner);
        Assert.Equal(winnerId, bestPartner.UserId);
        Assert.Equal("Deterministic Winner", bestPartner.DisplayName);
        Assert.Equal(8, bestPartner.Wins);
        Assert.Equal(2, bestPartner.Losses);
        Assert.Equal(10, bestPartner.GamesPlayed);
        Assert.Equal(0.8m, bestPartner.WinPercentage);
        Assert.Equal(110, bestPartner.PointsScored);
        Assert.Equal(101, bestPartner.PointsConceded);
        Assert.Equal(9, bestPartner.PointDifferential);
    }

    private async Task SeedPairingsAsync(
        BuiltCrewTestData context,
        params PairingSeed[] pairings)
    {
        await using var database = fixture.CreateDbContext();
        database.CrewDoublesPairingStatistics.AddRange(
            pairings.Select(pairing =>
            {
                var key = CrewDoublesPairingKey.Create(
                    pairing.FirstPlayerId,
                    pairing.SecondPlayerId
                );

                return new CrewDoublesPairingStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    Player1Id = key.Player1Id,
                    Player2Id = key.Player2Id,
                    Wins = pairing.Wins,
                    Losses = pairing.Losses,
                    PointsScored = pairing.PointsScored,
                    PointsConceded = pairing.PointsConceded,
                };
            })
        );
        await database.SaveChangesAsync();
    }

    private async Task<GetBestPartnerHttpResponse> GetBestPartnerAsync(
        TestIdentity identity)
    {
        using var apiClient = fixture.CreateApiClient(identity);
        using var response = await apiClient.GetAsync("/statistics/best-partner");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetBestPartnerHttpResponse>(
            TestJsonSerializerOptions.Default
        ) ?? throw new InvalidOperationException(
            "The API returned an empty best partner response."
        );
    }

    private sealed record PairingSeed(
        string FirstPlayerId,
        string SecondPlayerId,
        long Wins,
        long Losses,
        long PointsScored,
        long PointsConceded);
}
