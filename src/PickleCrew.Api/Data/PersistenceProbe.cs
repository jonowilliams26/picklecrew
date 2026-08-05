namespace PickleCrew.Api.Data;

public sealed class PersistenceProbe
{
    public long Id { get; private set; }

    public required string Value { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
