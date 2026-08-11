using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.IntegrationTests.EndpointFilters;

public sealed class WorkOsUserContextEndpointFilterTests
{
    [Fact]
    public async Task InvokeAsync_WhenSubjectClaimIsMissing_ShouldReturnUnauthorized()
    {
        var workOsUserContext = new WorkOsUserContext();
        var filter = new WorkOsUserContextEndpointFilter(workOsUserContext);
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
    public async Task InvokeAsync_WhenSubjectClaimExists_ShouldSetUserContext()
    {
        const string userId = "user_123";
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(WorkOsClaimTypes.Subject, userId)], "Test")
            ),
        };
        var workOsUserContext = new WorkOsUserContext();
        var filter = new WorkOsUserContextEndpointFilter(workOsUserContext);
        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        await filter.InvokeAsync(
            invocationContext,
            _ => ValueTask.FromResult<object?>(null)
        );

        Assert.Equal(userId, workOsUserContext.UserId);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
