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
}

internal static class WorkOsClaimTypes
{
    public const string Subject = "sub";
}
