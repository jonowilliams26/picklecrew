namespace PickleCrew.Api.Services;

internal sealed class CrewUserContext
{
    private long? crewId;
    private Guid? crewUuid;
    private string? organizationId;
    private string? role;
    private string? userId;

    public string UserId
    {
        get => userId ?? throw new InvalidOperationException(
            "The crew user context has not been initialized."
        );
        set => userId = value;
    }

    public string OrganizationId
    {
        get => organizationId ?? throw new InvalidOperationException(
            "The crew user context has not been initialized."
        );
        set => organizationId = value;
    }

    public string Role
    {
        get => role ?? throw new InvalidOperationException(
            "The crew user context has not been initialized."
        );
        set => role = value;
    }

    public long CrewId
    {
        get => crewId ?? throw new InvalidOperationException(
            "The crew user context has not been initialized."
        );
        set => crewId = value;
    }

    public Guid CrewUuid
    {
        get => crewUuid ?? throw new InvalidOperationException(
            "The crew user context has not been initialized."
        );
        set => crewUuid = value;
    }
}
