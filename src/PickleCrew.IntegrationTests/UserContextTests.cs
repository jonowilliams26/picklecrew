using PickleCrew.Api.Services;
using Xunit;

namespace PickleCrew.IntegrationTests;

public sealed class UserContextTests
{
    [Fact]
    public void Properties_WhenContextIsNotInitialized_ShouldThrowInvalidOperationException()
    {
        var userContext = new UserContext();

        Assert.Throws<InvalidOperationException>(() => userContext.UserId);
        Assert.Throws<InvalidOperationException>(() => userContext.CrewId);
        Assert.Throws<InvalidOperationException>(() => userContext.CrewUuid);
        Assert.Throws<InvalidOperationException>(() => userContext.Role);
    }
}
