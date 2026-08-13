using Microsoft.AspNetCore.Authorization;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Authorization;

internal static class WorkOsRoles
{
    public const string Admin = "admin";

    public const string Member = "member";
}

internal static class AuthorizationPolicies
{
    public const string CrewAdministrator = nameof(CrewAdministrator);

    public const string CrewUser = nameof(CrewUser);

    public const string User = nameof(User);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            User,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(WorkOsClaimTypes.Subject)
        );
        options.AddPolicy(
            CrewUser,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(WorkOsClaimTypes.Subject)
                .RequireClaim(WorkOsClaimTypes.OrganizationId)
                .RequireClaim(WorkOsClaimTypes.Role)
        );
        options.AddPolicy(
            CrewAdministrator,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(WorkOsClaimTypes.Subject)
                .RequireClaim(WorkOsClaimTypes.OrganizationId)
                .RequireClaim(
                    WorkOsClaimTypes.Role,
                    WorkOsRoles.Admin
                )
        );
    }
}
