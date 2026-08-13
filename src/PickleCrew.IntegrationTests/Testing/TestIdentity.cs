namespace PickleCrew.IntegrationTests.Testing;

internal sealed record TestIdentity
{
    public required string UserId { get; init; }

    public string? OrganizationId { get; init; }

    public string? Role { get; init; }

    public static TestIdentity CreateSubjectOnly()
    {
        return new TestIdentity
        {
            UserId = $"user_test_{Guid.NewGuid():N}",
        };
    }

    public static TestIdentity Create(string role = "member")
    {
        return new TestIdentity
        {
            UserId = $"user_test_{Guid.NewGuid():N}",
            OrganizationId = $"org_test_{Guid.NewGuid():N}",
            Role = role,
        };
    }
}
