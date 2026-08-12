namespace PickleCrew.Api.Extensions;

internal static class HostEnvironmentExtensions
{
    private const string TestingEnvironmentName = "Testing";

    public static bool IsTesting(this IHostEnvironment environment)
    {
        return environment.IsEnvironment(TestingEnvironmentName);
    }
}
