namespace PickleCrew.Api.EndpointFilters;

internal static class RequestValidationEndpointFilterExtensions
{
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(
        this RouteHandlerBuilder builder,
        Func<TRequest, Dictionary<string, string[]>> validate)
        where TRequest : class
    {
        return builder
            .AddEndpointFilter(new RequestValidationEndpointFilter<TRequest>(validate))
            .ProducesValidationProblem();
    }
}

internal sealed class RequestValidationEndpointFilter<TRequest>(
    Func<TRequest, Dictionary<string, string[]>> validate) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().Single();
        var errors = validate(request);

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}
