using TournamentServices.Domain.Brackets;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests.Brackets;

public class BracketSlotTests
{
    [Fact]
    public void ForTeam_WithValidId_CreatesNonByeSlot()
    {
        var slot = BracketSlot.ForTeam("team-1");

        Assert.Equal("team-1", slot.TeamId);
        Assert.False(slot.IsBye);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ForTeam_WithInvalidId_ThrowsDomainValidationException(string? invalidId)
    {
        Assert.Throws<DomainValidationException>(() => BracketSlot.ForTeam(invalidId!));
    }

    [Fact]
    public void Bye_CreatesSlotWithNullTeamIdAndIsByeTrue()
    {
        var slot = BracketSlot.Bye();

        Assert.Null(slot.TeamId);
        Assert.True(slot.IsBye);
    }

    [Fact]
    public void TwoTeamSlots_WithSameTeamId_AreEqual()
    {
        var slot1 = BracketSlot.ForTeam("team-1");
        var slot2 = BracketSlot.ForTeam("team-1");

        Assert.Equal(slot1, slot2);
    }

    [Fact]
    public void TwoByeSlots_AreEqual()
    {
        // Ambos son "posición vacía", así que como value objects son iguales
        // entre sí aunque se hayan creado en llamadas distintas.
        var bye1 = BracketSlot.Bye();
        var bye2 = BracketSlot.Bye();

        Assert.Equal(bye1, bye2);
    }

    [Fact]
    public void TeamSlotAndByeSlot_AreNotEqual()
    {
        var teamSlot = BracketSlot.ForTeam("team-1");
        var byeSlot = BracketSlot.Bye();

        Assert.NotEqual(teamSlot, byeSlot);
    }
}