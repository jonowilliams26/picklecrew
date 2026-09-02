using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record ChangeCrewNameHttpRequest
{
    public required string Name { get; init; }
}

public sealed record ChangeCrewNameHttpResponse
{
    public required string Name { get; init; }
}

internal sealed class ChangeCrewNameEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/name", HandleAsync)
            .WithName("ChangeCrewName")
            .WithSummary("Changes the active crew's name.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator)
            .WithRequestValidation<ChangeCrewNameHttpRequest>(Validate);
    }

    internal static Dictionary<string, string[]> Validate(
        ChangeCrewNameHttpRequest request)
    {
        const int maximumNameLength = 100;
        var normalizedName = request.Name?.Trim() ?? string.Empty;

        if (normalizedName.Length == 0)
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.Name)] = ["Crew name must not be blank."],
            };
        }

        if (normalizedName.Length > maximumNameLength)
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.Name)] =
                    [$"Crew name must contain at most {maximumNameLength} characters."],
            };
        }

        return [];
    }

    private static async Task<Ok<ChangeCrewNameHttpResponse>> HandleAsync(
        ChangeCrewNameHttpRequest request,
        CrewUserContext crewUserContext,
        PickleCrewDbContext database,
        IWorkOsService workOs,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var crew = await database.Crews.SingleAsync(
            candidate => candidate.Id == crewUserContext.CrewId,
            cancellationToken
        );

        await workOs.UpdateOrganizationNameAsync(
            crewUserContext.OrganizationId,
            normalizedName,
            cancellationToken
        );

        crew.Name = normalizedName;
        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new ChangeCrewNameHttpResponse
        {
            Name = crew.Name,
        });
    }
}
