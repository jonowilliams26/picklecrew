using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Onboarding;
using Xunit;

namespace PickleCrew.IntegrationTests.Onboarding;

[Collection(nameof(AppHostCollection))]
public sealed class CreateCrewEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task CreateCrew_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        using var response = await fixture.ApiClient.PostAsJsonAsync(
            "/onboarding/crew",
            new { name = "Kitchen Legends" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCrew_WhenRequestIsValid_ShouldProvisionAndPersistCrew()
    {
        var userId = $"user_{Guid.NewGuid():N}";
        using var apiClient = fixture.CreateApiClient(userId);

        using var response = await apiClient.PostAsJsonAsync(
            "/onboarding/crew",
            new CreateCrewHttpRequest { Name = "  Kitchen Legends  " }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdCrew = await response.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        Assert.NotNull(createdCrew);
        Assert.Equal("Kitchen Legends", createdCrew.Name);
        Assert.Equal($"/crews/{createdCrew.CrewId}", response.Headers.Location?.ToString());

        await using var dbContext = fixture.CreateDbContext();
        var storedCrew = await dbContext.Crews
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Uuid == createdCrew.CrewId);
        var storedAttempt = await dbContext.CrewProvisioningAttempts
            .AsNoTracking()
            .SingleAsync(candidate => candidate.CrewUuid == createdCrew.CrewId);

        Assert.NotEqual(0, storedCrew.Id);
        Assert.Equal(createdCrew.CrewId, storedCrew.Uuid);
        Assert.Equal(createdCrew.Name, storedCrew.Name);
        Assert.Equal(createdCrew.OrganizationId, storedCrew.WorkOsOrganizationId);
        Assert.Equal(userId, storedAttempt.WorkOsUserId);
        Assert.Equal(createdCrew.OrganizationId, storedAttempt.WorkOsOrganizationId);
        Assert.False(string.IsNullOrWhiteSpace(storedAttempt.WorkOsMembershipId));
        Assert.NotNull(storedAttempt.CompletedAtUtc);
    }
}
