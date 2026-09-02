using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class ChangeCrewNameEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task ChangeCrewName_WhenCallerIsAdministrator_ShouldUpdateCrewAndWorkOsOrganization()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PatchAsJsonAsync(
            "/crews/name",
            new ChangeCrewNameHttpRequest { Name = "  Dink Dynasty  " },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ChangeCrewNameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.Equal("Dink Dynasty", result?.Name);

        await using var database = fixture.CreateDbContext();
        var databaseName = await database.Crews
            .Where(crew => crew.Id == context.Id)
            .Select(crew => crew.Name)
            .SingleAsync();
        Assert.Equal("Dink Dynasty", databaseName);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var organization = await fakeWorkOs.GetOrganizationAsync(
            context.OrganizationId
        );
        Assert.Equal("Dink Dynasty", organization.Name);
    }

    [Fact]
    public async Task ChangeCrewName_WhenCallerIsNotAdministrator_ShouldReturnForbiddenAndKeepName()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithMember()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Members[0]);

        using var response = await apiClient.PatchAsJsonAsync(
            "/crews/name",
            new ChangeCrewNameHttpRequest { Name = "Dink Dynasty" },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        var databaseName = await database.Crews
            .Where(crew => crew.Id == context.Id)
            .Select(crew => crew.Name)
            .SingleAsync();
        Assert.Equal("Kitchen Legends", databaseName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChangeCrewName_WhenNameIsBlank_ShouldReturnBadRequestAndKeepName(
        string name)
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PatchAsJsonAsync(
            "/crews/name",
            new ChangeCrewNameHttpRequest { Name = name },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        var databaseName = await database.Crews
            .Where(crew => crew.Id == context.Id)
            .Select(crew => crew.Name)
            .SingleAsync();
        Assert.Equal("Kitchen Legends", databaseName);
    }

    [Fact]
    public async Task ChangeCrewName_WhenWorkOsUpdateFails_ShouldReturnServerErrorAndKeepDatabaseName()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .BuildAsync();
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.FailOperationAsync(
            scenarioId,
            FakeWorkOsOperation.UpdateOrganization
        );
        using var apiClient = fixture.CreateApiClient(
            context.Administrator,
            scenarioId
        );

        using var response = await apiClient.PatchAsJsonAsync(
            "/crews/name",
            new ChangeCrewNameHttpRequest { Name = "Dink Dynasty" },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        var databaseName = await database.Crews
            .Where(crew => crew.Id == context.Id)
            .Select(crew => crew.Name)
            .SingleAsync();
        Assert.Equal("Kitchen Legends", databaseName);
    }
}
