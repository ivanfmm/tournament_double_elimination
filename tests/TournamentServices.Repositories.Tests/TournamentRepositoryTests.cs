using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders;
using Xunit;

namespace TournamentServices.Repositories.Tests;

public class TournamentRepositoryTests
{
    private static DbContextOptions<TournamentDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TournamentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    // TournamentRepository usa GroupRepository para armar los grupos con sus equipos
    private static TournamentRepository NewRepository(TournamentDbContext context)
        => new(context, new GroupRepository(context));

    [Fact]
    public async Task AddAsync_PersistsTournament()
    {
        var options = CreateOptions();
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Copa Libertadores").Build();

        await using (var writeContext = new TournamentDbContext(options))
        {
            var repository = NewRepository(writeContext);
            await repository.AddAsync(tournament);
        }

        await using var readContext = new TournamentDbContext(options);
        var stored = await readContext.Tournaments.AsNoTracking().FirstOrDefaultAsync(t => t.Id == "t-1");

        Assert.NotNull(stored);
        Assert.Equal("Copa Libertadores", stored!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingTournament_ReturnsNull()
    {
        var options = CreateOptions();
        await using var context = new TournamentDbContext(options);
        var repository = NewRepository(context);

        var result = await repository.GetByIdAsync("no-existe");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ExistingTournament_AssemblesGroupsAndMatches()
    {
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            var tournament = new TournamentBuilder().WithId("t-1").Build();
            var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithTeam("team-1").Build();
            var match = new MatchBuilder().WithId("m-1").WithTournamentId("t-1").Build();

            writeContext.Tournaments.Add(tournament);
            writeContext.Groups.Add(group);
            writeContext.Matches.Add(match);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new TournamentDbContext(options);
        var repository = NewRepository(readContext);

        var result = await repository.GetByIdWithDetailsAsync("t-1");

        Assert.NotNull(result);
        Assert.Single(result!.Groups);
        Assert.Equal("g-1", result.Groups[0].Id);
        Assert.Single(result.Matches);
        Assert.Equal("m-1", result.Matches[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotAssembleGroupsOrMatches()
    {
        // Confirma la decisión de diseño que discutimos: GetAllAsync es
        // "liviano" a propósito, aunque el torneo SÍ tenga grupos en la DB.
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            var tournament = new TournamentBuilder().WithId("t-1").Build();
            var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build();

            writeContext.Tournaments.Add(tournament);
            writeContext.Groups.Add(group);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new TournamentDbContext(options);
        var repository = NewRepository(readContext);

        var result = await repository.GetAllAsync();

        var loaded = Assert.Single(result);
        Assert.Empty(loaded.Groups);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersistedTournaments()
    {
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-1").Build());
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-2").Build());
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new TournamentDbContext(options);
        var repository = NewRepository(readContext);

        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnFieldsOnly()
    {
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-1").WithName("Nombre Original").Build());
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = new TournamentDbContext(options))
        {
            var repository = NewRepository(updateContext);
            var updated = new TournamentBuilder()
                .WithId("t-1")
                .WithName("Nombre Actualizado")
                .WithFormat(new TournamentFormat(6, 3, TournamentType.Nfl))
                .Build();

            await repository.UpdateAsync(updated);
        }

        await using var verifyContext = new TournamentDbContext(options);
        var stored = await verifyContext.Tournaments.AsNoTracking().FirstAsync(t => t.Id == "t-1");

        Assert.Equal("Nombre Actualizado", stored.Name);
        Assert.Equal(TournamentType.Nfl, stored.Format.Type);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotAssembleGroupsOrMatches()
    {
        // GetByIdAsync es la version liviana; GetByIdWithDetailsAsync arma todo
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-1").Build());
            writeContext.Groups.Add(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build());
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new TournamentDbContext(options);
        var result = await NewRepository(readContext).GetByIdAsync("t-1");

        Assert.NotNull(result);
        Assert.Empty(result!.Groups);
    }

    [Fact]
    public async Task UpdateAsync_PersistsBracketState()
    {
        // Status, orden sorteado y campeon deben sobrevivir a un nuevo DbContext
        var options = CreateOptions();
        var format = new TournamentFormat(8, 1, TournamentType.DoubleElimination);

        await using (var writeContext = new TournamentDbContext(options))
        {
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-1").WithFormat(format).Build());
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = new TournamentDbContext(options))
        {
            var repository = NewRepository(updateContext);
            var tournament = (await repository.GetByIdAsync("t-1"))!;
            tournament.Start(new[] { "b", "a", "c" });
            tournament.Finish("a");
            await repository.UpdateAsync(tournament);
        }

        await using var verifyContext = new TournamentDbContext(options);
        var stored = await verifyContext.Tournaments.AsNoTracking().FirstAsync(t => t.Id == "t-1");

        Assert.Equal(TournamentStatus.Finished, stored.Status);
        Assert.Equal("a", stored.ChampionTeamId);
        Assert.Equal(new[] { "b", "a", "c" }, stored.SeededTeamIds);
    }
}
