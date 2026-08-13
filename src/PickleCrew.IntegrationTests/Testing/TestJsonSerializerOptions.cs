using System.Text.Json;
using System.Text.Json.Serialization;

namespace PickleCrew.IntegrationTests.Testing;

internal static class TestJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower),
        },
    };
}
