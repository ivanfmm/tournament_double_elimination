using Microsoft.EntityFrameworkCore;
using TournamentServices.TestBuilders;

namespace TournamentServices.Repositories.Tests;

public class GroupRepositoryTests
{
    // Un nombre de base unico por test para que corran en paralelo.
    private static DbContextOptions<TournamentDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TournamentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task AddAsync_GuardaGrupo()
    {
        var options = CreateOptions();
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Grupo A").Build();

        using (var context = new TournamentDbContext(options))
        {
            await new GroupRepository(context).AddAsync(group);
        }

        using var readContext = new TournamentDbContext(options);
        var saved = await readContext.Groups.FirstOrDefaultAsync(g => g.Id == "g-1");
        Assert.NotNull(saved);
        Assert.Equal("Grupo A", saved.Name);
        Assert.Equal("t-1", saved.TournamentId);
    }

    [Fact]
    public async Task GetByTournamentAsync_RegresaListaVaciaSiNoHayGrupos()
    {
        using var context = new TournamentDbContext(CreateOptions());

        var result = await new GroupRepository(context).GetByTournamentAsync("t-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTournamentAsync_RegresaSoloLosGruposDelTorneo()
    {
        using var context = new TournamentDbContext(CreateOptions());
        context.Groups.Add(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("A").Build());
        context.Groups.Add(new GroupBuilder().WithId("g-2").WithTournamentId("t-1").WithName("B").Build());
        context.Groups.Add(new GroupBuilder().WithId("g-3").WithTournamentId("t-2").WithName("A").Build());
        await context.SaveChangesAsync();

        var result = await new GroupRepository(context).GetByTournamentAsync("t-1");

        Assert.Equal(2, result.Count);
        Assert.All(result, g => Assert.Equal("t-1", g.TournamentId));
    }

    [Fact]
    public async Task GetByIdAsync_RegresaGrupoConSusEquipos()
    {
        var options = CreateOptions();
        using (var context = new TournamentDbContext(options))
        {
            context.Groups.Add(new GroupBuilder().WithId("g-1").WithTeam("team-1").WithTeam("team-2").Build());
            await context.SaveChangesAsync();
        }

        using var readContext = new TournamentDbContext(options);
        var result = await new GroupRepository(readContext).GetByIdAsync("g-1");

        Assert.NotNull(result);
        Assert.Equal(new[] { "team-1", "team-2" }, result.TeamIds);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaNullSiNoExiste()
    {
        using var context = new TournamentDbContext(CreateOptions());

        var result = await new GroupRepository(context).GetByIdAsync("no-existe");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_GuardaNombreYEquiposNuevos()
    {
        var options = CreateOptions();
        using (var context = new TournamentDbContext(options))
        {
            context.Groups.Add(new GroupBuilder().WithId("g-1").WithName("Viejo").Build());
            await context.SaveChangesAsync();
        }

        using (var context = new TournamentDbContext(options))
        {
            var repository = new GroupRepository(context);
            var group = await repository.GetByIdAsync("g-1");
            group!.Name = "Nuevo";
            group.AddTeam("team-1");
            await repository.UpdateAsync(group);
        }

        using var readContext = new TournamentDbContext(options);
        var saved = await readContext.Groups.FirstAsync(g => g.Id == "g-1");
        Assert.Equal("Nuevo", saved.Name);
        Assert.Equal(new[] { "team-1" }, saved.TeamIds);
    }

    [Fact]
    public async Task DeleteAsync_EliminaGrupo()
    {
        var options = CreateOptions();
        using (var context = new TournamentDbContext(options))
        {
            context.Groups.Add(new GroupBuilder().WithId("g-1").Build());
            await context.SaveChangesAsync();
        }

        using (var context = new TournamentDbContext(options))
        {
            await new GroupRepository(context).DeleteAsync("g-1");
        }

        using var readContext = new TournamentDbContext(options);
        Assert.False(await readContext.Groups.AnyAsync(g => g.Id == "g-1"));
    }

    [Fact]
    public async Task DeleteAsync_NoTronaSiNoExiste()
    {
        using var context = new TournamentDbContext(CreateOptions());

        var exception = await Record.ExceptionAsync(() => new GroupRepository(context).DeleteAsync("no-existe"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Grupo A")]
    [InlineData("grupo a")]
    [InlineData("  GRUPO A  ")]
    public async Task ExistsByNameInTournamentAsync_RegresaTrueSinImportarMayusculas(string name)
    {
        using var context = new TournamentDbContext(CreateOptions());
        context.Groups.Add(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Grupo A").Build());
        await context.SaveChangesAsync();

        var result = await new GroupRepository(context).ExistsByNameInTournamentAsync("t-1", name);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByNameInTournamentAsync_RegresaFalseSiElNombreEstaEnOtroTorneo()
    {
        using var context = new TournamentDbContext(CreateOptions());
        context.Groups.Add(new GroupBuilder().WithId("g-1").WithTournamentId("t-2").WithName("Grupo A").Build());
        await context.SaveChangesAsync();

        var result = await new GroupRepository(context).ExistsByNameInTournamentAsync("t-1", "Grupo A");

        Assert.False(result);
    }
}
