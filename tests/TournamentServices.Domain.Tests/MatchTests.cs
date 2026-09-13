// tests/TournamentServices.Domain.Tests/MatchTests.cs
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class MatchTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor", "group-1");

        Assert.Equal("match-1", match.Id);
        Assert.Equal("tournament-1", match.TournamentId);
        Assert.Equal("group-1", match.GroupId);
        Assert.Equal("team-home", match.HomeTeamId);
        Assert.Equal("team-visitor", match.VisitorTeamId);
    }

    [Fact]
    public void Constructor_WithoutGroupId_AllowsNullGroup()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        Assert.Null(match.GroupId);
    }

    [Fact]
    public void Constructor_NewMatch_IsNotCompletedAndHasNoWinner()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        Assert.False(match.IsCompleted);
        Assert.Null(match.Winner);
        Assert.Equal(0, match.Score.HomeTeamScore);
        Assert.Equal(0, match.Score.VisitorTeamScore);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidHomeTeamId_ThrowsDomainValidationException(string? invalidId)
    {
        Assert.Throws<DomainValidationException>(() => new Match("match-1", "tournament-1", invalidId!, "team-visitor"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidVisitorTeamId_ThrowsDomainValidationException(string? invalidId)
    {
        Assert.Throws<DomainValidationException>(() => new Match("match-1", "tournament-1", "team-home", invalidId!));
    }

    [Fact]
    public void Constructor_WithSameHomeAndVisitorTeam_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new Match("match-1", "tournament-1", "team-1", "team-1"));
    }

    [Fact]
    public void ParameterlessConstructor_AllowsBuilderStyleAssignment()
    {
        var match = new Match
        {
            Id = "match-1",
            TournamentId = "tournament-1",
            HomeTeamId = "team-home",
            VisitorTeamId = "team-visitor"
        };

        Assert.Equal("team-home", match.HomeTeamId);
        Assert.Equal("team-visitor", match.VisitorTeamId);
    }

    [Fact]
    public void BuilderStyleAssignment_WithSameTeamOnBothSides_ThrowsDomainValidationException()
    {
        var match = new Match
        {
            Id = "match-1",
            TournamentId = "tournament-1",
            HomeTeamId = "team-1"
        };

        Assert.Throws<DomainValidationException>(() => match.VisitorTeamId = "team-1");
    }

    [Fact]
    public void SetScore_HomeTeamScoresHigher_SetsWinnerHomeAndCompletesMatch()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        match.SetScore(new Score(3, 1));

        Assert.True(match.IsCompleted);
        Assert.Equal(Winner.Home, match.Winner);
        Assert.Equal(3, match.Score.HomeTeamScore);
        Assert.Equal(1, match.Score.VisitorTeamScore);
    }

    [Fact]
    public void SetScore_VisitorTeamScoresHigher_SetsWinnerVisitorAndCompletesMatch()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        match.SetScore(new Score(1, 3));

        Assert.True(match.IsCompleted);
        Assert.Equal(Winner.Visitor, match.Winner);
    }

    [Fact]
    public void SetScore_TiedScore_CompletesMatchWithNullWinner()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        match.SetScore(new Score(2, 2));

        Assert.True(match.IsCompleted);
        Assert.Null(match.Winner);
    }

    [Fact]
    public void SetScore_WithNegativeScore_ThrowsDomainValidationException()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        Assert.Throws<DomainValidationException>(() => match.SetScore(new Score(-1, 0)));
    }

    [Fact]
    public void SetScore_WithNegativeScore_DoesNotModifyMatchState()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        try
        {
            match.SetScore(new Score(-1, 0));
        }
        catch (DomainValidationException)
        {
            // ignorado a propósito para revisar el estado después
        }

        Assert.False(match.IsCompleted);
        Assert.Null(match.Winner);
    }

    [Fact]
    public void SetScore_CalledTwice_OverwritesPreviousScoreAndWinner()
    {
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        match.SetScore(new Score(3, 1));
        match.SetScore(new Score(0, 5));

        Assert.Equal(Winner.Visitor, match.Winner);
        Assert.Equal(0, match.Score.HomeTeamScore);
        Assert.Equal(5, match.Score.VisitorTeamScore);
    }
}