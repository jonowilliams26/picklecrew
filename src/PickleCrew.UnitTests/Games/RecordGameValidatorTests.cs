using PickleCrew.Api.Data;
using PickleCrew.Api.Games.RecordGame;
using Xunit;

namespace PickleCrew.UnitTests.Games;

public sealed class RecordGameValidatorTests
{
    [Fact]
    public void Validate_WhenSinglesGameIsValid_ShouldReturnNoErrors()
    {
        var errors = RecordGameValidator.Validate(CreateSinglesRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenMixedDoublesGameIsValid_ShouldReturnNoErrors()
    {
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides = [
                CreateSide(11, CreateMember("user_one"), CreateGuest("Jamie")),
                CreateSide(8, CreateMember("user_two"), CreateGuest("Taylor")),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenGameDoesNotHaveTwoSides_ShouldReturnSidesError()
    {
        var request = CreateSinglesRequest() with
        {
            Sides = [CreateSide(11, CreateMember("user_one"))],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains(nameof(request.Sides), errors);
    }

    [Theory]
    [InlineData(GameFormat.Singles, 2)]
    [InlineData(GameFormat.Doubles, 1)]
    public void Validate_WhenSideHasWrongParticipantCount_ShouldReturnParticipantError(
        GameFormat format,
        int participantCount)
    {
        var participants = Enumerable.Range(0, participantCount)
            .Select(index => CreateMember($"user_{index}"))
            .ToArray();
        var request = new RecordGameHttpRequest
        {
            Format = format,
            Sides = [
                CreateSide(11, participants),
                CreateSide(
                    7,
                    format == GameFormat.Singles
                        ? [CreateMember("user_other")]
                        : [CreateMember("user_other"), CreateGuest("Jamie")]
                ),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains("Sides[0].Participants", errors);
    }

    [Fact]
    public void Validate_WhenParticipantHasMemberAndGuestIdentity_ShouldReturnParticipantError()
    {
        var request = CreateSinglesRequest() with
        {
            Sides = [
                CreateSide(
                    11,
                    new RecordGameParticipantHttpRequest
                    {
                        UserId = "user_one",
                        GuestName = "Jamie",
                    }
                ),
                CreateSide(7, CreateMember("user_two")),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains("Sides[0].Participants[0]", errors);
    }

    [Fact]
    public void Validate_WhenMemberIsRepeated_ShouldReturnBothMemberErrors()
    {
        var request = CreateSinglesRequest() with
        {
            Sides = [
                CreateSide(11, CreateMember("user_one")),
                CreateSide(7, CreateMember("user_one")),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains("Sides[0].Participants[0].UserId", errors);
        Assert.Contains("Sides[1].Participants[0].UserId", errors);
    }

    [Fact]
    public void Validate_WhenGuestNameIsBlank_ShouldReturnGuestNameError()
    {
        var request = CreateSinglesRequest() with
        {
            Sides = [
                CreateSide(11, CreateMember("user_one")),
                CreateSide(7, CreateGuest("  ")),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains("Sides[1].Participants[0].GuestName", errors);
    }

    [Fact]
    public void Validate_WhenNormalizedGuestNameIsRepeated_ShouldReturnBothGuestErrors()
    {
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides = [
                CreateSide(11, CreateMember("user_one"), CreateGuest(" Jamie ")),
                CreateSide(7, CreateMember("user_two"), CreateGuest("jamie")),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains("Sides[0].Participants[1].GuestName", errors);
        Assert.Contains("Sides[1].Participants[1].GuestName", errors);
    }

    [Fact]
    public void Validate_WhenEveryParticipantIsGuest_ShouldReturnActiveMemberError()
    {
        var request = CreateSinglesRequest() with
        {
            Sides = [
                CreateSide(11, CreateGuest("Jamie")),
                CreateSide(7, CreateGuest("Taylor")),
            ],
        };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains(nameof(request.Sides), errors);
        Assert.Contains(
            errors[nameof(request.Sides)],
            message => message.Contains("active member", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void Validate_WhenFormatIsUnknown_ShouldReturnFormatError()
    {
        var request = CreateSinglesRequest() with { Format = (GameFormat)99 };

        var errors = RecordGameValidator.Validate(request);

        Assert.Contains(nameof(request.Format), errors);
    }

    private static RecordGameHttpRequest CreateSinglesRequest()
    {
        return new RecordGameHttpRequest
        {
            Format = GameFormat.Singles,
            Sides = [
                CreateSide(11, CreateMember("user_one")),
                CreateSide(7, CreateMember("user_two")),
            ],
        };
    }

    private static RecordGameSideHttpRequest CreateSide(
        int score,
        params RecordGameParticipantHttpRequest[] participants)
    {
        return new RecordGameSideHttpRequest
        {
            Score = score,
            Participants = participants,
        };
    }

    private static RecordGameParticipantHttpRequest CreateMember(string userId)
    {
        return new RecordGameParticipantHttpRequest { UserId = userId };
    }

    private static RecordGameParticipantHttpRequest CreateGuest(string guestName)
    {
        return new RecordGameParticipantHttpRequest { GuestName = guestName };
    }
}
