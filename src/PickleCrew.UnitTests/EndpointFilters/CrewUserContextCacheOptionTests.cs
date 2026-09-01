using PickleCrew.Api.Caching;
using Xunit;

namespace PickleCrew.UnitTests.EndpointFilters;

public sealed class CrewUserContextCacheOptionTests
{
    [Fact]
    public void CrewUserContextCacheOption_WhenCreated_ShouldIncludeOrganizationIdInKey()
    {
        const string organizationId = "org_01JEXAMPLE";

        ICacheOption option = new CrewUserContextCacheOption(organizationId);

        Assert.Equal("v1:crew-user-context:org_01JEXAMPLE", option.Key);
        Assert.Empty(option.Tags);
    }
}
