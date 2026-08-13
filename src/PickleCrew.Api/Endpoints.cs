using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Onboarding;
using PickleCrew.Api.Testing;

namespace PickleCrew.Api;

internal static class Endpoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(string.Empty)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapOnboardingEndpoints();
        endpoints.MapCrewEndpoints();
        endpoints.MapTestingEndpoints();

        return endpoints;
    }

    private static IEndpointRouteBuilder MapCrewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/crew")
            .WithCrewUserContext();

        return app;
    }

    private static IEndpointRouteBuilder MapOnboardingEndpoints(
        this IEndpointRouteBuilder app)
    {
        return app.MapGroup("/onboarding")
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
