using TournamentServices.Domain.Brackets;
using Xunit;
namespace TournamentServices.Domain.Tests.Brackets;

public class RandomTeamSeederTests
{
    [Fact]
    public void Seed_WithFixedRandomSeed_IsDeterministic()
    {
        var teamIds = new List<string> { "team-1", "team-2", "team-3", "team-4" };
        var seeder1 = new RandomTeamSeeder(new Random(42));
        var seeder2 = new RandomTeamSeeder(new Random(42));

        var result1 = seeder1.Seed(teamIds);
        var result2 = seeder2.Seed(teamIds);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Seed_DoesNotAddOrRemoveTeams()
    {
        var teamIds = new List<string> { "team-1", "team-2", "team-3", "team-4", "team-5" };
        var seeder = new RandomTeamSeeder(new Random(1));

        var result = seeder.Seed(teamIds);

        Assert.Equal(teamIds.Count, result.Count);
        foreach (var teamId in teamIds)
        {
            Assert.Contains(teamId, result);
        }
    }

    [Fact]
    public void Seed_DoesNotMutateOriginalList()
    {
        // Importante: Seed no debe alterar la lista que le pasaron,
        // porque quien la llama (el Delegate) podría seguir usándola.
        var teamIds = new List<string> { "team-1", "team-2", "team-3", "team-4" };
        var originalCopy = new List<string>(teamIds);
        var seeder = new RandomTeamSeeder(new Random(7));

        seeder.Seed(teamIds);

        Assert.Equal(originalCopy, teamIds);
    }

    [Fact]
    public void Seed_WithSingleTeam_ReturnsSameTeam()
    {
        var teamIds = new List<string> { "team-1" };
        var seeder = new RandomTeamSeeder(new Random(1));

        var result = seeder.Seed(teamIds);

        Assert.Single(result);
        Assert.Equal("team-1", result[0]);
    }

    [Fact]
    public void Seed_WithEmptyList_ReturnsEmptyList()
    {
        var teamIds = new List<string>();
        var seeder = new RandomTeamSeeder(new Random(1));

        var result = seeder.Seed(teamIds);

        Assert.Empty(result);
    }

    [Fact]
    public void Seed_WithDifferentSeeds_CanProduceDifferentOrders()
    {
        // No es un test de "debe ser distinto siempre" (eso sería flaky
        // por definición, un shuffle a veces coincide por azar).
        // Es solo una confirmación de que el shuffle SÍ hace algo,
        // probando semillas donde sabemos que el orden cambia.
        var teamIds = new List<string> { "team-1", "team-2", "team-3", "team-4", "team-5", "team-6" };

        var result1 = new RandomTeamSeeder(new Random(1)).Seed(teamIds);
        var result2 = new RandomTeamSeeder(new Random(2)).Seed(teamIds);

        Assert.NotEqual(result1, result2);
    }
}