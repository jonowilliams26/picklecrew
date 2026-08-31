using PickleCrew.Api.Caching;
using Xunit;

namespace PickleCrew.UnitTests.Games;

public sealed class GamesCacheOptionTests
{
    private static readonly Guid crewId = Guid.Parse(
        "b8c88f49-d47b-461a-8887-4aca8810a0a1"
    );

    [Fact]
    public void GamesHistoryCacheOption_ShouldIncludeKeyAndTags()
    {
        ICacheOption option = new GamesHistoryCacheOption(
            crewId,
            Page: 2,
            PageSize: 25
        );

        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:games:history:page:2:page-size:25",
            option.Key
        );
        Assert.Equal(
            [
                "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1",
                "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:games",
            ],
            option.Tags.Select(tag => tag.Value)
        );
    }

    [Fact]
    public void CrewCacheTags_ShouldSupportCrewAndFeatureInvalidation()
    {
        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1",
            new CrewCacheTag(crewId).ToString()
        );
        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:games",
            new CrewGamesCacheTag(crewId).ToString()
        );
        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:statistics",
            new CrewStatisticsCacheTag(crewId).ToString()
        );
    }
}
