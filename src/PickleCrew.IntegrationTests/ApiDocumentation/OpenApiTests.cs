using System.Text.Json;
using Xunit;

namespace PickleCrew.IntegrationTests.ApiDocumentation;

public sealed class OpenApiTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task OpenApiDocument_WhenGenerated_ShouldIncludeTraceIdInProblemDetailsSchemas()
    {
        using var httpResponse = await fixture.ApiClient.GetAsync("/openapi/v1.json");

        httpResponse.EnsureSuccessStatusCode();

        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(responseStream);
        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        var problemDetailsSchemas = schemas.EnumerateObject()
            .Where(schema => schema.Name.EndsWith("ProblemDetails", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(problemDetailsSchemas);
        Assert.All(problemDetailsSchemas, schema =>
        {
            var traceId = schema.Value
                .GetProperty("properties")
                .GetProperty("traceId");

            Assert.Equal("string", traceId.GetProperty("type").GetString());
            Assert.Contains(
                schema.Value.GetProperty("required").EnumerateArray(),
                property => property.GetString() == "traceId"
            );
        });
    }
}
