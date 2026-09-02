namespace PickleCrew.Api.Caching;

internal interface ICacheOption
{
    string Key { get; }

    ICacheTag[] Tags { get; }
}

internal sealed record CrewUserContextCacheOption(string OrganizationId) : ICacheOption
{
    public string Key { get; } = $"v1:crew-user-context:{OrganizationId}";

    public ICacheTag[] Tags { get; } = [];
}

internal sealed record GamesHistoryCacheOption(
    Guid CrewId,
    int Page,
    int PageSize) : ICacheOption
{
    public string Key { get; } =
        $"v1:crew:{CrewId}:games:history:page:{Page}:page-size:{PageSize}";

    public ICacheTag[] Tags { get; } =
    [
        new CrewCacheTag(CrewId),
        new CrewGamesCacheTag(CrewId),
    ];
}

internal sealed record CrewStatisticsCacheOption(
    Guid CrewId,
    CrewStatisticsCacheResource Resource) : ICacheOption
{
    public string Key { get; } =
        $"v1:crew:{CrewId}:statistics:{Resource.ToCacheKey()}";

    public ICacheTag[] Tags { get; } =
    [
        new CrewCacheTag(CrewId),
        new CrewStatisticsCacheTag(CrewId),
    ];
}

internal sealed record PersonalStatisticsCacheOption(
    Guid CrewId,
    string UserId,
    PersonalStatisticsCacheResource Resource) : ICacheOption
{
    public string Key { get; } =
        $"v1:crew:{CrewId}:statistics:user:{UserId}:{Resource.ToCacheKey()}";

    public ICacheTag[] Tags { get; } =
    [
        new CrewCacheTag(CrewId),
        new CrewStatisticsCacheTag(CrewId),
    ];
}

internal enum CrewStatisticsCacheResource
{
    BestDoublesPairing,
    BiggestRivals,
    ChiefPaddler,
    CrewLeaderboard,
    CrewLongestWinStreak,
    CrewStandings,
    FriendsOrLovers,
    IceMan,
    LimpWristSyndrome,
}

internal enum PersonalStatisticsCacheResource
{
    BestPartner,
    Nemesis,
    PersonalStatistics,
    RecentGames,
}

internal static class StatisticsCacheResourceExtensions
{
    public static string ToCacheKey(this CrewStatisticsCacheResource resource)
    {
        return resource switch
        {
            CrewStatisticsCacheResource.BestDoublesPairing => "best-doubles-pairing",
            CrewStatisticsCacheResource.BiggestRivals => "biggest-rivals",
            CrewStatisticsCacheResource.ChiefPaddler => "chief-paddler",
            CrewStatisticsCacheResource.CrewLeaderboard => "crew-leaderboard",
            CrewStatisticsCacheResource.CrewLongestWinStreak => "crew-longest-win-streak",
            CrewStatisticsCacheResource.CrewStandings => "crew-standings",
            CrewStatisticsCacheResource.FriendsOrLovers => "friends-or-lovers",
            CrewStatisticsCacheResource.IceMan => "ice-man",
            CrewStatisticsCacheResource.LimpWristSyndrome => "limp-wrist-syndrome",
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null),
        };
    }

    public static string ToCacheKey(this PersonalStatisticsCacheResource resource)
    {
        return resource switch
        {
            PersonalStatisticsCacheResource.BestPartner => "best-partner",
            PersonalStatisticsCacheResource.Nemesis => "nemesis",
            PersonalStatisticsCacheResource.PersonalStatistics => "personal-statistics",
            PersonalStatisticsCacheResource.RecentGames => "recent-games",
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null),
        };
    }
}
