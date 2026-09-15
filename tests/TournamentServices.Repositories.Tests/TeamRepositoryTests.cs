using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;
using TournamentServices.TestBuilders;

namespace TournamentServices.Repositories.Tests;

public class TeamRepositoryTests
{
    private static TournamentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TournamentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TournamentDbContext(options);
    }

    [Fact]
    public async Task AddAsync_GuardaEquipoCorrectamente()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);
        var team = new TeamBuilder().WithId("team-1").WithName("Real Madrid").Build();

        await repository.AddAsync(team);

        var saved = await context.Teams.FirstOrDefaultAsync(t => t.Id == "team-1");
        Assert.NotNull(saved);
        Assert.Equal("Real Madrid", saved.Name);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaEquipoCuandoExiste()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);
        var team = new TeamBuilder().WithId("team-1").WithName("Barcelona").Build();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync("team-1");

        Assert.NotNull(result);
        Assert.Equal("Barcelona", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaNullCuandoNoExiste()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);

        var result = await repository.GetByIdAsync("no-existe");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_RegresaListaVaciaSiNoHayEquipos()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);

        var result = await repository.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_RegresaTodosLosEquipos()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);
        context.Teams.Add(new TeamBuilder().WithId("team-1").WithName("Equipo A").Build());
        context.Teams.Add(new TeamBuilder().WithId("team-2").WithName("Equipo B").Build());
        await context.SaveChangesAsync();

        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ActualizaNombreDelEquipo()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);
        var team = new TeamBuilder().WithId("team-1").WithName("Nombre Viejo").Build();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        team.Name = "Nombre Nuevo";
        await repository.UpdateAsync(team);

        var updated = await context.Teams.FirstOrDefaultAsync(t => t.Id == "team-1");
        Assert.Equal("Nombre Nuevo", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_EliminaEquipoExistente()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);
        var team = new TeamBuilder().WithId("team-1").WithName("A Borrar").Build();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        await repository.DeleteAsync("team-1");

        var result = await context.Teams.FirstOrDefaultAsync(t => t.Id == "team-1");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NoTronaSiElIdNoExiste()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);

        var exception = await Record.ExceptionAsync(() => repository.DeleteAsync("no-existe"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ExistsByNameAsync_RegresaTrueSiElNombreYaExiste()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);
        context.Teams.Add(new TeamBuilder().WithId("team-1").WithName("Chivas").Build());
        await context.SaveChangesAsync();

        var result = await repository.ExistsByNameAsync("Chivas");

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByNameAsync_RegresaFalseSiElNombreNoExiste()
    {
        var context = CreateContext();
        var repository = new TeamRepository(context);

        var result = await repository.ExistsByNameAsync("No Existe FC");

        Assert.False(result);
    }
}
