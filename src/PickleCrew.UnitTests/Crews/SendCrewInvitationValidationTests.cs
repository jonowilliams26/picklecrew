using PickleCrew.Api.Crews;
using Xunit;

namespace PickleCrew.UnitTests.Crews;

public sealed class SendCrewInvitationValidationTests
{
    [Fact]
    public void Validate_WhenEmailAddressIsValid_ShouldReturnNoValidationErrors()
    {
        var errors = SendCrewInvitationEndpoint.Validate(
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = " friend@example.com ",
            }
        );

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("Friend <friend@example.com>")]
    public void Validate_WhenEmailAddressIsInvalid_ShouldReturnValidationError(
        string emailAddress)
    {
        var errors = SendCrewInvitationEndpoint.Validate(
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = emailAddress,
            }
        );

        Assert.Contains(nameof(SendCrewInvitationHttpRequest.EmailAddress), errors);
    }
}
