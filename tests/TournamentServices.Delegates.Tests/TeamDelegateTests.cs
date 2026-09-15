using Moq;
using TournamentServices.Domain;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders;

namespace TournamentServices.Delegates.Tests;

public class TeamDelegateTests
{
    private readonly Mock<ITeamRepository> _repoMock;
    private readonly ITeamDelegate _delegate;

    public TeamDelegateTests()
    {
        _repoMock = new Mock<ITeamRepository>();
        _delegate = new TeamDelegate(_repoMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaEquipoCuandoExiste()
    {
        var team = new TeamBuilder().WithId("team-1").WithName("Pumas").Build();
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        var result = await _delegate.GetByIdAsync("team-1");

        Assert.NotNull(result);
        Assert.Equal("Pumas", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaNullCuandoNoExiste()
    {
        _repoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Team?)null);

        var result = await _delegate.GetByIdAsync("no-existe");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_RegresaListaVacia()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Team>());

        var result = await _delegate.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_RegresaMultiplesEquipos()
    {
        var teams = new List<Team>
        {
            new TeamBuilder().WithId("team-1").WithName("Equipo A").Build(),
            new TeamBuilder().WithId("team-2").WithName("Equipo B").Build()
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(teams);

        var result = await _delegate.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateAsync_GuardaEquipoCuandoNombreEsUnico()
    {
        var team = new TeamBuilder().WithId("team-1").WithName("Nuevo Equipo").Build();
        _repoMock.Setup(r => r.ExistsByNameAsync("Nuevo Equipo")).ReturnsAsync(false);

        var result = await _delegate.CreateAsync(team);

        Assert.Equal("Nuevo Equipo", result.Name);
        _repoMock.Verify(r => r.AddAsync(team), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LanzaExcepcionSiNombreYaExiste()
    {
        var team = new TeamBuilder().WithId("team-1").WithName("Repetido").Build();
        _repoMock.Setup(r => r.ExistsByNameAsync("Repetido")).ReturnsAsync(true);

        await Assert.ThrowsAsync<DomainValidationException>(() => _delegate.CreateAsync(team));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ActualizaCuandoExisteYNombreEsValido()
    {
        var existing = new TeamBuilder().WithId("team-1").WithName("Nombre Viejo").Build();
        var updated = new TeamBuilder().WithId("team-1").WithName("Nombre Nuevo").Build();
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.ExistsByNameAsync("Nombre Nuevo")).ReturnsAsync(false);

        var result = await _delegate.UpdateAsync("team-1", updated);

        Assert.Equal("Nombre Nuevo", result.Name);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Team>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_LanzaNotFoundSiNoExiste()
    {
        var team = new TeamBuilder().WithId("no-existe").WithName("Cualquiera").Build();
        _repoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.UpdateAsync("no-existe", team));
    }

    [Fact]
    public async Task UpdateAsync_LanzaDomainValidationSiNuevoNombreYaExisteEnOtroEquipo()
    {
        var existing = new TeamBuilder().WithId("team-1").WithName("Nombre Actual").Build();
        var updated = new TeamBuilder().WithId("team-1").WithName("Nombre De Otro Equipo").Build();
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.ExistsByNameAsync("Nombre De Otro Equipo")).ReturnsAsync(true);

        await Assert.ThrowsAsync<DomainValidationException>(() => _delegate.UpdateAsync("team-1", updated));
    }

    [Fact]
    public async Task DeleteAsync_EliminaCuandoExiste()
    {
        var team = new TeamBuilder().WithId("team-1").WithName("A Borrar").Build();
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        await _delegate.DeleteAsync("team-1");

        _repoMock.Verify(r => r.DeleteAsync("team-1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_LanzaNotFoundSiNoExiste()
    {
        _repoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.DeleteAsync("no-existe"));
    }
}
