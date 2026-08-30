using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record CreateCrewHttpRequest
{
    public required Guid RequestId { get; init; }

    public required string Name { get; init; }
}

public sealed record CreateCrewHttpResponse
{
    public required Guid CrewId { get; init; }

    public required string Name { get; init; }

    public required string OrganizationId { get; init; }
}

internal sealed class CreateCrewEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(string.Empty, HandleAsync)
            .WithName("CreateCrew")
            .WithSummary("Creates a crew for the signed-in user.")
            .WithRequestValidation<CreateCrewHttpRequest>(Validate);
    }

    internal static Dictionary<string, string[]> Validate(CreateCrewHttpRequest request)
    {
        const int maximumNameLength = 100;

        if (request.RequestId == Guid.Empty)
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.RequestId)] = ["Request ID must not be empty."],
            };
        }

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

    internal static async Task<Created<CreateCrewHttpResponse>> HandleAsync(
        CreateCrewHttpRequest request,
        UserContext userContext,
        PickleCrewDbContext database,
        IWorkOsService workOs,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var workOsUserId = userContext.UserId;
        var attempt = await database.CrewProvisioningAttempts
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.WorkOsUserId == workOsUserId &&
                    candidate.RequestId == request.RequestId,
                cancellationToken
            );

        if (attempt?.CompletedAtUtc is not null)
        {
            var existingCrew = await database.Crews
                .AsNoTracking()
                .SingleAsync(
                    crew => crew.Uuid == attempt.CrewUuid,
                    cancellationToken
                );

            return TypedResults.Created(
                $"/crews/{existingCrew.Uuid}",
                new CreateCrewHttpResponse
                {
                    CrewId = existingCrew.Uuid,
                    Name = existingCrew.Name,
                    OrganizationId = existingCrew.WorkOsOrganizationId,
                }
            );
        }

        if (attempt is null)
        {
            attempt = new CrewProvisioningAttempt
            {
                CrewUuid = Guid.NewGuid(),
                RequestId = request.RequestId,
                CrewName = normalizedName,
                WorkOsUserId = workOsUserId,
                StartedAtUtc = timeProvider.GetUtcNow(),
            };

            database.CrewProvisioningAttempts.Add(attempt);
            await database.SaveChangesAsync(cancellationToken);
        }

        var externalId = attempt.CrewUuid.ToString();
        var organizationId = attempt.WorkOsOrganizationId
            ?? await workOs.FindOrganizationByExternalIdAsync(externalId, cancellationToken)
            ?? await workOs.CreateOrganizationAsync(
                attempt.CrewName,
                externalId,
                cancellationToken
            );

        if (attempt.WorkOsOrganizationId is null)
        {
            attempt.WorkOsOrganizationId = organizationId;
            await database.SaveChangesAsync(cancellationToken);
        }

        var membershipId = attempt.WorkOsMembershipId
            ?? await workOs.FindActiveMembershipIdAsync(
                workOsUserId,
                organizationId,
                cancellationToken
            )
            ?? await workOs.CreateAdminMembershipAsync(
                workOsUserId,
                organizationId,
                cancellationToken
            );

        attempt.WorkOsMembershipId = membershipId;

        var crew = new Crew
        {
            Uuid = attempt.CrewUuid,
            Name = attempt.CrewName,
            WorkOsOrganizationId = organizationId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        database.Crews.Add(crew);
        attempt.CompletedAtUtc = timeProvider.GetUtcNow();
        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/crews/{crew.Uuid}",
            new CreateCrewHttpResponse
            {
                CrewId = crew.Uuid,
                Name = crew.Name,
                OrganizationId = crew.WorkOsOrganizationId,
            }
        );
    }
}
