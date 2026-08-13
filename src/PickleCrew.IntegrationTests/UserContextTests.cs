using PickleCrew.Api.Services;
using Xunit;

namespace PickleCrew.IntegrationTests;

public sealed class UserContextTests
{
    [Fact]
    public void Properties_WhenContextIsNotInitialized_ShouldRequireOnlyUserId()
    {
        var userContext = new UserContext();

        Assert.Throws<InvalidOperationException>(() => userContext.UserId);
        Assert.Null(userContext.OrganizationId);
        Assert.Null(userContext.Role);
    }
}

public sealed class CrewUserContextTests
{
    [Fact]
    public void Properties_WhenContextIsNotInitialized_ShouldThrowInvalidOperationException()
    {
        var crewUserContext = new CrewUserContext();

        Assert.Throws<InvalidOperationException>(() => crewUserContext.UserId);
        Assert.Throws<InvalidOperationException>(() => crewUserContext.OrganizationId);
        Assert.Throws<InvalidOperationException>(() => crewUserContext.Role);
        Assert.Throws<InvalidOperationException>(() => crewUserContext.CrewId);
        Assert.Throws<InvalidOperationException>(() => crewUserContext.CrewUuid);
    }
}
