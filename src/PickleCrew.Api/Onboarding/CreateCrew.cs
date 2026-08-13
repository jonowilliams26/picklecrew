using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Onboarding;

public sealed record CreateCrewHttpRequest
{
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
        app.MapPost("/crew", HandleAsync)
            .WithName("CreateOnboardingCrew")
            .WithSummary("Creates the signed-in user's first crew.")
            .WithRequestValidation<CreateCrewHttpRequest>(Validate)
            .WithUserContext();
    }

    internal static Dictionary<string, string[]> Validate(CreateCrewHttpRequest request)
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

    internal static async Task<Created<CreateCrewHttpResponse>> HandleAsync(
        CreateCrewHttpRequest request,
        UserContext userContext,
        PickleCrewDbContext dbContext,
        IWorkOsService workOs,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var workOsUserId = userContext.UserId;
        var attempt = await dbContext.CrewProvisioningAttempts
            .SingleOrDefaultAsync(
                candidate => candidate.WorkOsUserId == workOsUserId,
                cancellationToken
            );

        if (attempt?.CompletedAtUtc is not null)
        {
            var existingCrew = await dbContext.Crews
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
                CrewName = normalizedName,
                WorkOsUserId = workOsUserId,
                StartedAtUtc = timeProvider.GetUtcNow(),
            };

            dbContext.CrewProvisioningAttempts.Add(attempt);
            await dbContext.SaveChangesAsync(cancellationToken);
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
            await dbContext.SaveChangesAsync(cancellationToken);
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

        dbContext.Crews.Add(crew);
        attempt.CompletedAtUtc = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

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
