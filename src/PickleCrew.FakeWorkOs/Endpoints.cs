using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PickleCrew.FakeWorkOs;

internal static class Endpoints
{
    public static IEndpointRouteBuilder MapFakeWorkOsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/setup", CreateOrganizationWithUser);
        app.MapPost("/users", CreateUser);
        app.MapPost("/organizations", CreateOrganization);
        app.MapPost("/memberships", CreateMembership);
        app.MapPost("/outcomes", CreateOutcome);

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
            FakeWorkOsState state)
    {
        var outcome = state.FindOutcome(
            FakeWorkOsOperation.CreateOrganization,
            request.ExternalId
        );

        if (outcome is not null)
        {
            return CreateProblem(outcome);
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
            FakeWorkOsState state)
    {
        var outcome = state.FindOutcome(
            FakeWorkOsOperation.CreateMembership,
            request.UserId
        );

        if (outcome is not null)
        {
            return CreateProblem(outcome);
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

    private static Results<Created<CreateOutcomeHttpResponse>, ValidationProblem>
        CreateOutcome(
            CreateOutcomeHttpRequest request,
            FakeWorkOsState state)
    {
        var isFailureStatusCode = request.StatusCode is >= 400 and <= 599;
        if (!isFailureStatusCode)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.StatusCode)] =
                        ["The status code must represent an HTTP error response."],
                }
            );
        }

        var outcome = state.CreateOutcome(
            request.Operation,
            request.Scope,
            request.StatusCode,
            request.Detail
        );

        return TypedResults.Created(
            "/outcomes",
            new CreateOutcomeHttpResponse
            {
                Operation = request.Operation,
                Scope = request.Scope,
                StatusCode = outcome.StatusCode,
                Detail = outcome.Detail,
            }
        );
    }

    private static Results<Ok<IdentifierHttpResponse>, NotFound, ProblemHttpResult>
        FindOrganizationByExternalId(
            string externalId,
            FakeWorkOsState state)
    {
        var outcome = state.FindOutcome(
            FakeWorkOsOperation.FindOrganizationByExternalId,
            externalId
        );

        if (outcome is not null)
        {
            return CreateProblem(outcome);
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
            FakeWorkOsState state)
    {
        var outcome = state.FindOutcome(
            FakeWorkOsOperation.FindActiveMembership,
            userId
        );

        if (outcome is not null)
        {
            return CreateProblem(outcome);
        }

        var membership = state.FindActiveMembership(userId, organizationId);

        return membership is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new IdentifierHttpResponse { Id = membership.Id });
    }

    private static ProblemHttpResult CreateProblem(FakeOperationOutcome outcome)
    {
        return TypedResults.Problem(
            detail: outcome.Detail,
            statusCode: outcome.StatusCode
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

public sealed record CreateOutcomeHttpRequest
{
    public required FakeWorkOsOperation Operation { get; init; }

    public required string Scope { get; init; }

    public required int StatusCode { get; init; }

    public required string Detail { get; init; }
}

public sealed record CreateOutcomeHttpResponse
{
    public required FakeWorkOsOperation Operation { get; init; }

    public required string Scope { get; init; }

    public required int StatusCode { get; init; }

    public required string Detail { get; init; }
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
