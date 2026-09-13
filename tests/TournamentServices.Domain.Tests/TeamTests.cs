using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class TeamTests
{
    [Fact]
    public void Constructor_WithValidIdAndName_SetsProperties()
    {
        var team = new Team("team-1", "River Plate");

        Assert.Equal("team-1", team.Id);
        Assert.Equal("River Plate", team.Name);
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromName()
    {
        var team = new Team("team-1", "  River Plate  ");

        Assert.Equal("River Plate", team.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsDomainValidationException(string? invalidName)
    {
        Assert.Throws<DomainValidationException>(() => new Team("team-1", invalidName!));
    }

    [Fact]
    public void Name_SetToValidValue_UpdatesName()
    {
        // Arrange
        var team = new Team("team-1", "River Plate");

        // Act
        team.Name = "Boca Juniors";

        // Assert
        Assert.Equal("Boca Juniors", team.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_SetToInvalidValue_ThrowsAndKeepsPreviousValue(string? invalidName)
    {
        var team = new Team("team-1", "River Plate");

        Assert.Throws<DomainValidationException>(() => team.Name = invalidName!);
        Assert.Equal("River Plate", team.Name);
    }

    [Fact]
    public void ParameterlessConstructor_AllowsBuilderStylePropertyAssignment()
    {
        var team = new Team
        {
            Id = "team-1",
            Name = "River Plate"
        };

        Assert.Equal("team-1", team.Id);
        Assert.Equal("River Plate", team.Name);
    }
}