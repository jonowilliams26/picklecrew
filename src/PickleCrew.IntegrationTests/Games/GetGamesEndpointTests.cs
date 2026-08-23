using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Data;
using PickleCrew.Api.Games;
using PickleCrew.Api.Shared;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class GetGamesEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetGames_WhenCrewHasMoreThanOnePage_ShouldReturnNewestPageWithinCrew()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var crewBuilder = fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId);

        for (var index = 0; index < 11; index++)
        {
            crewBuilder.WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Guest Player", 7)
            );
        }

        var context = await crewBuilder.BuildAsync();
        var otherAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherAdministratorId)
            .WithSinglesGame(
                side1: (otherAdministratorId, 11),
                side2: ("Other Guest", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games?page=3&pageSize=5");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(3, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        var game = Assert.Single(result.Items);
        Assert.Equal(context.Games[0].PlayedAtUtc, game.PlayedAtUtc);
        Assert.Equal(
            context.Administrator.EmailAddress,
            game.Sides[0].Participants[0].DisplayName
        );
    }

    [Fact]
    public async Task GetGames_WhenGameIncludesMembersAndGuests_ShouldReturnCompleteResult()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId, displayName: "Taylor Lob")
            .WithDoublesGame(
                side1: (administratorId, "Jamie Kitchen", 13),
                side2: (memberId, "Casey Court", 11)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        var returnedGame = Assert.Single(result.Items);
        Assert.Equal(GameFormat.Doubles, returnedGame.Format);
        Assert.Equal(13, returnedGame.Sides[0].Score);
        Assert.Equal(11, returnedGame.Sides[1].Score);
        Assert.Collection(
            returnedGame.Sides[0].Participants,
            participant =>
            {
                Assert.Equal(
                    context.Administrator.EmailAddress,
                    participant.DisplayName
                );
                Assert.False(participant.IsGuest);
            },
            participant =>
            {
                Assert.Equal("Jamie Kitchen", participant.DisplayName);
                Assert.True(participant.IsGuest);
            }
        );
        Assert.Collection(
            returnedGame.Sides[1].Participants,
            participant =>
            {
                Assert.Equal("Taylor Lob", participant.DisplayName);
                Assert.False(participant.IsGuest);
            },
            participant =>
            {
                Assert.Equal("Casey Court", participant.DisplayName);
                Assert.True(participant.IsGuest);
            }
        );
    }

    [Fact]
    public async Task GetGames_WhenMemberHasProfilePicture_ShouldReturnProfilePicture()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        const string profilePictureUrl = "https://example.test/player.jpg";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
                displayName: "Morgan Dink",
                profilePictureUrl: profilePictureUrl
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Guest Player", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        var game = Assert.Single(result.Items);
        var participant = Assert.Single(game.Sides[0].Participants);
        Assert.Equal("Morgan Dink", participant.DisplayName);
        Assert.Equal(profilePictureUrl, participant.ProfilePictureUrl);
    }

    [Fact]
    public async Task GetGames_WhenPaginationIsInvalid_ShouldReturnValidationProblem()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games?page=0&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Page must be at least 1", content, StringComparison.Ordinal);
        Assert.Contains(
            "Page size must be between 1 and 100",
            content,
            StringComparison.Ordinal
        );
    }
}
