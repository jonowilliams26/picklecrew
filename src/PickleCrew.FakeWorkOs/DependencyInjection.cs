using System.Text.Json;
using System.Text.Json.Serialization;

namespace PickleCrew.FakeWorkOs;

internal static class DependencyInjection
{
    public static IServiceCollection AddFakeWorkOs(this IServiceCollection services)
    {
        services.AddSingleton<FakeWorkOsState>();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)
            );
        });

        return services;
    }
}
