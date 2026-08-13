namespace PickleCrew.FakeWorkOs;

internal static class DependencyInjection
{
    public static IServiceCollection AddFakeWorkOs(this IServiceCollection services)
    {
        services.AddSingleton<FakeWorkOsState>();

        return services;
    }
}
