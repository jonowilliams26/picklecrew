namespace PickleCrew.Api.Caching;

internal interface ICacheTag
{
    string Value { get; }
}

internal readonly record struct CrewCacheTag(Guid CrewId) : ICacheTag
{
    public string Value => $"v1:crew:{CrewId}";

    public override string ToString()
    {
        return Value;
    }
}

internal readonly record struct CrewGamesCacheTag(Guid CrewId) : ICacheTag
{
    public string Value => $"v1:crew:{CrewId}:games";

    public override string ToString()
    {
        return Value;
    }
}

internal readonly record struct CrewStatisticsCacheTag(Guid CrewId) : ICacheTag
{
    public string Value => $"v1:crew:{CrewId}:statistics";

    public override string ToString()
    {
        return Value;
    }
}
