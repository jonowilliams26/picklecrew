namespace PickleCrew.IntegrationTests.Testing;

internal sealed record TestIdentity
{
    public required string UserId { get; init; }

    public required string EmailAddress { get; init; }

    public string? OrganizationId { get; init; }

    public string? Role { get; init; }

    public static TestIdentity CreateSubjectOnly()
    {
        var userId = $"user_test_{Guid.NewGuid():N}";

        return new TestIdentity
        {
            UserId = userId,
            EmailAddress = $"{userId}@example.test",
        };
    }

    public static TestIdentity Create(string role = "member")
    {
        var userId = $"user_test_{Guid.NewGuid():N}";

        return new TestIdentity
        {
            UserId = userId,
            EmailAddress = $"{userId}@example.test",
            OrganizationId = $"org_test_{Guid.NewGuid():N}",
            Role = role,
        };
    }
}
