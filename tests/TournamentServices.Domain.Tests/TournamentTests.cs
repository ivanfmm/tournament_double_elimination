using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class TournamentTests
{
    private static TournamentFormat DefaultFormat(int numberOfGroups = 2) =>
        new(maxTeamsPerGroup: 4, numberOfGroups: numberOfGroups, type: TournamentType.RoundRobin);

    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var format = DefaultFormat();

        var tournament = new Tournament("tournament-1", "Copa Libertadores", format);

        Assert.Equal("tournament-1", tournament.Id);
        Assert.Equal("Copa Libertadores", tournament.Name);
        Assert.Equal(format, tournament.Format);
        Assert.Empty(tournament.Groups);
        Assert.Empty(tournament.Matches);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsDomainValidationException(string? invalidName)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Tournament("tournament-1", invalidName!, DefaultFormat()));
    }

    [Fact]
    public void Constructor_WithNullFormat_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Tournament("tournament-1", "Copa Libertadores", null!));
    }

    [Fact]
    public void ParameterlessConstructor_AllowsBuilderStyleAssignment()
    {
        var tournament = new Tournament
        {
            Id = "tournament-1",
            Name = "Copa Libertadores",
            Format = DefaultFormat()
        };

        Assert.Equal("Copa Libertadores", tournament.Name);
    }

    [Fact]
    public void AddGroup_WithValidGroup_AddsToList()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat());
        var group = new Group("group-1", "tournament-1", "Grupo A");

        tournament.AddGroup(group);

        Assert.Single(tournament.Groups);
        Assert.Contains(group, tournament.Groups);
    }

    [Fact]
    public void AddGroup_NullGroup_ThrowsDomainValidationException()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat());

        Assert.Throws<DomainValidationException>(() => tournament.AddGroup(null!));
    }

    [Fact]
    public void AddGroup_DuplicateNameCaseInsensitive_ThrowsDomainValidationException()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat());
        tournament.AddGroup(new Group("group-1", "tournament-1", "Grupo A"));

        Assert.Throws<DomainValidationException>(() =>
            tournament.AddGroup(new Group("group-2", "tournament-1", "grupo a")));
    }

    [Fact]
    public void AddGroup_ExceedingNumberOfGroups_ThrowsDomainValidationException()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat(numberOfGroups: 1));
        tournament.AddGroup(new Group("group-1", "tournament-1", "Grupo A"));

        Assert.Throws<DomainValidationException>(() =>
            tournament.AddGroup(new Group("group-2", "tournament-1", "Grupo B")));
    }

    [Fact]
    public void AddGroup_ExceedingNumberOfGroups_DoesNotModifyList()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat(numberOfGroups: 1));
        tournament.AddGroup(new Group("group-1", "tournament-1", "Grupo A"));

        try
        {
            tournament.AddGroup(new Group("group-2", "tournament-1", "Grupo B"));
        }
        catch (DomainValidationException)
        {
            // ignorado a propósito
        }

        Assert.Single(tournament.Groups);
    }

    [Fact]
    public void AddMatch_WithValidMatch_AddsToList()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat());
        var match = new Match("match-1", "tournament-1", "team-home", "team-visitor");

        tournament.AddMatch(match);

        Assert.Single(tournament.Matches);
        Assert.Contains(match, tournament.Matches);
    }

    [Fact]
    public void AddMatch_NullMatch_ThrowsDomainValidationException()
    {
        var tournament = new Tournament("tournament-1", "Copa Libertadores", DefaultFormat());

        Assert.Throws<DomainValidationException>(() => tournament.AddMatch(null!));
    }
}