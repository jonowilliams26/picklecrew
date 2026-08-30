namespace PickleCrew.Api.Extensions;

internal static class DevelopmentApiDelayExtensions
{
    private const int MinimumDelayMilliseconds = 1_000;
    private const int MaximumDelayMillisecondsExclusive = 5_001;
    private const string EnabledConfigurationKey = "DevelopmentApiDelay:Enabled";
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static WebApplication UseDevelopmentApiDelay(this WebApplication app)
    {
        if (
            !app.Environment.IsDevelopment()
            || !app.Configuration.GetValue<bool>(EnabledConfigurationKey)
        )
        {
            return app;
        }

        app.Use(async (context, next) =>
        {
            var isHealthCheck =
                context.Request.Path.StartsWithSegments(HealthEndpointPath)
                || context.Request.Path.StartsWithSegments(AlivenessEndpointPath);

            if (!isHealthCheck)
            {
                var delayMilliseconds = Random.Shared.Next(
                    MinimumDelayMilliseconds,
                    MaximumDelayMillisecondsExclusive
                );

                await Task.Delay(delayMilliseconds, context.RequestAborted);
            }

            await next(context);
        });

        return app;
    }
}
