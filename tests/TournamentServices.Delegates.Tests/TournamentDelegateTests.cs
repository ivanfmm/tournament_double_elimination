using Moq;
using TournamentServices.Delegates;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders;
using Xunit;

namespace TournamentServices.Delegates.Tests;

public class TournamentDelegateTests
{
    private readonly Mock<ITournamentRepository> _repositoryMock = new();
    private readonly Mock<IGroupRepository> _groupRepoMock = new();
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly TournamentDelegate _sut;

    public TournamentDelegateTests()
    {
        // Moq regresa null para Task<IReadOnlyList<T>>; un repo real sin datos regresa lista vacia
        _groupRepoMock.Setup(r => r.GetByTournamentAsync(It.IsAny<string>())).ReturnsAsync(new List<Group>());
        _matchRepoMock.Setup(r => r.GetByTournamentAsync(It.IsAny<string>())).ReturnsAsync(new List<TournamentServices.Domain.Match>());

        _sut = new TournamentDelegate(_repositoryMock.Object, _groupRepoMock.Object, _matchRepoMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTournament_ReturnsTournament()
    {
        var tournament = new TournamentBuilder().WithId("t-1").Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.GetByIdAsync("t-1");

        Assert.Equal(tournament, result);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync("no-existe"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsWhatRepositoryReturns()
    {
        var tournaments = new List<Tournament>
        {
            new TournamentBuilder().WithId("t-1").Build(),
            new TournamentBuilder().WithId("t-2").Build()
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tournaments);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryAddAsync()
    {
        var tournament = new TournamentBuilder().WithId("t-1").Build();
        _repositoryMock.Setup(r => r.AddAsync(tournament)).ReturnsAsync(tournament);

        var result = await _sut.CreateAsync(tournament);

        Assert.Equal(tournament, result);
        _repositoryMock.Verify(r => r.AddAsync(tournament), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingTournament_CallsRepositoryUpdateAsync()
    {
        var existing = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").Build();
        var updated = new TournamentBuilder().WithId("t-1").WithName("Nombre Nuevo").Build();

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(updated);

        Assert.Equal("Nombre Nuevo", result.Name);
        // Se guarda la instancia cargada (existing) con los cambios aplicados
        _repositoryMock.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        var updated = new TournamentBuilder().WithId("no-existe").Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(updated));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tournament>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_WithNameOnly_UpdatesOnlyName()
    {
        var original = new TournamentFormat(4, 2, TournamentType.RoundRobin);
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").WithFormat(original).Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.PatchAsync("t-1", new TournamentPatch(Name: "Nombre Nuevo"));

        Assert.Equal("Nombre Nuevo", result.Name);
        Assert.Equal(original, result.Format); // no se tocó
        _repositoryMock.Verify(r => r.UpdateAsync(tournament), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WithFormatOnly_UpdatesOnlyFormat()
    {
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        var newFormat = new TournamentFormat(8, 4, TournamentType.Nfl);
        var result = await _sut.PatchAsync("t-1", new TournamentPatch(MaxTeamsPerGroup: 8, NumberOfGroups: 4, Type: TournamentType.Nfl));

        Assert.Equal("Nombre Original", result.Name); // no se tocó
        Assert.Equal(newFormat, result.Format);
    }

    [Fact]
    public async Task PatchAsync_WithBothFields_UpdatesBoth()
    {
        var tournament = new TournamentBuilder().WithId("t-1").Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        var newFormat = new TournamentFormat(8, 4, TournamentType.Nfl);
        var result = await _sut.PatchAsync("t-1", new TournamentPatch(Name: "Nombre Nuevo", MaxTeamsPerGroup: 8, NumberOfGroups: 4, Type: TournamentType.Nfl));

        Assert.Equal("Nombre Nuevo", result.Name);
        Assert.Equal(newFormat, result.Format);
    }

    [Fact]
    public async Task PatchAsync_WithNoFields_DoesNotChangeAnything()
    {
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.PatchAsync("t-1", new TournamentPatch());

        Assert.Equal("Nombre Original", result.Name);
        // Aun así se llama UpdateAsync: un patch vacío es un caso raro
        // pero válido, no hay razón de negocio para rechazarlo aquí.
        _repositoryMock.Verify(r => r.UpdateAsync(tournament), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.PatchAsync("no-existe", new TournamentPatch(Name: "Algo")));
    }

    [Fact]
    public async Task DeleteAsync_ExistingTournament_CallsRepositoryDeleteAsync()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(new TournamentBuilder().WithId("t-1").Build());
        _repositoryMock.Setup(r => r.DeleteAsync("t-1")).ReturnsAsync(true);

        await _sut.DeleteAsync("t-1");

        _repositoryMock.Verify(r => r.DeleteAsync("t-1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.DeleteAsync("no-existe")).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync("no-existe"));
    }

    [Fact]
    public async Task PatchAsync_FormatChangeOnStartedTournament_ThrowsBusinessRuleException()
    {
        var tournament = AStartedTournament("t-1");
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.PatchAsync("t-1", new TournamentPatch(NumberOfGroups: 4)));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tournament>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_NameChangeOnStartedTournament_IsAllowed()
    {
        var tournament = AStartedTournament("t-1");
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.PatchAsync("t-1", new TournamentPatch(Name: "Nuevo Nombre"));

        Assert.Equal("Nuevo Nombre", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_FormatChangeOnStartedTournament_ThrowsBusinessRuleException()
    {
        var existing = AStartedTournament("t-1");
        var updated = new TournamentBuilder().WithId("t-1")
            .WithFormat(new TournamentFormat(8, 2, TournamentType.DoubleElimination)).Build();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync("t-1")).ReturnsAsync(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(updated));
    }

    private static Tournament AStartedTournament(string id)
    {
        var tournament = new TournamentBuilder().WithId(id)
            .WithFormat(new TournamentFormat(4, 2, TournamentType.DoubleElimination)).Build();
        tournament.Start(new[] { "a", "b" });
        return tournament;
    }
}
