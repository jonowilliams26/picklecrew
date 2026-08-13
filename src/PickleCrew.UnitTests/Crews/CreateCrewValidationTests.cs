using PickleCrew.Api.Crews;
using Xunit;

namespace PickleCrew.UnitTests.Crews;

public sealed class CreateCrewValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsBlank_ShouldReturnValidationError(string? name)
    {
        var errors = CreateCrewEndpoint.Validate(
            new CreateCrewHttpRequest { Name = name! }
        );

        Assert.Contains(nameof(CreateCrewHttpRequest.Name), errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_WhenNameLengthIsValid_ShouldReturnNoValidationErrors(int length)
    {
        var errors = CreateCrewEndpoint.Validate(
            new CreateCrewHttpRequest { Name = $" {new string('a', length)} " }
        );

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ShouldReturnValidationError()
    {
        var errors = CreateCrewEndpoint.Validate(
            new CreateCrewHttpRequest { Name = $" {new string('a', 101)} " }
        );

        Assert.Contains(nameof(CreateCrewHttpRequest.Name), errors);
    }
}
