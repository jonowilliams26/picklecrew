using PickleCrew.Api.PersistenceProbe;

namespace PickleCrew.Api;

internal static class Endpoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapGroup(string.Empty)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .MapEndpoint<CreatePersistenceProbeEndpoint>();
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
