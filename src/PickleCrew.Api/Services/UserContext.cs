namespace PickleCrew.Api.Services;

internal sealed class UserContext
{
    private string? userId;

    public string UserId
    {
        get => userId ?? throw new InvalidOperationException(
            "The user context has not been initialized."
        );
        set => userId = value;
    }

    public string? OrganizationId { get; set; }

    public string? Role { get; set; }
}
