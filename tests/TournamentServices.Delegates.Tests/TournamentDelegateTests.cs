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
    private readonly TournamentDelegate _sut;

    public TournamentDelegateTests()
    {
        _sut = new TournamentDelegate(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTournament_ReturnsTournament()
    {
        var tournament = new TournamentBuilder().WithId("t-1").Build();
        _repositoryMock.Setup(r => r.GetByIdAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.GetByIdAsync("t-1");

        Assert.Equal(tournament, result);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

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

        _repositoryMock.Setup(r => r.GetByIdAsync("t-1")).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(updated);

        Assert.Equal("Nombre Nuevo", result.Name);
        _repositoryMock.Verify(r => r.UpdateAsync(updated), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        var updated = new TournamentBuilder().WithId("no-existe").Build();
        _repositoryMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(updated));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tournament>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_WithNameOnly_UpdatesOnlyName()
    {
        var original = new TournamentFormat(4, 2, TournamentType.RoundRobin);
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").WithFormat(original).Build();
        _repositoryMock.Setup(r => r.GetByIdAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.PatchAsync("t-1", new TournamentPatch(Name: "Nombre Nuevo"));

        Assert.Equal("Nombre Nuevo", result.Name);
        Assert.Equal(original, result.Format); // no se tocó
        _repositoryMock.Verify(r => r.UpdateAsync(tournament), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WithFormatOnly_UpdatesOnlyFormat()
    {
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").Build();
        _repositoryMock.Setup(r => r.GetByIdAsync("t-1")).ReturnsAsync(tournament);

        var newFormat = new TournamentFormat(8, 4, TournamentType.Nfl);
        var result = await _sut.PatchAsync("t-1", new TournamentPatch(Format: newFormat));

        Assert.Equal("Nombre Original", result.Name); // no se tocó
        Assert.Equal(newFormat, result.Format);
    }

    [Fact]
    public async Task PatchAsync_WithBothFields_UpdatesBoth()
    {
        var tournament = new TournamentBuilder().WithId("t-1").Build();
        _repositoryMock.Setup(r => r.GetByIdAsync("t-1")).ReturnsAsync(tournament);

        var newFormat = new TournamentFormat(8, 4, TournamentType.Nfl);
        var result = await _sut.PatchAsync("t-1", new TournamentPatch(Name: "Nombre Nuevo", Format: newFormat));

        Assert.Equal("Nombre Nuevo", result.Name);
        Assert.Equal(newFormat, result.Format);
    }

    [Fact]
    public async Task PatchAsync_WithNoFields_DoesNotChangeAnything()
    {
        var tournament = new TournamentBuilder().WithId("t-1").WithName("Nombre Original").Build();
        _repositoryMock.Setup(r => r.GetByIdAsync("t-1")).ReturnsAsync(tournament);

        var result = await _sut.PatchAsync("t-1", new TournamentPatch());

        Assert.Equal("Nombre Original", result.Name);
        // Aun así se llama UpdateAsync: un patch vacío es un caso raro
        // pero válido, no hay razón de negocio para rechazarlo aquí.
        _repositoryMock.Verify(r => r.UpdateAsync(tournament), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_NonExistingTournament_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.PatchAsync("no-existe", new TournamentPatch(Name: "Algo")));
    }

    [Fact]
    public async Task DeleteAsync_ExistingTournament_CallsRepositoryDeleteAsync()
    {
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
}