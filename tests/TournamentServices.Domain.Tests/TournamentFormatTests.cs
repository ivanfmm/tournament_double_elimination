using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class TournamentFormatTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var format = new TournamentFormat(4, 2, TournamentType.RoundRobin);

        Assert.Equal(4, format.MaxTeamsPerGroup);
        Assert.Equal(2, format.NumberOfGroups);
        Assert.Equal(TournamentType.RoundRobin, format.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxTeamsPerGroup_ThrowsDomainValidationException(int invalidValue)
    {
        Assert.Throws<DomainValidationException>(() =>
            new TournamentFormat(invalidValue, 2, TournamentType.RoundRobin));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidNumberOfGroups_ThrowsDomainValidationException(int invalidValue)
    {
        Assert.Throws<DomainValidationException>(() =>
            new TournamentFormat(4, invalidValue, TournamentType.RoundRobin));
    }

    [Fact]
    public void TwoFormats_WithSameValues_AreEqual()
    {
        var format1 = new TournamentFormat(4, 2, TournamentType.Nfl);
        var format2 = new TournamentFormat(4, 2, TournamentType.Nfl);

        Assert.Equal(format1, format2);
    }
}