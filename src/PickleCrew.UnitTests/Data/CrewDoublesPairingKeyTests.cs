using PickleCrew.Api.Data;
using Xunit;

namespace PickleCrew.UnitTests.Data;

public sealed class CrewDoublesPairingKeyTests
{
    [Fact]
    public void Create_WhenPlayerIdsAreNotOrdered_ShouldOrderPlayerIds()
    {
        var key = CrewDoublesPairingKey.Create("user_b", "user_a");

        Assert.Equal("user_a", key.Player1Id);
        Assert.Equal("user_b", key.Player2Id);
    }

    [Fact]
    public void Create_WhenPlayerOrderIsReversed_ShouldReturnEqualKeys()
    {
        var firstKey = CrewDoublesPairingKey.Create("user_a", "user_b");
        var secondKey = CrewDoublesPairingKey.Create("user_b", "user_a");

        Assert.Equal(firstKey, secondKey);
    }

    [Fact]
    public void Create_WhenPlayerIdCasingDiffers_ShouldUseOrdinalOrdering()
    {
        var key = CrewDoublesPairingKey.Create("user_a", "user_B");

        Assert.Equal("user_B", key.Player1Id);
        Assert.Equal("user_a", key.Player2Id);
    }

    [Fact]
    public void Create_WhenPlayerIdsAreIdentical_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CrewDoublesPairingKey.Create("user_a", "user_a")
        );
    }
}
