using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetNemesisEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetNemesis_WhenMemberHasNotLostToAnotherMember_ShouldReturnNullNemesis()
    {
        const string administratorId = "user_nemesis_none_admin";
        const string memberId = "user_nemesis_none_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (memberId, 7)
            )
            .WithSinglesGame(
                side1: (administratorId, 7),
                side2: ("Winning Guest", 11)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/nemesis");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetNemesisHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Nemesis);
    }

    [Fact]
    public async Task GetNemesis_WhenOpponentHasPlayedFewerThanThreeGames_ShouldReturnNullNemesis()
    {
        const string administratorId = "user_nemesis_minimum_admin";
        const string opponentId = "user_nemesis_minimum_opponent";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: opponentId)
            .WithSinglesGames(
                side1: administratorId,
                side2: opponentId,
                scores:
                [
                    (7, 11),
                    (8, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/nemesis");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetNemesisHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Nemesis);
    }

    [Fact]
    public async Task GetNemesis_WhenQualifyingDoublesGamesContainGuests_ShouldReturnMemberOpponent()
    {
        const string administratorId = "user_nemesis_guest_admin";
        const string opponentId = "user_nemesis_guest_opponent";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: opponentId, displayName: "Member Opponent")
            .WithDoublesGames(
                side1: (administratorId, "Guest Partner"),
                side2: (opponentId, "Guest Opponent"),
                scores:
                [
                    (7, 11),
                    (11, 8),
                    (9, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/nemesis");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetNemesisHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var nemesis = Assert.IsType<NemesisHttpResponse>(result?.Nemesis);
        Assert.Equal(opponentId, nemesis.UserId);
        Assert.Equal("Member Opponent", nemesis.DisplayName);
        Assert.Equal(2, nemesis.Losses);
    }

    [Fact]
    public async Task GetNemesis_WhenOpponentsAreTied_ShouldReturnFirstByUserIdWithCurrentProfile()
    {
        const string administratorId = "user_nemesis_tie_admin";
        const string alphaOpponentId = "user_nemesis_tie_alpha_id";
        const string zuluOpponentId = "user_nemesis_tie_zulu_id";
        const string lowerOpponentId = "user_nemesis_tie_lower";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: alphaOpponentId,
                displayName: "Former Alpha",
                profilePictureUrl: "https://example.test/former-alpha.jpg"
            )
            .WithMember(id: zuluOpponentId, displayName: "Zulu Opponent")
            .WithMember(id: lowerOpponentId, displayName: "Lower Opponent")
            .WithSinglesGames(
                side1: administratorId,
                side2: alphaOpponentId,
                scores:
                [
                    (7, 11),
                    (8, 11),
                    (11, 6),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: zuluOpponentId,
                scores:
                [
                    (6, 11),
                    (9, 11),
                    (11, 7),
                ]
            )
            .WithSinglesGame(
                side1: (administratorId, 7),
                side2: (lowerOpponentId, 11)
            )
            .BuildAsync();

        using (var fakeWorkOs = fixture.CreateFakeWorkOsClient())
        {
            await fakeWorkOs.UpdateUserAsync(
                alphaOpponentId,
                "Alpha Current",
                "https://example.test/current-alpha.jpg"
            );
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/nemesis");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetNemesisHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var nemesis = Assert.IsType<NemesisHttpResponse>(result?.Nemesis);
        Assert.Equal(alphaOpponentId, nemesis.UserId);
        Assert.Equal("Alpha Current", nemesis.DisplayName);
        Assert.Equal("https://example.test/current-alpha.jpg", nemesis.ProfilePictureUrl);
        Assert.Equal(2, nemesis.Losses);
    }

    [Fact]
    public async Task GetNemesis_WhenOpponentIsInactive_ShouldReturnNullNemesis()
    {
        const string administratorId = "user_nemesis_former_admin";
        const string formerOpponentId = "user_nemesis_former_opponent";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: formerOpponentId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Opponent"
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: formerOpponentId,
                scores:
                [
                    (7, 11),
                    (8, 11),
                    (9, 11),
                ]
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/nemesis");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetNemesisHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Nemesis);
    }

    [Fact]
    public async Task GetNemesis_WhenLossesExistInAnotherCrew_ShouldUseOnlyActiveCrewMatchups()
    {
        const string administratorId = "user_nemesis_scope_admin";
        const string activeCrewOpponentId = "user_nemesis_scope_active";
        const string otherCrewOpponentId = "user_nemesis_scope_other";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: activeCrewOpponentId,
                displayName: "Active Crew Nemesis"
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: activeCrewOpponentId,
                scores:
                [
                    (8, 11),
                    (11, 7),
                    (11, 6),
                ]
            )
            .BuildAsync();
        await fixture.CreateCrewBuilder()
            .WithMember(id: administratorId)
            .WithMember(
                id: otherCrewOpponentId,
                displayName: "Other Crew Nemesis"
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: otherCrewOpponentId,
                scores:
                [
                    (5, 11),
                    (6, 11),
                    (7, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/nemesis");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetNemesisHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var nemesis = Assert.IsType<NemesisHttpResponse>(result?.Nemesis);
        Assert.Equal(activeCrewOpponentId, nemesis.UserId);
        Assert.Equal("Active Crew Nemesis", nemesis.DisplayName);
        Assert.Equal(1, nemesis.Losses);
    }
}
