using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PickleCrew.FakeWorkOs;

internal static class Endpoints
{
    private const string ScenarioIdHeaderName = "X-Test-Scenario-Id";

    public static IEndpointRouteBuilder MapFakeWorkOsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/setup", CreateOrganizationWithUser);
        app.MapPost("/users", CreateUser);
        app.MapPost("/organizations", CreateOrganization);
        app.MapPost("/memberships", CreateMembership);
        app.MapPost("/faults", FailOperation);

        app.MapGet(
            "/organizations/by-external-id/{externalId}",
            FindOrganizationByExternalId
        );
        app.MapGet(
            "/organizations/{organizationId}/active-memberships/{userId}",
            FindActiveMembership
        );

        return app;
    }

    private static Created<CreateOrganizationWithUserHttpResponse> CreateOrganizationWithUser(
        CreateOrganizationWithUserHttpRequest request,
        FakeWorkOsState state)
    {
        var setup = state.CreateOrganizationWithUser(
            request.UserId,
            request.OrganizationName,
            request.OrganizationExternalId,
            request.Role
        );

        return TypedResults.Created(
            $"/organizations/{setup.Organization.Id}",
            new CreateOrganizationWithUserHttpResponse
            {
                UserId = setup.User.Id,
                OrganizationId = setup.Organization.Id,
                MembershipId = setup.Membership.Id,
            }
        );
    }

    private static Created<IdentifierHttpResponse> CreateUser(
        CreateUserHttpRequest request,
        FakeWorkOsState state)
    {
        var user = state.CreateUser(request.Id);

        return TypedResults.Created(
            $"/users/{user.Id}",
            new IdentifierHttpResponse { Id = user.Id }
        );
    }

    private static Results<Created<IdentifierHttpResponse>, ProblemHttpResult>
        CreateOrganization(
            CreateOrganizationHttpRequest request,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.CreateOrganization
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.CreateOrganization);
        }

        var organization = state.CreateOrganization(
            request.Name,
            request.ExternalId,
            request.Id
        );

        return TypedResults.Created(
            $"/organizations/{organization.Id}",
            new IdentifierHttpResponse { Id = organization.Id }
        );
    }

    private static Results<Created<IdentifierHttpResponse>, ProblemHttpResult>
        CreateMembership(
            CreateMembershipHttpRequest request,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.CreateMembership
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.CreateMembership);
        }

        var membership = state.CreateMembership(
            request.UserId,
            request.OrganizationId,
            request.Role,
            request.Status,
            request.Id
        );

        return TypedResults.Created(
            $"/memberships/{membership.Id}",
            new IdentifierHttpResponse { Id = membership.Id }
        );
    }

    private static NoContent FailOperation(
        FailOperationHttpRequest request,
        FakeWorkOsState state)
    {
        state.FailOperation(request.ScenarioId, request.Operation);

        return TypedResults.NoContent();
    }

    private static Results<Ok<IdentifierHttpResponse>, NotFound, ProblemHttpResult>
        FindOrganizationByExternalId(
            string externalId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.FindOrganizationByExternalId
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.FindOrganizationByExternalId);
        }

        var organization = state.FindOrganization(externalId);

        return organization is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new IdentifierHttpResponse { Id = organization.Id });
    }

    private static Results<Ok<IdentifierHttpResponse>, NotFound, ProblemHttpResult>
        FindActiveMembership(
            string organizationId,
            string userId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.FindActiveMembership
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.FindActiveMembership);
        }

        var membership = state.FindActiveMembership(userId, organizationId);

        return membership is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new IdentifierHttpResponse { Id = membership.Id });
    }

    private static bool ShouldFailOperation(
        HttpRequest request,
        FakeWorkOsState state,
        FakeWorkOsOperation operation)
    {
        var scenarioId = request.Headers[ScenarioIdHeaderName].ToString();

        return !string.IsNullOrWhiteSpace(scenarioId)
            && state.ShouldFailOperation(scenarioId, operation);
    }

    private static ProblemHttpResult CreateFaultProblem(FakeWorkOsOperation operation)
    {
        return TypedResults.Problem(
            detail: $"Configured {operation} failure.",
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
}

public sealed record CreateOrganizationWithUserHttpRequest
{
    public required string UserId { get; init; }

    public required string OrganizationName { get; init; }

    public required string OrganizationExternalId { get; init; }

    public required MembershipRole Role { get; init; }
}

public sealed record CreateOrganizationWithUserHttpResponse
{
    public required string UserId { get; init; }

    public required string OrganizationId { get; init; }

    public required string MembershipId { get; init; }
}

public sealed record CreateUserHttpRequest
{
    public required string Id { get; init; }
}

public sealed record CreateOrganizationHttpRequest
{
    public string? Id { get; init; }

    public required string Name { get; init; }

    public required string ExternalId { get; init; }
}

public sealed record CreateMembershipHttpRequest
{
    public string? Id { get; init; }

    public required string UserId { get; init; }

    public required string OrganizationId { get; init; }

    public required MembershipRole Role { get; init; }

    public MembershipStatus Status { get; init; } = MembershipStatus.Active;
}

public sealed record FailOperationHttpRequest
{
    public required string ScenarioId { get; init; }

    public required FakeWorkOsOperation Operation { get; init; }
}

public sealed record IdentifierHttpResponse
{
    public required string Id { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<MembershipRole>))]
public enum MembershipRole
{
    Admin,
    Member,
}

[JsonConverter(typeof(JsonStringEnumConverter<MembershipStatus>))]
public enum MembershipStatus
{
    Active,
    Inactive,
}

[JsonConverter(typeof(JsonStringEnumConverter<FakeWorkOsOperation>))]
public enum FakeWorkOsOperation
{
    FindOrganizationByExternalId,
    CreateOrganization,
    FindActiveMembership,
    CreateMembership,
}
