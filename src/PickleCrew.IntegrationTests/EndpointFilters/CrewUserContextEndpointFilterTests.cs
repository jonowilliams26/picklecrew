using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Data;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace PickleCrew.IntegrationTests.EndpointFilters;

[Collection(nameof(AppHostCollection))]
public sealed class CrewUserContextEndpointFilterTests(AppHostFixture fixture)
{
    [Theory]
    [InlineData(null, "org_123", "admin")]
    [InlineData("user_123", null, "admin")]
    [InlineData("user_123", "org_123", null)]
    public async Task InvokeAsync_WhenRequiredClaimIsMissing_ShouldReturnUnauthorized(
        string? userId,
        string? organizationId,
        string? role)
    {
        await using var dbContext = fixture.CreateDbContext();
        var userContext = new UserContext();
        var crewUserContext = new CrewUserContext();
        using var fusionCache = new FusionCache(new FusionCacheOptions());
        var filter = new CrewUserContextEndpointFilter(
            userContext,
            crewUserContext,
            dbContext,
            fusionCache
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            CreateHttpContext(userId, organizationId, role)
        );
        var handlerCalled = false;

        var result = await filter.InvokeAsync(
            invocationContext,
            _ =>
            {
                handlerCalled = true;
                return ValueTask.FromResult<object?>(null);
            }
        );

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenOrganizationIsNotMapped_ShouldReturnForbidden()
    {
        await using var dbContext = fixture.CreateDbContext();
        var userContext = new UserContext();
        var crewUserContext = new CrewUserContext();
        using var fusionCache = new FusionCache(new FusionCacheOptions());
        var filter = new CrewUserContextEndpointFilter(
            userContext,
            crewUserContext,
            dbContext,
            fusionCache
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            CreateHttpContext("user_123", $"org_{Guid.NewGuid():N}", "admin")
        );
        var handlerCalled = false;

        var result = await filter.InvokeAsync(
            invocationContext,
            _ =>
            {
                handlerCalled = true;
                return ValueTask.FromResult<object?>(null);
            }
        );

        Assert.IsType<ForbidHttpResult>(result);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenCrewContextIsValid_ShouldInitializeContextBeforeHandler()
    {
        const string role = "admin";
        var userId = $"user_{Guid.NewGuid():N}";
        await using var dbContext = fixture.CreateDbContext();
        var crew = new Crew
        {
            Name = "Kitchen Legends",
            WorkOsOrganizationId = $"org_{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var provisioningAttempt = new CrewProvisioningAttempt
        {
            CrewUuid = crew.Uuid,
            RequestId = Guid.NewGuid(),
            CrewName = crew.Name,
            WorkOsUserId = userId,
            WorkOsOrganizationId = crew.WorkOsOrganizationId,
            WorkOsMembershipId = $"om_{Guid.NewGuid():N}",
            StartedAtUtc = crew.CreatedAtUtc,
            CompletedAtUtc = crew.CreatedAtUtc,
        };
        dbContext.CrewProvisioningAttempts.Add(provisioningAttempt);
        dbContext.Crews.Add(crew);
        await dbContext.SaveChangesAsync();

        var userContext = new UserContext();
        var crewUserContext = new CrewUserContext();
        using var fusionCache = new FusionCache(new FusionCacheOptions());
        var filter = new CrewUserContextEndpointFilter(
            userContext,
            crewUserContext,
            dbContext,
            fusionCache
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            CreateHttpContext(userId, crew.WorkOsOrganizationId, role)
        );
        var handlerResult = new object();

        var result = await filter.InvokeAsync(
            invocationContext,
            _ =>
            {
                Assert.Equal(userId, userContext.UserId);
                Assert.Equal(crew.WorkOsOrganizationId, userContext.OrganizationId);
                Assert.Equal(role, userContext.Role);

                Assert.Equal(userId, crewUserContext.UserId);
                Assert.Equal(crew.WorkOsOrganizationId, crewUserContext.OrganizationId);
                Assert.Equal(role, crewUserContext.Role);
                Assert.Equal(crew.Id, crewUserContext.CrewId);
                Assert.Equal(crew.Uuid, crewUserContext.CrewUuid);

                return ValueTask.FromResult<object?>(handlerResult);
            }
        );

        Assert.Same(handlerResult, result);
    }

    [Fact]
    public async Task InvokeAsync_WhenCrewMappingIsCached_ShouldUseCachedCrewIdentifiers()
    {
        const string role = "admin";
        var userId = $"user_{Guid.NewGuid():N}";
        await using var dbContext = fixture.CreateDbContext();
        var crew = new Crew
        {
            Name = "Cached Crew",
            WorkOsOrganizationId = $"org_{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var provisioningAttempt = new CrewProvisioningAttempt
        {
            CrewUuid = crew.Uuid,
            RequestId = Guid.NewGuid(),
            CrewName = crew.Name,
            WorkOsUserId = userId,
            WorkOsOrganizationId = crew.WorkOsOrganizationId,
            WorkOsMembershipId = $"om_{Guid.NewGuid():N}",
            StartedAtUtc = crew.CreatedAtUtc,
            CompletedAtUtc = crew.CreatedAtUtc,
        };
        dbContext.CrewProvisioningAttempts.Add(provisioningAttempt);
        dbContext.Crews.Add(crew);
        await dbContext.SaveChangesAsync();

        using var fusionCache = new FusionCache(new FusionCacheOptions());
        var firstFilter = new CrewUserContextEndpointFilter(
            new UserContext(),
            new CrewUserContext(),
            dbContext,
            fusionCache
        );
        var firstInvocationContext = new TestEndpointFilterInvocationContext(
            CreateHttpContext(userId, crew.WorkOsOrganizationId, role)
        );

        await firstFilter.InvokeAsync(
            firstInvocationContext,
            _ => ValueTask.FromResult<object?>(null)
        );

        dbContext.Crews.Remove(crew);
        await dbContext.SaveChangesAsync();

        var cachedCrewUserContext = new CrewUserContext();
        var secondFilter = new CrewUserContextEndpointFilter(
            new UserContext(),
            cachedCrewUserContext,
            dbContext,
            fusionCache
        );
        var secondInvocationContext = new TestEndpointFilterInvocationContext(
            CreateHttpContext(userId, crew.WorkOsOrganizationId, role)
        );
        var handlerCalled = false;

        await secondFilter.InvokeAsync(
            secondInvocationContext,
            _ =>
            {
                handlerCalled = true;
                Assert.Equal(crew.Id, cachedCrewUserContext.CrewId);
                Assert.Equal(crew.Uuid, cachedCrewUserContext.CrewUuid);

                return ValueTask.FromResult<object?>(null);
            }
        );

        Assert.True(handlerCalled);
    }

    private static DefaultHttpContext CreateHttpContext(
        string? userId,
        string? organizationId,
        string? role)
    {
        var claims = new List<Claim>();
        AddClaim(claims, WorkOsClaimTypes.Subject, userId);
        AddClaim(claims, WorkOsClaimTypes.OrganizationId, organizationId);
        AddClaim(claims, WorkOsClaimTypes.Role, role);

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
        };
    }

    private static void AddClaim(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
