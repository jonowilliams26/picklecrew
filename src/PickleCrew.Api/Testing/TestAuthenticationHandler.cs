using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Testing;

internal static class TestAuthenticationDefaults
{
    public const string AuthenticationScheme = "Test";
    public const string UserIdHeaderName = "X-Test-User-Id";
    public const string OrganizationIdHeaderName = "X-Test-Organization-Id";
    public const string RoleHeaderName = "X-Test-Role";
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[TestAuthenticationDefaults.UserIdHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(WorkOsClaimTypes.Subject, userId),
        };

        AddOptionalClaim(
            claims,
            WorkOsClaimTypes.OrganizationId,
            TestAuthenticationDefaults.OrganizationIdHeaderName
        );
        AddOptionalClaim(
            claims,
            WorkOsClaimTypes.Role,
            TestAuthenticationDefaults.RoleHeaderName
        );

        var identity = new ClaimsIdentity(
            claims,
            TestAuthenticationDefaults.AuthenticationScheme,
            WorkOsClaimTypes.Subject,
            WorkOsClaimTypes.Role
        );
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(
            principal,
            TestAuthenticationDefaults.AuthenticationScheme
        );

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private void AddOptionalClaim(List<Claim> claims, string claimType, string headerName)
    {
        var value = Request.Headers[headerName].ToString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }
}
