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
            "/organizations/{organizationId}/active-member-email-addresses",
            ListActiveMemberEmailAddresses
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
        var user = state.CreateUser(request.Id, request.EmailAddress);

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

    private static Results<Ok<EmailAddressListHttpResponse>, ProblemHttpResult>
        ListActiveMemberEmailAddresses(
            string organizationId,
            FakeWorkOsState state,
            HttpRequest httpRequest)
    {
        if (ShouldFailOperation(
            httpRequest,
            state,
            FakeWorkOsOperation.ListActiveMemberEmailAddresses
        ))
        {
            return CreateFaultProblem(
                FakeWorkOsOperation.ListActiveMemberEmailAddresses
            );
        }

        return TypedResults.Ok(
            new EmailAddressListHttpResponse
            {
                EmailAddresses = state.ListActiveMemberEmailAddresses(organizationId),
            }
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

public sealed record EmailAddressListHttpResponse
{
    public required string[] EmailAddresses { get; init; }
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
    FindActiveMembership,
    CreateMembership,
    ListActiveMemberEmailAddresses,
    ListPendingInvitations,
    SendInvitation,
}
