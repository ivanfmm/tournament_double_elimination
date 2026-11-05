using Moq;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders;

namespace TournamentServices.Delegates.Tests;

public class GroupDelegateTests
{
    private readonly Mock<IGroupRepository> _groupRepoMock;
    private readonly Mock<ITournamentRepository> _tournamentRepoMock;
    private readonly Mock<ITeamRepository> _teamRepoMock;
    private readonly IGroupDelegate _delegate;

    public GroupDelegateTests()
    {
        _groupRepoMock = new Mock<IGroupRepository>();
        _tournamentRepoMock = new Mock<ITournamentRepository>();
        _teamRepoMock = new Mock<ITeamRepository>();

        // Moq regresa null para Task<IReadOnlyList<T>>; un repo real sin datos regresa lista vacia
        _groupRepoMock.Setup(r => r.GetByTournamentAsync(It.IsAny<string>())).ReturnsAsync(new List<Group>());

        _delegate = new GroupDelegate(
            _groupRepoMock.Object,
            _tournamentRepoMock.Object,
            _teamRepoMock.Object);
    }

    // Helpers para no repetir setups en cada test
    private void SetupTournament(Tournament tournament)
    {
        _tournamentRepoMock.Setup(r => r.GetByIdAsync(tournament.Id)).ReturnsAsync(tournament);
    }

