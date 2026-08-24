using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Extensions;
using PickleCrew.Api.Games.Services;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;
using PickleCrew.Api.Testing;
using PickleCrew.Api.WorkOs;
using WorkOS;

namespace PickleCrew.Api;

internal static class DependencyInjection
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        const string connectionName = "picklecrew";

        builder.AddNpgsqlDbContext<PickleCrewDbContext>(connectionName);

        builder.Services.AddDbContext<CrewScopedDbContext>((services, options) =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(connectionName)
                ?? "Host=localhost;Database=picklecrew";

            options.UseNpgsql(connectionString);
        });
        builder.EnrichNpgsqlDbContext<CrewScopedDbContext>(settings =>
            settings.DisableHealthChecks = true
        );

        return builder;
    }

    public static IServiceCollection AddTimeProvider(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection AddUserContexts(this IServiceCollection services)
    {
        services.AddScoped<UserContext>();
        services.AddScoped<CrewUserContext>();

        return services;
    }

    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddScoped<GameStatisticsService>();

        return services;
    }

    public static IServiceCollection AddStatisticsServices(this IServiceCollection services)
    {
        services.AddScoped<CrewStandingsService>();

        return services;
    }

    public static WebApplicationBuilder AddWorkOs(this WebApplicationBuilder builder)
    {
        var isTesting = builder.Environment.IsTesting();

        builder.Services.AddAuthorization(AuthorizationPolicies.Configure);

        if (isTesting)
        {
            builder.Services
                .AddAuthentication(TestAuthenticationDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationDefaults.AuthenticationScheme,
                    _ => { }
                );
            builder.Services.AddHttpContextAccessor();
            builder.Services
                .AddHttpClient<IWorkOsService, TestWorkOsService>(
                    client => client.BaseAddress = new Uri("https+http://fake-workos")
                );

            return builder;
        }

        var configuration = builder.Configuration.GetSection(WorkOsOptions.SectionName);
        var isGeneratingOpenApi = Environment.GetCommandLineArgs()[0]
            .EndsWith("GetDocument.Insider.dll", StringComparison.OrdinalIgnoreCase);
        var clientId = configuration[nameof(WorkOsOptions.ClientId)] ?? "client_unconfigured";
        var issuer = $"https://api.workos.com/user_management/{clientId}";
        var jwksAddress = $"https://api.workos.com/sso/jwks/{clientId}";

        var workOsOptions = builder.Services.AddOptions<WorkOsOptions>()
            .Bind(configuration);

        if (!isGeneratingOpenApi)
        {
            workOsOptions
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.ClientId),
                    "ClientId is required.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.ApiKey),
                    "ApiKey is required.")
                .ValidateOnStart();
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "sub",
                    ValidIssuer = issuer,
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };
                options.ConfigurationManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        jwksAddress,
                        new WorkOsJwksConfigurationRetriever(issuer),
                        new HttpDocumentRetriever { RequireHttps = true });
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tokenClientId = context.Principal?.FindFirst("client_id")?.Value;
                        if (!string.Equals(tokenClientId, clientId, StringComparison.Ordinal))
                        {
                            context.Fail("The token client_id is invalid.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<WorkOsOptions>>().Value;
            return new WorkOSClient(new WorkOSOptions
            {
                ApiKey = options.ApiKey,
                ClientId = options.ClientId,
            });
        });
        builder.Services.AddSingleton<IWorkOsService, WorkOsService>();

        return builder;
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
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)
            );
        });

        return services;
    }
}
