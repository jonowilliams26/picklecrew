using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetEasyWinEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetEasyWin_WhenMemberHasNotDefeatedAnotherMember_ShouldReturnNullEasyWin()
    {
        const string administratorId = "user_easy_win_none_admin";
        const string memberId = "user_easy_win_none_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (administratorId, 7),
                side2: (memberId, 11)
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Losing Guest", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/easy-win");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetEasyWinHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.EasyWin);
    }

    [Fact]
    public async Task GetEasyWin_WhenOpponentHasPlayedFewerThanThreeGames_ShouldReturnNullEasyWin()
    {
        const string administratorId = "user_easy_win_minimum_admin";
        const string opponentId = "user_easy_win_minimum_opponent";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: opponentId)
            .WithSinglesGames(
                side1: administratorId,
                side2: opponentId,
                scores:
                [
                    (11, 7),
                    (11, 8),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/easy-win");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetEasyWinHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.EasyWin);
    }

    [Fact]
    public async Task GetEasyWin_WhenQualifyingDoublesGamesContainGuests_ShouldReturnMemberOpponent()
    {
        const string administratorId = "user_easy_win_guest_admin";
        const string opponentId = "user_easy_win_guest_opponent";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: opponentId, displayName: "Member Opponent")
            .WithDoublesGames(
                side1: (administratorId, "Guest Partner"),
                side2: (opponentId, "Guest Opponent"),
                scores:
                [
                    (11, 7),
                    (8, 11),
                    (11, 9),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/easy-win");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetEasyWinHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var easyWin = Assert.IsType<EasyWinHttpResponse>(result?.EasyWin);
        Assert.Equal(opponentId, easyWin.UserId);
        Assert.Equal("Member Opponent", easyWin.DisplayName);
        Assert.Equal(2, easyWin.Wins);
    }

    [Fact]
    public async Task GetEasyWin_WhenOpponentsAreTied_ShouldReturnFirstByUserIdWithCurrentProfile()
    {
        const string administratorId = "user_easy_win_tie_admin";
        const string alphaOpponentId = "user_easy_win_tie_alpha_id";
        const string zuluOpponentId = "user_easy_win_tie_zulu_id";
        const string lowerOpponentId = "user_easy_win_tie_lower";
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
                    (11, 7),
                    (11, 8),
                    (6, 11),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: zuluOpponentId,
                scores:
                [
                    (11, 6),
                    (11, 9),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: lowerOpponentId,
                scores:
                [
                    (11, 7),
                    (7, 11),
                    (8, 11),
                ]
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
        using var response = await apiClient.GetAsync("/statistics/easy-win");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetEasyWinHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var easyWin = Assert.IsType<EasyWinHttpResponse>(result?.EasyWin);
        Assert.Equal(alphaOpponentId, easyWin.UserId);
        Assert.Equal("Alpha Current", easyWin.DisplayName);
        Assert.Equal("https://example.test/current-alpha.jpg", easyWin.ProfilePictureUrl);
        Assert.Equal(2, easyWin.Wins);
    }

    [Fact]
    public async Task GetEasyWin_WhenOpponentIsInactive_ShouldReturnNullEasyWin()
    {
        const string administratorId = "user_easy_win_former_admin";
        const string formerOpponentId = "user_easy_win_former_opponent";
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
                    (11, 7),
                    (11, 8),
                    (11, 9),
                ]
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/easy-win");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetEasyWinHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.EasyWin);
    }

    [Fact]
    public async Task GetEasyWin_WhenWinsExistInAnotherCrew_ShouldUseOnlyActiveCrewMatchups()
    {
        const string administratorId = "user_easy_win_scope_admin";
        const string activeCrewOpponentId = "user_easy_win_scope_active";
        const string otherCrewOpponentId = "user_easy_win_scope_other";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: activeCrewOpponentId,
                displayName: "Active Crew Easy Win"
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: activeCrewOpponentId,
                scores:
                [
                    (11, 8),
                    (7, 11),
                    (6, 11),
                ]
            )
            .BuildAsync();
        await fixture.CreateCrewBuilder()
            .WithMember(id: administratorId)
            .WithMember(
                id: otherCrewOpponentId,
                displayName: "Other Crew Easy Win"
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: otherCrewOpponentId,
                scores:
                [
                    (11, 5),
                    (11, 6),
                    (11, 7),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/easy-win");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetEasyWinHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var easyWin = Assert.IsType<EasyWinHttpResponse>(result?.EasyWin);
        Assert.Equal(activeCrewOpponentId, easyWin.UserId);
        Assert.Equal("Active Crew Easy Win", easyWin.DisplayName);
        Assert.Equal(1, easyWin.Wins);
    }
}
