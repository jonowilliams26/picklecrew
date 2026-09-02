using Microsoft.AspNetCore.Http.HttpResults;

namespace PickleCrew.FakeWorkOs;

internal static class Endpoints
{
    private const string ScenarioIdHeaderName = "X-Test-Scenario-Id";

    public static IEndpointRouteBuilder MapFakeWorkOsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/setup", CreateOrganizationWithUser);
        app.MapPost("/users", CreateUser);
        app.MapPut("/users/{userId}", UpdateUser);
        app.MapPost("/organizations", CreateOrganization);
        app.MapDelete("/organizations/{organizationId}", DeleteOrganization);
        app.MapPost("/memberships", CreateMembership);
        app.MapPut("/memberships/{membershipId}", UpdateMembershipRole);
        app.MapPost("/memberships/{membershipId}/deactivate", DeactivateMembership);
        app.MapPost("/invitations", CreateInvitation);
        app.MapPost("/faults", FailOperation);

        app.MapGet(
            "/organizations/by-external-id/{externalId}",
            FindOrganizationByExternalId
        );
        app.MapGet(
            "/organizations/{organizationId}/active-memberships/{userId}",
            FindActiveMembership
        );
        app.MapGet(
            "/organizations/{organizationId}/active-members",
            ListActiveMembers
        );
        app.MapGet(
            "/organizations/{organizationId}/pending-invitations",
            ListPendingInvitations
        );
        app.MapGet(
            "/organizations/{organizationId}/invitations",
            ListInvitations
        );

        return app;
    }

    private static Created<CreateOrganizationWithUserHttpResponse> CreateOrganizationWithUser(
        CreateOrganizationWithUserHttpRequest request,
        FakeWorkOsState state)
    {
        var setup = state.CreateOrganizationWithUser(
            request.UserId,
            request.UserEmailAddress,
            request.OrganizationName,
            request.OrganizationExternalId,
            request.Role,
            request.UserFirstName,
            request.UserLastName,
            request.UserDisplayName,
            request.UserProfilePictureUrl
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
        var user = state.CreateUser(
            request.Id,
            request.EmailAddress,
            request.FirstName,
            request.LastName,
            request.DisplayName,
            request.ProfilePictureUrl
        );

        return TypedResults.Created(
            $"/users/{user.Id}",
            new IdentifierHttpResponse { Id = user.Id }
        );
    }

    private static Ok<IdentifierHttpResponse> UpdateUser(
        string userId,
        UpdateUserHttpRequest request,
        FakeWorkOsState state)
    {
        var user = state.UpdateUser(
            userId,
            request.DisplayName,
            request.ProfilePictureUrl
        );

        return TypedResults.Ok(new IdentifierHttpResponse { Id = user.Id });
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

    private static Results<NoContent, NotFound, ProblemHttpResult>
        DeleteOrganization(
            string organizationId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.DeleteOrganization
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.DeleteOrganization);
        }

        var organization = state.DeleteOrganization(organizationId);

        return organization is null
            ? TypedResults.NotFound()
            : TypedResults.NoContent();
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

    private static Results<NoContent, NotFound, ProblemHttpResult>
        UpdateMembershipRole(
            string membershipId,
            UpdateMembershipRoleHttpRequest request,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.UpdateMembership
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.UpdateMembership);
        }

        var membership = state.UpdateMembershipRole(
            membershipId,
            request.Role
        );

        return membership is null
            ? TypedResults.NotFound()
            : TypedResults.NoContent();
    }

    private static Results<NoContent, NotFound, ProblemHttpResult>
        DeactivateMembership(
            string membershipId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.DeactivateMembership
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.DeactivateMembership);
        }

        var membership = state.DeactivateMembership(membershipId);

        return membership is null
            ? TypedResults.NotFound()
            : TypedResults.NoContent();
    }

    private static Results<Created<InvitationHttpResponse>, ProblemHttpResult>
        CreateInvitation(
            CreateInvitationHttpRequest request,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.SendInvitation,
            request.EmailAddress
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.SendInvitation);
        }

        var invitation = state.CreateInvitation(
            request.OrganizationId,
            request.EmailAddress,
            request.Role,
            request.InviterUserId,
            request.State,
            request.Id
        );

        return TypedResults.Created(
            $"/invitations/{invitation.Id}",
            ToHttpResponse(invitation)
        );
    }

    private static NoContent FailOperation(
        FailOperationHttpRequest request,
        FakeWorkOsState state)
    {
        state.FailOperation(
            request.ScenarioId,
            request.Operation,
            request.RecipientEmailAddress
        );

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

    private static Results<Ok<ActiveCrewMemberListHttpResponse>, ProblemHttpResult>
        ListActiveMembers(
            string organizationId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.ListActiveMembers
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.ListActiveMembers);
        }

        var members = state.ListActiveMembers(organizationId)
            .Select(member => new ActiveCrewMemberHttpResponse
            {
                WorkOsUserId = member.WorkOsUserId,
                DisplayName = member.DisplayName,
                EmailAddress = member.EmailAddress,
                Role = member.Role,
                ProfilePictureUrl = member.ProfilePictureUrl,
            })
            .ToArray();

        return TypedResults.Ok(
            new ActiveCrewMemberListHttpResponse { Members = members }
        );
    }

    private static Results<Ok<InvitationListHttpResponse>, ProblemHttpResult>
        ListPendingInvitations(
            string organizationId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.ListPendingInvitations
        ))
        {
            return CreateFaultProblem(FakeWorkOsOperation.ListPendingInvitations);
        }

        var invitations = state.ListPendingInvitations(organizationId)
            .Select(ToHttpResponse)
            .ToArray();

        return TypedResults.Ok(
            new InvitationListHttpResponse { Invitations = invitations }
        );
    }

    private static Ok<InvitationListHttpResponse> ListInvitations(
        string organizationId,
        FakeWorkOsState state)
    {
        var invitations = state.ListInvitations(organizationId)
            .Select(ToHttpResponse)
            .ToArray();

        return TypedResults.Ok(
            new InvitationListHttpResponse { Invitations = invitations }
        );
    }

    private static bool ShouldFailOperation(
        HttpRequest request,
        FakeWorkOsState state,
        FakeWorkOsOperation operation,
        string? recipientEmailAddress = null)
    {
        var scenarioId = request.Headers[ScenarioIdHeaderName].ToString();

        return !string.IsNullOrWhiteSpace(scenarioId)
            && state.ShouldFailOperation(
                scenarioId,
                operation,
                recipientEmailAddress
            );
    }

    private static ProblemHttpResult CreateFaultProblem(FakeWorkOsOperation operation)
    {
        return TypedResults.Problem(
            detail: $"Configured {operation} failure.",
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }

    private static InvitationHttpResponse ToHttpResponse(FakeInvitation invitation)
    {
        return new InvitationHttpResponse
        {
            Id = invitation.Id,
            OrganizationId = invitation.OrganizationId,
            EmailAddress = invitation.EmailAddress,
            State = invitation.State,
            Role = invitation.Role,
            InviterUserId = invitation.InviterUserId,
        };
    }
}

