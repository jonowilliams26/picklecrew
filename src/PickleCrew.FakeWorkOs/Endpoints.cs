using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PickleCrew.FakeWorkOs;

internal static class Endpoints
{
    public static IEndpointRouteBuilder MapFakeWorkOsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/organizations/by-external-id/{externalId}",
            FindOrganizationByExternalId
        );
        app.MapPost("/organizations", CreateOrganization);
        app.MapGet(
            "/organizations/{organizationId}/active-memberships/{userId}",
            FindActiveMembership
        );
        app.MapPost(
            "/organizations/{organizationId}/memberships",
            CreateMembership
        );

        app.MapPost("/setup", CreateOrganizationWithUser);

        return app;
    }

    private static Results<Ok<IdentifierHttpResponse>, NotFound> FindOrganizationByExternalId(
        string externalId,
        FakeWorkOsState state)
    {
        var organization = state.FindOrganization(externalId);

        return organization is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new IdentifierHttpResponse { Id = organization.Id });
    }

    private static Created<IdentifierHttpResponse> CreateOrganization(
        CreateOrganizationHttpRequest request,
        FakeWorkOsState state)
    {
        var organization = state.CreateOrganization(request.Name, request.ExternalId);

        return TypedResults.Created(
            $"/organizations/{organization.Id}",
            new IdentifierHttpResponse { Id = organization.Id }
        );
    }

    private static Results<Ok<IdentifierHttpResponse>, NotFound> FindActiveMembership(
        string organizationId,
        string userId,
        FakeWorkOsState state)
    {
        var membership = state.FindActiveMembership(userId, organizationId);

        return membership is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new IdentifierHttpResponse { Id = membership.Id });
    }

    private static Created<IdentifierHttpResponse> CreateMembership(
        string organizationId,
        CreateMembershipHttpRequest request,
        FakeWorkOsState state)
    {
        var membership = state.CreateMembership(
            request.UserId,
            organizationId,
            request.Role
        );

        return TypedResults.Created(
            $"/organizations/{organizationId}/memberships/{membership.Id}",
            new IdentifierHttpResponse { Id = membership.Id }
        );
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
}

public sealed record CreateOrganizationHttpRequest
{
    public required string Name { get; init; }

    public required string ExternalId { get; init; }
}

public sealed record CreateMembershipHttpRequest
{
    public required string UserId { get; init; }

    public required MembershipRole Role { get; init; }
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
