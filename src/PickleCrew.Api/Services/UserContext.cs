namespace PickleCrew.Api.Services;

internal sealed class UserContext
{
    private long? crewId;
    private Guid? crewUuid;
    private string? role;
    private string? userId;

    public string UserId
    {
        get => userId ?? throw new InvalidOperationException(
            "The user context has not been initialized."
        );
        set => userId = value;
    }

    public long CrewId
    {
        get => crewId ?? throw new InvalidOperationException(
            "The user context has not been initialized."
        );
        set => crewId = value;
    }

    public Guid CrewUuid
    {
        get => crewUuid ?? throw new InvalidOperationException(
            "The user context has not been initialized."
        );
        set => crewUuid = value;
    }

    public string Role
    {
        get => role ?? throw new InvalidOperationException(
            "The user context has not been initialized."
        );
        set => role = value;
    }
}