public sealed record CreateOrganizationWithUserHttpRequest
{
    public required string UserId { get; init; }

    public required string UserEmailAddress { get; init; }

    public string? UserFirstName { get; init; }

    public string? UserLastName { get; init; }

    public string? UserDisplayName { get; init; }

    public string? UserProfilePictureUrl { get; init; }

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

    public required string EmailAddress { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

public sealed record UpdateUserHttpRequest
{
    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }
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

public sealed record UpdateMembershipRoleHttpRequest
{
    public required MembershipRole Role { get; init; }
}

public sealed record CreateInvitationHttpRequest
{
    public string? Id { get; init; }

    public required string OrganizationId { get; init; }

    public required string EmailAddress { get; init; }

    public required MembershipRole Role { get; init; }

    public required string InviterUserId { get; init; }

    public InvitationState State { get; init; } = InvitationState.Pending;
}

public sealed record FailOperationHttpRequest
{
    public required string ScenarioId { get; init; }

    public required FakeWorkOsOperation Operation { get; init; }

    public string? RecipientEmailAddress { get; init; }
}

public sealed record IdentifierHttpResponse
{
    public required string Id { get; init; }
}

public sealed record ActiveCrewMemberListHttpResponse
{
    public required ActiveCrewMemberHttpResponse[] Members { get; init; }
}

public sealed record ActiveCrewMemberHttpResponse
{
    public required string WorkOsUserId { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public required MembershipRole Role { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

public sealed record InvitationListHttpResponse
{
    public required InvitationHttpResponse[] Invitations { get; init; }
}

public sealed record InvitationHttpResponse
{
    public required string Id { get; init; }

    public required string OrganizationId { get; init; }

    public required string EmailAddress { get; init; }

    public required InvitationState State { get; init; }

    public required MembershipRole Role { get; init; }

    public required string InviterUserId { get; init; }
}

public enum MembershipRole
{
    Admin,
    Member,
}

public enum MembershipStatus
{
    Active,
    Inactive,
}

public enum InvitationState
{
    Pending,
    Accepted,
    Revoked,
    Expired,
}

public enum FakeWorkOsOperation
{
    FindOrganizationByExternalId,
    CreateOrganization,
    DeleteOrganization,
    FindActiveMembership,
    CreateMembership,
    UpdateMembership,
    DeactivateMembership,
    ListActiveMembers,
    ListPendingInvitations,
    SendInvitation,
}
