using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class ScoreTests
{
    [Fact]
    public void Constructor_WithValidScores_SetsProperties()
    {
        var score = new Score(3, 1);

        Assert.Equal(3, score.HomeTeamScore);
        Assert.Equal(1, score.VisitorTeamScore);
    }

    [Fact]
    public void Constructor_WithTiedScores_IsValid()
    {
        var score = new Score(2, 2);

        Assert.Equal(2, score.HomeTeamScore);
        Assert.Equal(2, score.VisitorTeamScore);
    }

    [Fact]
    public void ParameterlessConstructor_DefaultsToZeroZero()
    {
        var score = new Score();

        Assert.Equal(0, score.HomeTeamScore);
        Assert.Equal(0, score.VisitorTeamScore);
    }

    [Fact]
    public void Constructor_WithNegativeHomeScore_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new Score(-1, 0));
    }

    [Fact]
    public void Constructor_WithNegativeVisitorScore_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new Score(0, -1));
    }

    [Fact]
    public void Constructor_WithBothScoresNegative_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new Score(-2, -3));
    }

    [Fact]
    public void TwoScores_WithSameValues_AreEqual()
    {
        var score1 = new Score(3, 1);
        var score2 = new Score(3, 1);

        Assert.Equal(score1, score2);
        Assert.True(score1 == score2);
    }

    [Fact]
    public void TwoScores_WithDifferentValues_AreNotEqual()
    {
        var score1 = new Score(3, 1);
        var score2 = new Score(1, 3);

        Assert.NotEqual(score1, score2);
        Assert.False(score1 == score2);
    }
}