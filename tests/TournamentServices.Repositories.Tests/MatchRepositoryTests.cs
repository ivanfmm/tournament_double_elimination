using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.TestBuilders;

namespace TournamentServices.Repositories.Tests;

public class MatchRepositoryTests
{
    // Cada test usa un nombre de base distinto para que puedan correr
    // en paralelo sin pisarse los datos entre si.
    private static TournamentDbContext NewDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<TournamentDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TournamentDbContext(options);
    }

    private static string NewDbName() => Guid.NewGuid().ToString();

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheMatch()
    {
        var dbName = NewDbName();
        await using var db = NewDb(dbName);
        var repository = new MatchRepository(db);

        var match = new MatchBuilder()
            .WithId("m1")
            .WithTournamentId("tour-1")
            .WithHomeTeam("t1")
            .WithVisitorTeam("t2")
            .Build();

        await repository.AddAsync(match);
        var found = await repository.GetByIdAsync("m1");

        Assert.NotNull(found);
        Assert.Equal("tour-1", found!.TournamentId);
        Assert.Equal("t1", found.HomeTeamId);
        Assert.Equal("t2", found.VisitorTeamId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMatchDoesNotExist_ReturnsNull()
    {
        await using var db = NewDb(NewDbName());
        var repository = new MatchRepository(db);

        var found = await repository.GetByIdAsync("no-existe");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByTournamentAsync_ReturnsOnlyMatchesOfThatTournament()
    {
        await using var db = NewDb(NewDbName());
        var repository = new MatchRepository(db);

        await repository.AddAsync(new MatchBuilder()
            .WithId("m1").WithTournamentId("tour-1")
            .WithHomeTeam("t1").WithVisitorTeam("t2").Build());

        await repository.AddAsync(new MatchBuilder()
            .WithId("m2").WithTournamentId("tour-1")
            .WithHomeTeam("t3").WithVisitorTeam("t4").Build());

        await repository.AddAsync(new MatchBuilder()
            .WithId("m3").WithTournamentId("tour-2")
            .WithHomeTeam("t5").WithVisitorTeam("t6").Build());

        var result = await repository.GetByTournamentAsync("tour-1");

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal("tour-1", m.TournamentId));
    }

    [Fact]
    public async Task GetByTournamentAsync_WhenTournamentHasNoMatches_ReturnsEmptyList()
    {
        await using var db = NewDb(NewDbName());
        var repository = new MatchRepository(db);

        var result = await repository.GetByTournamentAsync("tour-vacio");

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsScoreWinnerAndIsCompleted()
    {
        var dbName = NewDbName();

        await using (var db = NewDb(dbName))
        {
            var repository = new MatchRepository(db);
            await repository.AddAsync(new MatchBuilder()
                .WithId("m1").WithTournamentId("tour-1")
                .WithHomeTeam("t1").WithVisitorTeam("t2").Build());
        }

        // Context nuevo para confirmar que el cambio si viajo a la "base"
        // y no solo al cache de seguimiento del context anterior.
        await using (var db = NewDb(dbName))
        {
            var repository = new MatchRepository(db);
            var match = await repository.GetByIdAsync("m1");
            match!.SetScore(new Score(3, 1));
            await repository.UpdateAsync(match);
        }

        await using (var db = NewDb(dbName))
        {
            var repository = new MatchRepository(db);
            var reloaded = await repository.GetByIdAsync("m1");

            Assert.NotNull(reloaded);
            Assert.Equal(3, reloaded!.Score.HomeTeamScore);
            Assert.Equal(1, reloaded.Score.VisitorTeamScore);
            Assert.Equal(Winner.Home, reloaded.Winner);
            Assert.True(reloaded.IsCompleted);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheMatch()
    {
        await using var db = NewDb(NewDbName());
        var repository = new MatchRepository(db);

        await repository.AddAsync(new MatchBuilder()
            .WithId("m1").WithTournamentId("tour-1")
            .WithHomeTeam("t1").WithVisitorTeam("t2").Build());

        await repository.DeleteAsync("m1");

        Assert.Null(await repository.GetByIdAsync("m1"));
    }

    [Fact]
    public async Task DeleteAsync_WhenMatchDoesNotExist_DoesNothing()
    {
        await using var db = NewDb(NewDbName());
        var repository = new MatchRepository(db);

        // No debe lanzar: quien decide el 404 es el Delegate.
        await repository.DeleteAsync("no-existe");
    }
}
