using System.Net.Mail;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record SendCrewInvitationsHttpRequest
{
    public required string[] EmailAddresses { get; init; }
}

public sealed record SendCrewInvitationsHttpResponse
{
    public required CrewInvitationRecipientHttpResponse[] Outcomes { get; init; }
}

public sealed record CrewInvitationRecipientHttpResponse
{
    public required string EmailAddress { get; init; }

    public required CrewInvitationOutcomeStatus Status { get; init; }

    public string? Message { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<CrewInvitationOutcomeStatus>))]
public enum CrewInvitationOutcomeStatus
{
    Sent,
    AlreadyActiveMember,
    AlreadyPending,
    Failed,
}

internal sealed class SendCrewInvitationsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/invitations", HandleAsync)
            .WithName("SendCrewInvitations")
            .WithSummary("Sends crew invitations during onboarding.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator)
            .WithRequestValidation<SendCrewInvitationsHttpRequest>(Validate);
    }

    internal static Dictionary<string, string[]> Validate(
        SendCrewInvitationsHttpRequest request)
    {
        if (request.EmailAddresses is not { Length: 1 })
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.EmailAddresses)] =
                    ["Enter exactly one email address."],
            };
        }

        var emailAddress = request.EmailAddresses[0]?.Trim() ?? string.Empty;
        var isValidEmailAddress = MailAddress.TryCreate(emailAddress, out var parsedAddress)
            && string.Equals(
                parsedAddress.Address,
                emailAddress,
                StringComparison.OrdinalIgnoreCase
            );

        if (!isValidEmailAddress)
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.EmailAddresses)] =
                    ["Enter a valid email address."],
            };
        }

        return [];
    }

    internal static async Task<Ok<SendCrewInvitationsHttpResponse>> HandleAsync(
        SendCrewInvitationsHttpRequest request,
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CancellationToken cancellationToken)
    {
        var emailAddress = request.EmailAddresses[0].Trim();
        await workOs.SendCrewInvitationAsync(
            crewUserContext.OrganizationId,
            emailAddress,
            crewUserContext.UserId,
            cancellationToken
        );

        return TypedResults.Ok(new SendCrewInvitationsHttpResponse
        {
            Outcomes =
            [
                new CrewInvitationRecipientHttpResponse
                {
                    EmailAddress = emailAddress,
                    Status = CrewInvitationOutcomeStatus.Sent,
                },
            ],
        });
    }
}
