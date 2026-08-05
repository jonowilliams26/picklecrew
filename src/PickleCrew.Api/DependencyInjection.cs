using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonPropertyInfo is not null
                    || !typeof(ProblemDetails).IsAssignableFrom(context.JsonTypeInfo.Type))
                {
                    return Task.CompletedTask;
                }

                schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                schema.Properties["traceId"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description = "The trace identifier for the request.",
                };
                schema.Required ??= new HashSet<string>();
                schema.Required.Add("traceId");

                return Task.CompletedTask;
            });
        });

        return services;
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
