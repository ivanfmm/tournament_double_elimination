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

    [Fact]
    public async Task AddAsync_PersistsTournament()
    {
        var options = CreateOptions();
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Copa Libertadores").Build();

        await using (var writeContext = new TournamentDbContext(options))
        {
            var repository = new TournamentRepository(writeContext);
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
        var repository = new TournamentRepository(context);

        var result = await repository.GetByIdAsync("no-existe");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTournament_AssemblesGroupsAndMatches()
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
        var repository = new TournamentRepository(readContext);

        var result = await repository.GetByIdAsync("t-1");

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
        var repository = new TournamentRepository(readContext);

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
        var repository = new TournamentRepository(readContext);

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
            var repository = new TournamentRepository(updateContext);
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
    public async Task DeleteAsync_ExistingTournament_ReturnsTrue()
    {
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-1").Build());
            await writeContext.SaveChangesAsync();
        }

        await using var context = new TournamentDbContext(options);
        var repository = new TournamentRepository(context);

        var deleted = await repository.DeleteAsync("t-1");

        Assert.True(deleted);
        Assert.Empty(await context.Tournaments.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingTournament_ReturnsFalse()
    {
        var options = CreateOptions();
        await using var context = new TournamentDbContext(options);
        var repository = new TournamentRepository(context);

        var deleted = await repository.DeleteAsync("no-existe");

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_CascadesToGroupsAndMatches()
    {
        var options = CreateOptions();

        await using (var writeContext = new TournamentDbContext(options))
        {
            writeContext.Tournaments.Add(new TournamentBuilder().WithId("t-1").Build());
            writeContext.Groups.Add(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build());
            writeContext.Matches.Add(new MatchBuilder().WithId("m-1").WithTournamentId("t-1").Build());
            await writeContext.SaveChangesAsync();
        }

        await using var context = new TournamentDbContext(options);
        var repository = new TournamentRepository(context);

        await repository.DeleteAsync("t-1");

        Assert.Empty(await context.Groups.ToListAsync());
        Assert.Empty(await context.Matches.ToListAsync());
    }
}