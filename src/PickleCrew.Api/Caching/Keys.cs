namespace PickleCrew.Api.Caching;

internal interface ICacheOption
{
    string Key { get; }

    ICacheTag[] Tags { get; }
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
