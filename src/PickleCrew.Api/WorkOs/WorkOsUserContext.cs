namespace PickleCrew.Api.WorkOs;

internal sealed class WorkOsUserContext
{
    private string? userId;

    public string UserId
    {
        get => userId ?? throw new InvalidOperationException(
            "The WorkOS user context has not been initialized."
        );
        set => userId = value;
    }

    public string? OrganizationId { get; set; }

    public string? Role { get; set; }
}

internal static class WorkOsClaimTypes
{
    public const string OrganizationId = "org_id";

    public const string Role = "role";

    public const string Subject = "sub";
}
