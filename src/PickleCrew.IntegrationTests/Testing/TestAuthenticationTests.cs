using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Data;
using PickleCrew.Api.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Testing;

[Collection(nameof(AppHostCollection))]
public sealed class TestAuthenticationTests(AppHostFixture fixture)
{
    [Fact]
    public async Task UserContext_WhenOnlySubjectClaimIsSupplied_ShouldAllowRequest()
    {
        var identity = TestIdentity.CreateSubjectOnly();
        using var client = fixture.CreateApiClient(identity);

        using var response = await client.GetAsync("/testing/user-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var context = await response.Content.ReadFromJsonAsync<TestUserContextHttpResponse>();

        Assert.NotNull(context);
        Assert.Equal(identity.UserId, context.UserId);
        Assert.Null(context.OrganizationId);
        Assert.Null(context.Role);
    }

    [Fact]
    public async Task CrewUserContext_WhenOrganizationAndRoleClaimsAreMissing_ShouldForbidRequest()
    {
        var identity = TestIdentity.CreateSubjectOnly();
        using var client = fixture.CreateApiClient(identity);

        using var response = await client.GetAsync("/testing/crew-user-context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CrewUserContext_WhenRequiredClaimsAreSupplied_ShouldPropagateContext()
    {
        var identity = TestIdentity.Create(role: "admin");
        await using var dbContext = fixture.CreateDbContext();
        var crew = new Crew
        {
            Name = "Policy Test Crew",
            WorkOsOrganizationId = identity.OrganizationId!,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var provisioningAttempt = new CrewProvisioningAttempt
        {
            CrewUuid = crew.Uuid,
            CrewName = crew.Name,
            WorkOsUserId = identity.UserId,
            WorkOsOrganizationId = crew.WorkOsOrganizationId,
            WorkOsMembershipId = $"om_{Guid.NewGuid():N}",
            StartedAtUtc = crew.CreatedAtUtc,
            CompletedAtUtc = crew.CreatedAtUtc,
        };
        dbContext.CrewProvisioningAttempts.Add(provisioningAttempt);
        dbContext.Crews.Add(crew);
        await dbContext.SaveChangesAsync();
        using var client = fixture.CreateApiClient(identity);

        using var response = await client.GetAsync("/testing/crew-user-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var context = await response.Content
            .ReadFromJsonAsync<TestCrewUserContextHttpResponse>();

        Assert.NotNull(context);
        Assert.Equal(identity.UserId, context.UserId);
        Assert.Equal(identity.OrganizationId, context.OrganizationId);
        Assert.Equal(identity.Role, context.Role);
        Assert.Equal(crew.Id, context.CrewId);
        Assert.Equal(crew.Uuid, context.CrewUuid);
    }
}
