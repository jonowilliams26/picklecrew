using System.Net.Mail;
using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using WorkOS;

namespace PickleCrew.Api.Crews;

public sealed record SendCrewInvitationHttpRequest
{
    public required string EmailAddress { get; init; }
}

public sealed record SendCrewInvitationHttpResponse
{
    public required string EmailAddress { get; init; }

    public required CrewInvitationOutcomeStatus Status { get; init; }

    public string? Message { get; init; }
}

public enum CrewInvitationOutcomeStatus
{
    Sent,
    AlreadyActiveMember,
    AlreadyPending,
    Failed,
}

internal sealed class SendCrewInvitationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/invitations", HandleAsync)
            .WithName("SendCrewInvitation")
            .WithSummary("Sends an invitation to join the active crew.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator)
            .WithRequestValidation<SendCrewInvitationHttpRequest>(Validate);
    }

    internal static Dictionary<string, string[]> Validate(
        SendCrewInvitationHttpRequest request)
    {
        var emailAddress = NormalizeEmailAddress(request.EmailAddress);
        if (!IsValidEmailAddress(emailAddress))
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.EmailAddress)] = ["Enter a valid email address."],
            };
        }

        return [];
    }

    internal static async Task<Ok<SendCrewInvitationHttpResponse>> HandleAsync(
        SendCrewInvitationHttpRequest request,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var emailAddress = NormalizeEmailAddress(request.EmailAddress);
        var roster = await crewService.ListCrewMembersAsync(cancellationToken);
        var isActiveMember = roster.ActiveMembers.Any(member => string.Equals(
            member.EmailAddress,
            emailAddress,
            StringComparison.OrdinalIgnoreCase
        ));

        if (isActiveMember)
        {
            var activeMemberOutcome = CreateOutcome(
                emailAddress,
                CrewInvitationOutcomeStatus.AlreadyActiveMember,
                "Already an active member of this crew."
            );

            return TypedResults.Ok(activeMemberOutcome);
        }

        var hasPendingInvitation = roster.PendingInvitations.Any(invitation =>
            string.Equals(
                invitation.EmailAddress,
                emailAddress,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (hasPendingInvitation)
        {
            var pendingInvitationOutcome = CreateOutcome(
                emailAddress,
                CrewInvitationOutcomeStatus.AlreadyPending,
                "An invitation is already pending."
            );

            return TypedResults.Ok(pendingInvitationOutcome);
        }

        var outcome = await SendInvitationAsync(
            crewService,
            emailAddress,
            cancellationToken
        );

        return TypedResults.Ok(outcome);
    }

    private static async Task<SendCrewInvitationHttpResponse> SendInvitationAsync(
        ICrewService crewService,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            await crewService.SendCrewInvitationAsync(emailAddress, cancellationToken);

            return CreateOutcome(emailAddress, CrewInvitationOutcomeStatus.Sent);
        }
        catch (ApiException)
        {
            return CreateFailedOutcome(emailAddress);
        }
        catch (HttpRequestException)
        {
            return CreateFailedOutcome(emailAddress);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateFailedOutcome(emailAddress);
        }
    }

    private static SendCrewInvitationHttpResponse CreateFailedOutcome(
        string emailAddress)
    {
        return CreateOutcome(
            emailAddress,
            CrewInvitationOutcomeStatus.Failed,
            "Could not send this invitation. Try again."
        );
    }

    private static SendCrewInvitationHttpResponse CreateOutcome(
        string emailAddress,
        CrewInvitationOutcomeStatus status,
        string? message = null)
    {
        return new SendCrewInvitationHttpResponse
        {
            EmailAddress = emailAddress,
            Status = status,
            Message = message,
        };
    }

    private static string NormalizeEmailAddress(string? emailAddress)
    {
        return emailAddress?.Trim() ?? string.Empty;
    }

    private static bool IsValidEmailAddress(string emailAddress)
    {
        return MailAddress.TryCreate(emailAddress, out var parsedAddress)
            && string.Equals(
                parsedAddress.Address,
                emailAddress,
                StringComparison.OrdinalIgnoreCase
            );
    }
}
