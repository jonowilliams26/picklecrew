using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using PickleCrew.Api;
using Xunit;

namespace PickleCrew.IntegrationTests.ApiDocumentation;

[Collection(nameof(AppHostCollection))]
public sealed class OpenApiTests(AppHostFixture fixture)
{
    [Fact]
    public async Task OpenApiDocument_WhenResponseIsEmptyErrorStatusCode_ShouldUseProblemDetailsSchema()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddApiDocumentation();

        await using var app = builder.Build();
        app.MapGet("/resource", static () => TypedResults.NotFound());
        await app.StartAsync();

        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync();
        var path = document.Paths["/resource"];
        Assert.NotNull(path.Operations);
        var operation = path.Operations[HttpMethod.Get];
        Assert.NotNull(operation.Responses);
        var response = operation.Responses[StatusCodes.Status404NotFound.ToString()];
        Assert.NotNull(response.Content);
        var mediaType = response.Content["application/problem+json"];
        Assert.NotNull(mediaType.Schema);
        Assert.NotNull(mediaType.Schema.Properties);

        Assert.Contains("traceId", mediaType.Schema.Properties.Keys);
    }

    [Fact]
    public async Task OpenApiDocument_WhenGenerated_ShouldDescribeIntegerPropertiesWithoutStringTypes()
    {
        using var httpResponse = await fixture.ApiClient.GetAsync("/openapi/v1.json");

        httpResponse.EnsureSuccessStatusCode();

        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(responseStream);
        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        var statusSchema = schemas
            .GetProperty("ProblemDetails")
            .GetProperty("properties")
            .GetProperty("status");

        AssertIntegerSchema(statusSchema);
    }

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

    private static void AssertIntegerSchema(JsonElement schema)
    {
        var type = schema.GetProperty("type");
        var types = type.ValueKind switch
        {
            JsonValueKind.String => [type.GetString()],
            JsonValueKind.Array => type
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray(),
            _ => [],
        };

        Assert.Contains("integer", types);
        Assert.DoesNotContain("string", types);
        Assert.False(schema.TryGetProperty("pattern", out _));
    }
}
