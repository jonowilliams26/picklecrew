using PickleCrew.Api.Crews;
using Xunit;

namespace PickleCrew.UnitTests.Crews;

public sealed class ChangeCrewMemberRoleValidationTests
{
    [Theory]
    [InlineData(CrewMemberRole.Admin)]
    [InlineData(CrewMemberRole.Member)]
    public void Validate_WhenRoleIsSupported_ShouldReturnNoErrors(
        CrewMemberRole role)
    {
        var request = CreateRequest(role);

        var errors = ChangeCrewMemberRoleEndpoint.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenRoleIsUnsupported_ShouldReturnRoleError()
    {
        var request = CreateRequest((CrewMemberRole)100);

        var errors = ChangeCrewMemberRoleEndpoint.Validate(request);

        var roleErrors = Assert.Single(errors);
        Assert.Equal(nameof(ChangeCrewMemberRoleHttpRequestBody.Role), roleErrors.Key);
        Assert.Equal("Select a valid crew member role.", Assert.Single(roleErrors.Value));
    }

    private static ChangeCrewMemberRoleHttpRequest CreateRequest(CrewMemberRole role)
    {
        return new ChangeCrewMemberRoleHttpRequest
        {
            UserId = "user_123",
            Body = new ChangeCrewMemberRoleHttpRequestBody { Role = role },
        };
    }
}
