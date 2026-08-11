using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using PickleCrew.Api.Data.Database;

namespace PickleCrew.Api;

internal static class DependencyInjection
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<PickleCrewDbContext>("picklecrew");

        return builder;
    }

    public static IServiceCollection AddTimeProvider(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection AddRequestLogging(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = environment.IsDevelopment()
                ? HttpLoggingFields.All
                : HttpLoggingFields.RequestMethod
                    | HttpLoggingFields.RequestPath
                    | HttpLoggingFields.ResponseStatusCode
                    | HttpLoggingFields.Duration;
            options.RequestBodyLogLimit = 4 * 1024;
            options.ResponseBodyLogLimit = 4 * 1024;
            options.CombineLogs = true;
        });

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddOperationTransformer(async (operation, context, cancellationToken) =>
            {
                if (operation.Responses is null)
                {
                    return;
                }

                var emptyErrorResponses = operation.Responses
                    .Where(response =>
                        int.TryParse(response.Key, out var statusCode)
                        && statusCode is >= StatusCodes.Status400BadRequest and < 600
                        && response.Value.Content is not { Count: > 0 })
                    .Select(response => response.Value)
                    .OfType<OpenApiResponse>()
                    .ToArray();

                if (emptyErrorResponses.Length == 0)
                {
                    return;
                }

                var problemDetailsSchema = await context.GetOrCreateSchemaAsync(
                    typeof(ProblemDetails),
                    cancellationToken: cancellationToken
                );

                foreach (var response in emptyErrorResponses)
                {
                    response.Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new()
                        {
                            Schema = problemDetailsSchema,
                        },
                    };
                }
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                var schemaType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type)
                    ?? context.JsonTypeInfo.Type;

                if (IsIntegerType(schemaType))
                {
                    if (schema.Type is { } jsonSchemaType)
                    {
                        schema.Type = jsonSchemaType & ~JsonSchemaType.String;
                    }

                    schema.Pattern = null;
                }

                if (context.JsonPropertyInfo is null
                    && typeof(ProblemDetails).IsAssignableFrom(context.JsonTypeInfo.Type))
                {
                    schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                    schema.Properties["traceId"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Description = "The trace identifier for the request.",
                    };
                    schema.Required ??= new HashSet<string>();
                    schema.Required.Add("traceId");
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static bool IsIntegerType(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong);
    }

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }

    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}
