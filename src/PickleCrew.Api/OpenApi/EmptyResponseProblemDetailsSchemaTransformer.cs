using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PickleCrew.Api.OpenApi;

public static class EmptyResponseProblemDetailsOperationTransformer
{
    public static void AddEmptyResponseProblemDetailsSchemaTransformer(this OpenApiOptions options)
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
    }
}