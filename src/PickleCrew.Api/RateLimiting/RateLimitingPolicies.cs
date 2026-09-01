using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.RateLimiting;

internal static class RateLimitingPolicies
{
    public const string AuthenticatedUser = nameof(AuthenticatedUser);

    internal const int AuthenticatedUserPermitLimit = 100;

    private static readonly TimeSpan AuthenticatedUserWindow = TimeSpan.FromMinutes(1);

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(
            AuthenticatedUser,
            httpContext =>
            {
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    return RateLimitPartition.GetNoLimiter("unauthenticated");
                }

                var userId = httpContext.User.FindFirst(WorkOsClaimTypes.Subject)?.Value;
                var partitionKey = string.IsNullOrWhiteSpace(userId)
                    ? "authenticated-without-subject"
                    : $"user:{userId}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = AuthenticatedUserPermitLimit,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        Window = AuthenticatedUserWindow,
                    }
                );
            }
        );
        options.OnRejected = (context, _) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                var retryAfterSeconds = Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds));
                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(
                    CultureInfo.InvariantCulture
                );
            }

            return ValueTask.CompletedTask;
        };
    }
}
