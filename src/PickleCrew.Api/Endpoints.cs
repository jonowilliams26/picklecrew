using PickleCrew.Api.Crews;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Testing;

namespace PickleCrew.Api;

internal static class Endpoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(string.Empty)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapCrewsEndpoints();
        endpoints.MapTestingEndpoints();

        return endpoints;
    }

    private static IEndpointRouteBuilder MapCrewsEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapGroup("/crews")
            .WithUserContext()
            .MapEndpoint<CreateCrewEndpoint>();
    }

    public static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);

        return app;
    }
}

internal interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}
