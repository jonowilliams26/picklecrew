using PickleCrew.Api.Caching;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class StatisticsCacheOptionTests
{
    private static readonly Guid crewId = Guid.Parse(
        "b8c88f49-d47b-461a-8887-4aca8810a0a1"
    );

    [Fact]
    public void CrewStatisticsCacheOption_ShouldIncludeKeyAndTags()
    {
        ICacheOption option = new CrewStatisticsCacheOption(
            crewId,
            CrewStatisticsCacheResource.CrewLeaderboard
        );

        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:statistics:crew-leaderboard",
            option.Key
        );
        Assert.Equal(
            [
                "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1",
                "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:statistics",
            ],
            option.Tags.Select(tag => tag.Value)
        );
    }

    [Fact]
    public void CrewHealthCacheOption_WhenCreated_ShouldIncludeEndDateInKey()
    {
        ICacheOption option = new CrewHealthCacheOption(
            crewId,
            new DateOnly(2026, 3, 15)
        );

        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:statistics:crew-health:end-date:2026-03-15",
            option.Key
        );
        Assert.Equal(
            [
                "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1",
                "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:statistics",
            ],
            option.Tags.Select(tag => tag.Value)
        );
    }

    [Fact]
    public void PersonalStatisticsCacheOption_ShouldIncludeRequiredUserInKey()
    {
        ICacheOption option = new PersonalStatisticsCacheOption(
            crewId,
            "user_test_123",
            PersonalStatisticsCacheResource.PersonalStatistics
        );

        Assert.Equal(
            "v1:crew:b8c88f49-d47b-461a-8887-4aca8810a0a1:statistics:user:user_test_123:personal-statistics",
            option.Key
        );
    }

    [Fact]
    public void StatisticsCacheResources_ShouldHaveUniqueCacheKeys()
    {
        var crewResources = Enum.GetValues<CrewStatisticsCacheResource>();
        var personalResources = Enum.GetValues<PersonalStatisticsCacheResource>();
        var keys = crewResources
            .Select(resource => resource.ToCacheKey())
            .Concat(personalResources.Select(resource => resource.ToCacheKey()))
            .ToArray();

        Assert.Equal(
            crewResources.Length + personalResources.Length,
            keys.Distinct(StringComparer.Ordinal).Count()
        );
    }
}
