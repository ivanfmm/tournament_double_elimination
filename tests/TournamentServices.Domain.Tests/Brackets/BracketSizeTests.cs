using TournamentServices.Domain.Brackets;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests.Brackets;

public class BracketSizeTests
{
    [Fact]
    public void Constructor_With32Teams_ProducesExactBracketWithNoByes()
    {
        var bracketSize = new BracketSize(32);

        Assert.Equal(32, bracketSize.TeamCount);
        Assert.Equal(32, bracketSize.TotalSlots);
        Assert.Equal(0, bracketSize.ByeCount);
        Assert.Equal(5, bracketSize.Rounds);
    }

    [Fact]
    public void Constructor_With20Teams_RoundsUpTo32WithByes()
    {
        var bracketSize = new BracketSize(20);

        Assert.Equal(20, bracketSize.TeamCount);
        Assert.Equal(32, bracketSize.TotalSlots);
        Assert.Equal(12, bracketSize.ByeCount);
    }

    [Theory]
    [InlineData(2, 2, 0, 1)]
    [InlineData(4, 4, 0, 2)]
    [InlineData(8, 8, 0, 3)]
    [InlineData(16, 16, 0, 4)]
    [InlineData(64, 64, 0, 6)]
    public void Constructor_WithExactPowerOfTwo_HasNoByes(
        int teamCount, int expectedSlots, int expectedByes, int expectedRounds)
    {
        var bracketSize = new BracketSize(teamCount);

        Assert.Equal(expectedSlots, bracketSize.TotalSlots);
        Assert.Equal(expectedByes, bracketSize.ByeCount);
        Assert.Equal(expectedRounds, bracketSize.Rounds);
    }

    [Theory]
    [InlineData(3, 4, 1)]    // 3 equipos -> bracket de 4, 1 bye
    [InlineData(5, 8, 3)]    // 5 equipos -> bracket de 8, 3 byes
    [InlineData(9, 16, 7)]   // 9 equipos -> bracket de 16, 7 byes
    [InlineData(17, 32, 15)] // 17 equipos -> bracket de 32, 15 byes
    [InlineData(33, 64, 31)] // 33 equipos -> bracket de 64, 31 byes
    public void Constructor_WithNonPowerOfTwo_RoundsUpAndCalculatesByes(
        int teamCount, int expectedSlots, int expectedByes)
    {
        var bracketSize = new BracketSize(teamCount);

        Assert.Equal(expectedSlots, bracketSize.TotalSlots);
        Assert.Equal(expectedByes, bracketSize.ByeCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-5)]
    public void Constructor_WithFewerThanTwoTeams_ThrowsDomainValidationException(int invalidCount)
    {
        Assert.Throws<DomainValidationException>(() => new BracketSize(invalidCount));
    }

    [Fact]
    public void TwoBracketSizes_WithSameTeamCount_AreEqual()
    {
        var bracket1 = new BracketSize(20);
        var bracket2 = new BracketSize(20);

        Assert.Equal(bracket1, bracket2);
    }
}