    private void SetupGroup(Group group)
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
    }

    // El delegate consulta los grupos actuales al repo, no a Tournament.Groups
    private void SetupTournamentGroups(string tournamentId, params Group[] groups)
    {
        _groupRepoMock.Setup(r => r.GetByTournamentAsync(tournamentId)).ReturnsAsync(groups.ToList());
    }

    private static Tournament AStartedTournament(string id)
    {
        var format = new TournamentFormat(maxTeamsPerGroup: 4, numberOfGroups: 2, type: TournamentType.DoubleElimination);
        var tournament = new TournamentBuilder().WithId(id).WithFormat(format).Build();
        tournament.Start(new[] { "team-1", "team-2" });
        return tournament;
    }

    private void SetupTeam(string teamId)
    {
        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId))
            .ReturnsAsync(new TeamBuilder().WithId(teamId).WithName($"Equipo {teamId}").Build());
    }

    // ---------- GetByTournamentAsync ----------

    [Fact]
    public async Task GetByTournamentAsync_RegresaGruposDelTorneo()
    {
        SetupTournament(new TournamentBuilder().WithId("t-1").Build());
        var groups = new List<Group>
        {
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("A").Build(),
            new GroupBuilder().WithId("g-2").WithTournamentId("t-1").WithName("B").Build()
        };
        _groupRepoMock.Setup(r => r.GetByTournamentAsync("t-1")).ReturnsAsync(groups);

        var result = await _delegate.GetByTournamentAsync("t-1");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByTournamentAsync_RegresaVacioSiNoHayGrupos()
    {
        SetupTournament(new TournamentBuilder().WithId("t-1").Build());
        _groupRepoMock.Setup(r => r.GetByTournamentAsync("t-1")).ReturnsAsync(new List<Group>());

        var result = await _delegate.GetByTournamentAsync("t-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTournamentAsync_LanzaNotFoundSiNoExisteElTorneo()
    {
        _tournamentRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.GetByTournamentAsync("no-existe"));
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_RegresaGrupoConEquipos()
    {
        SetupGroup(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithTeam("team-1").Build());

        var result = await _delegate.GetByIdAsync("t-1", "g-1");

        Assert.NotNull(result);
        Assert.Contains("team-1", result.TeamIds);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaNullSiNoExiste()
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Group?)null);

        var result = await _delegate.GetByIdAsync("t-1", "no-existe");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_RegresaNullSiElGrupoEsDeOtroTorneo()
    {
        SetupGroup(new GroupBuilder().WithId("g-1").WithTournamentId("t-2").Build());

        var result = await _delegate.GetByIdAsync("t-1", "g-1");

        Assert.Null(result);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_GeneraIdYGuarda()
    {
        SetupTournament(new TournamentBuilder().WithId("t-1").Build());
        _groupRepoMock.Setup(r => r.AddAsync(It.IsAny<Group>())).ReturnsAsync((Group g) => g);

        var result = await _delegate.CreateAsync("t-1", "Grupo A");

        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        Assert.Equal("t-1", result.TournamentId);
        Assert.Equal("Grupo A", result.Name);
        _groupRepoMock.Verify(r => r.AddAsync(result), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LanzaNotFoundSiNoExisteElTorneo()
    {
        _tournamentRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.CreateAsync("no-existe", "Grupo A"));
        _groupRepoMock.Verify(r => r.AddAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_LanzaBusinessRuleSiElNombreSeRepite()
    {
        SetupTournament(new TournamentBuilder().WithId("t-1").Build());
        _groupRepoMock.Setup(r => r.ExistsByNameInTournamentAsync("t-1", "grupo a")).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.CreateAsync("t-1", "grupo a"));
        _groupRepoMock.Verify(r => r.AddAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_LanzaBusinessRuleSiYaNoCabenGrupos()
    {
        var format = new TournamentFormat(maxTeamsPerGroup: 4, numberOfGroups: 1, type: TournamentType.RoundRobin);
        var existing = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Grupo A").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithFormat(format).Build());
        SetupTournamentGroups("t-1", existing);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.CreateAsync("t-1", "Grupo B"));
        _groupRepoMock.Verify(r => r.AddAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_LanzaBusinessRuleSiElTorneoYaInicio()
    {
        SetupTournament(AStartedTournament("t-1"));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.CreateAsync("t-1", "Grupo C"));
        _groupRepoMock.Verify(r => r.AddAsync(It.IsAny<Group>()), Times.Never);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ActualizaNombre()
    {
        SetupGroup(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Viejo").Build());
        _groupRepoMock.Setup(r => r.ExistsByNameInTournamentAsync("t-1", "Nuevo")).ReturnsAsync(false);

        var result = await _delegate.UpdateAsync("t-1", "g-1", "Nuevo");

        Assert.Equal("Nuevo", result.Name);
        _groupRepoMock.Verify(r => r.UpdateAsync(result), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PermiteCambiarSoloMayusculasDelMismoNombre()
    {
        SetupGroup(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("grupo a").Build());
        _groupRepoMock.Setup(r => r.ExistsByNameInTournamentAsync("t-1", "Grupo A")).ReturnsAsync(true);

        var result = await _delegate.UpdateAsync("t-1", "g-1", "Grupo A");

        Assert.Equal("Grupo A", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_LanzaBusinessRuleSiOtroGrupoYaUsaElNombre()
    {
        SetupGroup(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Grupo A").Build());
        _groupRepoMock.Setup(r => r.ExistsByNameInTournamentAsync("t-1", "Grupo B")).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.UpdateAsync("t-1", "g-1", "Grupo B"));
        _groupRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_LanzaNotFoundSiNoExiste()
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Group?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.UpdateAsync("t-1", "no-existe", "X"));
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_EliminaGrupo()
    {
        SetupGroup(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithTeam("team-1").Build());
        _groupRepoMock.Setup(r => r.DeleteAsync("g-1")).ReturnsAsync(true);

        await _delegate.DeleteAsync("t-1", "g-1");

        _groupRepoMock.Verify(r => r.DeleteAsync("g-1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_LanzaNotFoundSiNoExiste()
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Group?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.DeleteAsync("t-1", "no-existe"));
        _groupRepoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    // ---------- AssignTeamsAsync ----------

    [Fact]
    public async Task AssignTeamsAsync_AgregaEquiposValidos()
    {
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithGroup(group).Build());
        SetupGroup(group);
        SetupTeam("team-1");
        SetupTeam("team-2");

        await _delegate.AssignTeamsAsync("t-1", "g-1", new[] { "team-1", "team-2" });

        Assert.Equal(new[] { "team-1", "team-2" }, group.TeamIds);
        _groupRepoMock.Verify(r => r.UpdateAsync(group), Times.Once);
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaNotFoundSiNoExisteElTorneo()
    {
        _tournamentRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _delegate.AssignTeamsAsync("no-existe", "g-1", new[] { "team-1" }));
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaNotFoundSiNoExisteElGrupo()
    {
        SetupTournament(new TournamentBuilder().WithId("t-1").Build());
        _groupRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Group?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _delegate.AssignTeamsAsync("t-1", "no-existe", new[] { "team-1" }));
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaBusinessRuleSiUnEquipoNoExiste()
    {
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithGroup(group).Build());
        SetupGroup(group);
        _teamRepoMock.Setup(r => r.GetByIdAsync("fantasma")).ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _delegate.AssignTeamsAsync("t-1", "g-1", new[] { "fantasma" }));
        _groupRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaBusinessRuleSiHayIdsRepetidos()
    {
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithGroup(group).Build());
        SetupGroup(group);
        SetupTeam("team-1");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _delegate.AssignTeamsAsync("t-1", "g-1", new[] { "team-1", "team-1" }));
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaBusinessRuleSiElEquipoYaEstaEnOtroGrupoDelTorneo()
    {
        var target = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("A").Build();
        var other = new GroupBuilder().WithId("g-2").WithTournamentId("t-1").WithName("B").WithTeam("team-1").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithGroup(target).WithGroup(other).Build());
        SetupGroup(target);
        SetupTournamentGroups("t-1", target, other);
        SetupTeam("team-1");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _delegate.AssignTeamsAsync("t-1", "g-1", new[] { "team-1" }));
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaBusinessRuleSiSeExcedeMaxTeamsPerGroup()
    {
        var format = new TournamentFormat(maxTeamsPerGroup: 2, numberOfGroups: 2, type: TournamentType.RoundRobin);
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithTeam("team-1").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithFormat(format).WithGroup(group).Build());
        SetupGroup(group);
        SetupTeam("team-2");
        SetupTeam("team-3");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _delegate.AssignTeamsAsync("t-1", "g-1", new[] { "team-2", "team-3" }));
        Assert.Single(group.TeamIds);
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaDomainValidationSiLaListaVieneVacia()
    {
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build();
        SetupTournament(new TournamentBuilder().WithId("t-1").WithGroup(group).Build());
        SetupGroup(group);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.AssignTeamsAsync("t-1", "g-1", Array.Empty<string>()));
    }

    [Fact]
    public async Task AssignTeamsAsync_LanzaBusinessRuleSiElTorneoYaInicio()
    {
        var group = new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build();
        SetupTournament(AStartedTournament("t-1"));
        SetupGroup(group);
        SetupTeam("team-3");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _delegate.AssignTeamsAsync("t-1", "g-1", new[] { "team-3" }));
        _groupRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Group>()), Times.Never);
    }
}
