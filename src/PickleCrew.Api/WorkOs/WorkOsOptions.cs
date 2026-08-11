namespace PickleCrew.Api.WorkOs;

internal sealed record WorkOsOptions
{
    public const string SectionName = "WorkOS";

    public required string ClientId { get; init; }

    public required string ApiKey { get; init; }

    public string Issuer { get; init; } = "https://api.workos.com";
}
