using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PickleCrew.Api.OpenApi;

public static class TraceIdProblemDetailsSchemaTransformer
{
    public static void AddTraceIdProblemDetailsSchemaTransformer(this OpenApiOptions options)
    {
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
}