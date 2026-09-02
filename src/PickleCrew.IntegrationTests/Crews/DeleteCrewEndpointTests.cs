using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class DeleteCrewEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task DeleteCrew_WhenAdministratorConfirmsCrewName_ShouldPermanentlyDeleteCrew()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var memberOneId = $"user_test_{Guid.NewGuid():N}";
        var memberTwoId = $"user_test_{Guid.NewGuid():N}";
        var memberThreeId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberOneId)
            .WithMember(id: memberTwoId)
            .WithMember(id: memberThreeId)
            .WithDoublesGame(
                side1: (administratorId, memberOneId, 11),
                side2: (memberTwoId, memberThreeId, 5)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var contextResponse = await apiClient.GetAsync(
            "/testing/crew-user-context"
        );
        Assert.Equal(HttpStatusCode.OK, contextResponse.StatusCode);

        using var response = await DeleteCrewAsync(
            apiClient,
            "Kitchen Legends"
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var forbiddenResponse = await apiClient.GetAsync(
            "/testing/crew-user-context"
        );
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var organizationId = await fakeWorkOs.FindOrganizationByExternalIdAsync(
            context.Uuid.ToString()
        );
        Assert.Null(organizationId);

        await using var database = fixture.CreateDbContext();
        Assert.False(await database.Crews.AnyAsync(crew => crew.Id == context.Id));
        Assert.False(await database.CrewProvisioningAttempts.AnyAsync(
            attempt => attempt.CrewUuid == context.Uuid
        ));
        Assert.False(await database.Games.AnyAsync(game => game.CrewId == context.Id));
        Assert.False(await database.PlayerStatistics.AnyAsync(
            statistics => statistics.CrewId == context.Id
        ));
        Assert.False(await database.MatchupStatistics.AnyAsync(
            statistics => statistics.CrewId == context.Id
        ));
        Assert.False(await database.DoublesPairingStatistics.AnyAsync(
            statistics => statistics.CrewId == context.Id
        ));
        Assert.False(await database.PlayerWinStreaks.AnyAsync(
            winStreak => winStreak.CrewId == context.Id
        ));
    }

    [Fact]
    public async Task DeleteCrew_WhenWorkOsOrganizationWasAlreadyDeleted_ShouldDeleteLocalCrew()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .BuildAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.DeleteOrganizationAsync(context.OrganizationId);
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await DeleteCrewAsync(
            apiClient,
            "Kitchen Legends"
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        Assert.False(await database.Crews.AnyAsync(crew => crew.Id == context.Id));
    }

    [Fact]
    public async Task DeleteCrew_WhenCallerIsNotAdministrator_ShouldReturnForbiddenAndPreserveCrew()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithMember()
            .BuildAsync();
        var member = Assert.Single(context.Members);
        using var apiClient = fixture.CreateApiClient(member);

        using var response = await DeleteCrewAsync(
            apiClient,
            "Kitchen Legends"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Crews.AnyAsync(crew => crew.Id == context.Id));
    }

    [Fact]
    public async Task DeleteCrew_WhenCrewNameDoesNotMatchExactly_ShouldReturnBadRequestAndPreserveCrew()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await DeleteCrewAsync(
            apiClient,
            "kitchen legends"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var organizationId = await fakeWorkOs.FindOrganizationByExternalIdAsync(
            context.Uuid.ToString()
        );
        Assert.Equal(context.OrganizationId, organizationId);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Crews.AnyAsync(crew => crew.Id == context.Id));
    }

    [Fact]
    public async Task DeleteCrew_WhenWorkOsIsUnavailable_ShouldReturnServerErrorAndPreserveCrew()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .BuildAsync();
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.FailOperationAsync(
            scenarioId,
            FakeWorkOsOperation.DeleteOrganization
        );
        using var apiClient = fixture.CreateApiClient(
            context.Administrator,
            scenarioId
        );

        using var response = await DeleteCrewAsync(
            apiClient,
            "Kitchen Legends"
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            context.OrganizationId,
            await fakeWorkOs.FindOrganizationByExternalIdAsync(context.Uuid.ToString())
        );

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Crews.AnyAsync(crew => crew.Id == context.Id));
    }

    private static async Task<HttpResponseMessage> DeleteCrewAsync(
        HttpClient client,
        string crewName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/crews")
        {
            Content = JsonContent.Create(new DeleteCrewHttpRequest
            {
                CrewName = crewName,
            }),
        };

        return await client.SendAsync(request);
    }
}
