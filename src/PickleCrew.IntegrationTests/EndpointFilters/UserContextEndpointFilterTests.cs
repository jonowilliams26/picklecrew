using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Data;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.IntegrationTests.EndpointFilters;

[Collection(nameof(AppHostCollection))]
public sealed class UserContextEndpointFilterTests(AppHostFixture fixture)
{
    [Fact]
    public async Task InvokeAsync_WhenOrganizationClaimIsMissing_ShouldReturnUnauthorized()
    {
        await using var dbContext = fixture.CreateDbContext();
        var workOsUserContext = new WorkOsUserContext
        {
            UserId = "user_123",
            Role = "admin",
        };
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(
            workOsUserContext,
            userContext,
            dbContext
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            new DefaultHttpContext()
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
    public async Task InvokeAsync_WhenRoleClaimIsMissing_ShouldReturnUnauthorized()
    {
        await using var dbContext = fixture.CreateDbContext();
        var workOsUserContext = new WorkOsUserContext
        {
            UserId = "user_123",
            OrganizationId = "org_123",
        };
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(
            workOsUserContext,
            userContext,
            dbContext
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            new DefaultHttpContext()
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
        var workOsUserContext = new WorkOsUserContext
        {
            UserId = "user_123",
            OrganizationId = $"org_{Guid.NewGuid():N}",
            Role = "admin",
        };
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(
            workOsUserContext,
            userContext,
            dbContext
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            new DefaultHttpContext()
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

        var workOsUserContext = new WorkOsUserContext
        {
            UserId = userId,
            OrganizationId = crew.WorkOsOrganizationId,
            Role = role,
        };
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(
            workOsUserContext,
            userContext,
            dbContext
        );
        var invocationContext = new TestEndpointFilterInvocationContext(
            new DefaultHttpContext()
        );
        var handlerResult = new object();

        var result = await filter.InvokeAsync(
            invocationContext,
            _ =>
            {
                Assert.Equal(userId, userContext.UserId);
                Assert.Equal(crew.Id, userContext.CrewId);
                Assert.Equal(crew.Uuid, userContext.CrewUuid);
                Assert.Equal(role, userContext.Role);

                return ValueTask.FromResult<object?>(handlerResult);
            }
        );

        Assert.Same(handlerResult, result);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
