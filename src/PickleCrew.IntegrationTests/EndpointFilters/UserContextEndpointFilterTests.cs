using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.IntegrationTests.EndpointFilters;

public sealed class UserContextEndpointFilterTests
{
    [Fact]
    public async Task InvokeAsync_WhenSubjectClaimIsMissing_ShouldReturnUnauthorized()
    {
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(userContext);
        var invocationContext = new TestEndpointFilterInvocationContext(
            new DefaultHttpContext()
        );

        var result = await filter.InvokeAsync(
            invocationContext,
            _ => ValueTask.FromResult<object?>(null)
        );

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task InvokeAsync_WhenOnlySubjectClaimExists_ShouldSetUserIdAndContinue()
    {
        const string userId = "user_123";
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(WorkOsClaimTypes.Subject, userId)], "Test")
            ),
        };
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(userContext);
        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);
        var handlerResult = new object();

        var result = await filter.InvokeAsync(
            invocationContext,
            _ => ValueTask.FromResult<object?>(handlerResult)
        );

        Assert.Same(handlerResult, result);
        Assert.Equal(userId, userContext.UserId);
        Assert.Null(userContext.OrganizationId);
        Assert.Null(userContext.Role);
    }

    [Fact]
    public async Task InvokeAsync_WhenOptionalClaimsExist_ShouldSetUserContext()
    {
        const string organizationId = "org_123";
        const string role = "admin";
        const string userId = "user_123";
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(WorkOsClaimTypes.OrganizationId, organizationId),
                        new Claim(WorkOsClaimTypes.Role, role),
                        new Claim(WorkOsClaimTypes.Subject, userId),
                    ],
                    "Test"
                )
            ),
        };
        var userContext = new UserContext();
        var filter = new UserContextEndpointFilter(userContext);
        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        await filter.InvokeAsync(
            invocationContext,
            _ => ValueTask.FromResult<object?>(null)
        );

        Assert.Equal(userId, userContext.UserId);
        Assert.Equal(organizationId, userContext.OrganizationId);
        Assert.Equal(role, userContext.Role);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
