using PickleCrew.Api.Crews;
using Xunit;

namespace PickleCrew.UnitTests.Crews;

public sealed class ChangeCrewNameValidationTests
{
    [Fact]
    public void Validate_WhenNameIsValid_ShouldReturnNoErrors()
    {
        var request = new ChangeCrewNameHttpRequest
        {
            Name = "Kitchen Legends",
        };

        var errors = ChangeCrewNameEndpoint.Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsBlank_ShouldReturnNameError(string name)
    {
        var request = new ChangeCrewNameHttpRequest { Name = name };

        var errors = ChangeCrewNameEndpoint.Validate(request);

        Assert.Contains(nameof(request.Name), errors);
    }

    [Fact]
    public void Validate_WhenTrimmedNameExceedsMaximumLength_ShouldReturnNameError()
    {
        var request = new ChangeCrewNameHttpRequest
        {
            Name = $" {new string('a', 101)} ",
        };

        var errors = ChangeCrewNameEndpoint.Validate(request);

        Assert.Contains(nameof(request.Name), errors);
    }
}
