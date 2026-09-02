using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
                scores:
                [
                    (11, 6),
                    (9, 11),
                    (11, 8),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, unqualifiedPartnerId),
                side2: (firstOpponentId, secondOpponentId),
                scores:
                [
                    (11, 3),
                    (11, 4),
                ]
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
        Assert.Equal(2m / 3m, bestPartner.WinPercentage, precision: 20);
        Assert.Equal(31, bestPartner.PointsScored);
        Assert.Equal(25, bestPartner.PointsConceded);
        Assert.Equal(6, bestPartner.PointDifferential);
    }

    [Fact]
    public async Task GetBestPartner_WhenPairingsDifferByEligibilityAndRank_ShouldReturnBestActiveCrewPartner()
    {
        const string administratorId = "user_personal_rank_admin";
        const string winnerId = "user_personal_rank_a_winner";
        const string lowerDifferentialId = "user_personal_rank_lower_differential";
        const string fewerGamesId = "user_personal_rank_fewer_games";
        const string lowerPercentageId = "user_personal_rank_lower_percentage";
        const string unqualifiedId = "user_personal_rank_unqualified";
        const string inactiveId = "user_personal_rank_inactive";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: winnerId, displayName: "Best Partner")
            .WithMember(id: lowerDifferentialId)
            .WithMember(id: fewerGamesId)
            .WithMember(id: lowerPercentageId)
            .WithMember(id: unqualifiedId)
            .WithMember(
                id: inactiveId,
                status: FakeMembershipStatus.Inactive
            )
            .WithDoublesGames(
                side1: (administratorId, winnerId),
                side2: ("Winner Guest One", "Winner Guest Two"),
                scores:
                [
                    (11, 1),
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                    (11, 6),
                    (11, 7),
                    (11, 8),
                    (9, 11),
                    (8, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, lowerDifferentialId),
                side2: ("Differential Guest One", "Differential Guest Two"),
                scores:
                [
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, fewerGamesId),
                side2: ("Fewer Guest One", "Fewer Guest Two"),
                scores:
                [
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, lowerPercentageId),
                side2: ("Percentage Guest One", "Percentage Guest Two"),
                scores:
                [
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, unqualifiedId),
                side2: ("Unqualified Guest One", "Unqualified Guest Two"),
                scores:
                [
                    (11, 0),
                    (11, 1),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, inactiveId),
                side2: ("Inactive Guest One", "Inactive Guest Two"),
                scores:
                [
                    (11, 0),
                    (11, 1),
                    (11, 2),
                    (11, 3),
                ]
            )
            .BuildAsync();
        await fixture.CreateCrewBuilder()
            .WithName("Other Personal Partner Crew")
            .WithMember(id: administratorId)
            .WithMember(id: winnerId)
            .WithDoublesGames(
                side1: (administratorId, winnerId),
                side2: ("Other Guest One", "Other Guest Two"),
                scores:
                [
                    (11, 0),
                    (11, 1),
                    (11, 2),
                ]
            )
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            await database.CrewDoublesPairingStatistics
                .Where(statistics => statistics.CrewUuid == context.Uuid)
                .ExecuteDeleteAsync();
        }

        var result = await GetBestPartnerAsync(context.Administrator);

        var bestPartner = Assert.IsType<BestPartnerHttpResponse>(result.BestPartner);
        Assert.Equal(winnerId, bestPartner.UserId);
        Assert.Equal("Best Partner", bestPartner.DisplayName);
        Assert.Equal(8, bestPartner.Wins);
        Assert.Equal(2, bestPartner.Losses);
        Assert.Equal(10, bestPartner.GamesPlayed);
        Assert.Equal(0.8m, bestPartner.WinPercentage);
        Assert.Equal(105, bestPartner.PointsScored);
        Assert.Equal(58, bestPartner.PointsConceded);
        Assert.Equal(47, bestPartner.PointDifferential);
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
}
