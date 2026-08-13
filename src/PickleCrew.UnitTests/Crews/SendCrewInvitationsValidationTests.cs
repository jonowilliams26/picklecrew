using PickleCrew.Api.Crews;
using Xunit;

namespace PickleCrew.UnitTests.Crews;

public sealed class SendCrewInvitationsValidationTests
{
    [Fact]
    public void Validate_WhenOneEmailAddressIsValid_ShouldReturnNoValidationErrors()
    {
        var errors = SendCrewInvitationsEndpoint.Validate(
            new SendCrewInvitationsHttpRequest
            {
                EmailAddresses = [" friend@example.com "],
            }
        );

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData()]
    [InlineData("friend@example.com", "other@example.com")]
    public void Validate_WhenRecipientCountIsNotOne_ShouldReturnValidationError(
        params string[] emailAddresses)
    {
        var errors = SendCrewInvitationsEndpoint.Validate(
            new SendCrewInvitationsHttpRequest
            {
                EmailAddresses = emailAddresses,
            }
        );

        Assert.Contains(nameof(SendCrewInvitationsHttpRequest.EmailAddresses), errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("Friend <friend@example.com>")]
    public void Validate_WhenEmailAddressIsInvalid_ShouldReturnValidationError(
        string emailAddress)
    {
        var errors = SendCrewInvitationsEndpoint.Validate(
            new SendCrewInvitationsHttpRequest
            {
                EmailAddresses = [emailAddress],
            }
        );

        Assert.Contains(nameof(SendCrewInvitationsHttpRequest.EmailAddresses), errors);
    }
}
