using Microsoft.AspNetCore.Authorization;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Authorization;

internal static class AuthorizationPolicies
{
    public const string WorkOsUser = nameof(WorkOsUser);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            WorkOsUser,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(WorkOsClaimTypes.Subject)
        );
    }
}
