namespace PickleCrew.Api.WorkOs;

internal sealed record WorkOsOptions
{
    public const string SectionName = "WorkOS";

    public required string ClientId { get; init; }

    public required string ApiKey { get; init; }
}

internal static class WorkOsTokenEndpoints
{
    private const string ApiBaseUrl = "https://api.workos.com";

    public static string GetIssuer(string clientId) =>
        $"{ApiBaseUrl}/user_management/{clientId}";

    public static string GetJwksAddress(string clientId) =>
        $"{ApiBaseUrl}/sso/jwks/{clientId}";
}
