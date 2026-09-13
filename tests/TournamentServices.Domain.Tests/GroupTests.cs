// tests/TournamentServices.Domain.Tests/GroupTests.cs
using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class GroupTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");

        Assert.Equal("group-1", group.Id);
        Assert.Equal("tournament-1", group.TournamentId);
        Assert.Equal("Grupo A", group.Name);
        Assert.Empty(group.TeamIds);
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromName()
    {
        var group = new Group("group-1", "tournament-1", "  Grupo A  ");

        Assert.Equal("Grupo A", group.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsDomainValidationException(string? invalidName)
    {
        Assert.Throws<DomainValidationException>(() => new Group("group-1", "tournament-1", invalidName!));
    }

    [Fact]
    public void ParameterlessConstructor_AllowsBuilderStyleAssignment()
    {
        var group = new Group
        {
            Id = "group-1",
            TournamentId = "tournament-1",
            Name = "Grupo A"
        };

        Assert.Equal("Grupo A", group.Name);
    }

    [Fact]
    public void AddTeam_WithValidId_AddsTeamToList()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");

        group.AddTeam("team-1");

        Assert.Single(group.TeamIds);
        Assert.Contains("team-1", group.TeamIds);
    }

    [Fact]
    public void AddTeam_MultipleDifferentTeams_AddsAll()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");

        group.AddTeam("team-1");
        group.AddTeam("team-2");

        Assert.Equal(2, group.TeamIds.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTeam_WithInvalidId_ThrowsDomainValidationException(string? invalidId)
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");

        Assert.Throws<DomainValidationException>(() => group.AddTeam(invalidId!));
    }

    [Fact]
    public void AddTeam_DuplicateTeam_ThrowsDomainValidationException()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");
        group.AddTeam("team-1");

        Assert.Throws<DomainValidationException>(() => group.AddTeam("team-1"));
    }

    [Fact]
    public void AddTeam_DuplicateTeam_DoesNotModifyList()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");
        group.AddTeam("team-1");

        try
        {
            group.AddTeam("team-1");
        }
        catch (DomainValidationException)
        {
            // ignorado a propósito
        }

        Assert.Single(group.TeamIds);
    }

    [Fact]
    public void RemoveTeam_ExistingTeam_RemovesFromList()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");
        group.AddTeam("team-1");

        group.RemoveTeam("team-1");

        Assert.Empty(group.TeamIds);
    }

    [Fact]
    public void RemoveTeam_NonExistingTeam_ThrowsDomainValidationException()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");

        Assert.Throws<DomainValidationException>(() => group.RemoveTeam("team-1"));
    }

    [Fact]
    public void TeamIds_IsReadOnly_CannotBeModifiedDirectly()
    {
        var group = new Group("group-1", "tournament-1", "Grupo A");
        group.AddTeam("team-1");

        Assert.IsAssignableFrom<IReadOnlyList<string>>(group.TeamIds);
    }
}