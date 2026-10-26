using Moq;
using TournamentServices.Delegates;
using TournamentServices.Domain;
using TournamentServices.Domain.Brackets;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders;

// Moq tambien define una clase Match; este alias fija la de Domain.
using Match = TournamentServices.Domain.Match;

namespace TournamentServices.Delegates.Tests;

public class BracketDelegateTests
{
    private readonly Mock<ITournamentRepository> _tournamentRepoMock = new();
    private readonly Mock<IGroupRepository> _groupRepoMock = new();
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly Mock<ITeamSeeder> _seederMock = new();
    private readonly IBracketDelegate _delegate;

    // "Base de datos" de partidos para los tests: AddAsync agrega aqui y
    // GetByTournamentAsync regresa una copia, como lo haria el repositorio.
    private readonly List<Match> _savedMatches = new();

    public BracketDelegateTests()
    {
        // Seeder que no baraja, para que los tests sean predecibles
        _seederMock
            .Setup(s => s.Seed(It.IsAny<IReadOnlyList<string>>()))
            .Returns((IReadOnlyList<string> teamIds) => teamIds);

        _matchRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Match>()))
            .ReturnsAsync((Match m) => { _savedMatches.Add(m); return m; });

        _matchRepoMock
            .Setup(r => r.GetByTournamentAsync(It.IsAny<string>()))
            .ReturnsAsync((string tournamentId) =>
                (IReadOnlyList<Match>)_savedMatches.Where(m => m.TournamentId == tournamentId).ToList());

        _delegate = new BracketDelegate(
            _tournamentRepoMock.Object,
            _groupRepoMock.Object,
            _matchRepoMock.Object,
            _teamRepoMock.Object,
            _seederMock.Object);
    }

    private Tournament GivenTournament(string id = "t-1", TournamentType type = TournamentType.DoubleElimination, params string[] teamIds)
    {
        var tournament = new TournamentBuilder()
            .WithId(id)
            .WithFormat(new TournamentFormat(maxTeamsPerGroup: 16, numberOfGroups: 2, type: type))
            .Build();
        _tournamentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tournament);

        var group = new GroupBuilder().WithId("g-1").WithTournamentId(id);
        foreach (var teamId in teamIds)
        {
            group.WithTeam(teamId);
        }
        _groupRepoMock
            .Setup(r => r.GetByTournamentAsync(id))
            .ReturnsAsync(new List<Group> { group.Build() });

        return tournament;
    }

    private Match PendingMatch(string bracketKey)
        => _savedMatches.Single(m => m.BracketKey == bracketKey && !m.IsCompleted);

    // ---------- StartAsync ----------

    [Fact]
    public async Task StartAsync_With4Teams_CreatesRound1MatchesAndStartsTournament()
    {
        var tournament = GivenTournament(teamIds: new[] { "a", "b", "c", "d" });

        var result = await _delegate.StartAsync("t-1");

        Assert.Equal(TournamentStatus.InProgress, result.Status);
        Assert.Equal(new[] { "a", "b", "c", "d" }, result.SeededTeamIds);
        Assert.Equal(2, _savedMatches.Count);
        Assert.All(_savedMatches, m => Assert.NotNull(m.BracketKey));
        Assert.All(_savedMatches, m => Assert.False(Guid.Parse(m.Id) == Guid.Empty));
        _tournamentRepoMock.Verify(r => r.UpdateAsync(tournament), Times.Once);
    }

    [Fact]
    public async Task StartAsync_NotDoubleElimination_ThrowsBusinessRuleException()
    {
        GivenTournament(type: TournamentType.RoundRobin, teamIds: new[] { "a", "b" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.StartAsync("t-1"));
        _matchRepoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithLessThanTwoTeams_ThrowsBusinessRuleException()
    {
        GivenTournament(teamIds: new[] { "a" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.StartAsync("t-1"));
        _tournamentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Tournament>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_Twice_ThrowsBusinessRuleException()
    {
        GivenTournament(teamIds: new[] { "a", "b" });
        await _delegate.StartAsync("t-1");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.StartAsync("t-1"));
        Assert.Single(_savedMatches);
    }

    [Fact]
    public async Task StartAsync_TournamentDoesNotExist_ThrowsNotFoundException()
    {
        _tournamentRepoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Tournament?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.StartAsync("no-existe"));
    }

    // ---------- ReportScoreAsync ----------

    [Fact]
    public async Task ReportScoreAsync_Tie_ThrowsBusinessRuleException()
    {
        GivenTournament(teamIds: new[] { "a", "b" });
        await _delegate.StartAsync("t-1");
        var match = PendingMatch("wb-r1-m0");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.ReportScoreAsync("t-1", match.Id, 2, 2));
        Assert.False(match.IsCompleted);
    }

    [Fact]
    public async Task ReportScoreAsync_MatchAlreadyCompleted_ThrowsBusinessRuleException()
    {
        GivenTournament(teamIds: new[] { "a", "b", "c", "d" });
        await _delegate.StartAsync("t-1");
        var match = PendingMatch("wb-r1-m0");
        await _delegate.ReportScoreAsync("t-1", match.Id, 1, 0);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.ReportScoreAsync("t-1", match.Id, 0, 1));
        Assert.Equal(Winner.Home, match.Winner);
    }

    [Fact]
    public async Task ReportScoreAsync_TournamentNotStarted_ThrowsBusinessRuleException()
    {
        GivenTournament(teamIds: new[] { "a", "b" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.ReportScoreAsync("t-1", "m-1", 1, 0));
    }

    [Fact]
    public async Task ReportScoreAsync_MatchNotFound_ThrowsNotFoundException()
    {
        GivenTournament(teamIds: new[] { "a", "b" });
        await _delegate.StartAsync("t-1");

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.ReportScoreAsync("t-1", "no-existe", 1, 0));
    }

    [Fact]
    public async Task ReportScoreAsync_SetsResultOrderAndCreatesNextMatch()
    {
        // 3 equipos: a tiene bye y b vs c juegan la ronda 1. Al ganar b, se arma a vs b.
        GivenTournament(teamIds: new[] { "a", "b", "c" });
        await _delegate.StartAsync("t-1");
        var first = Assert.Single(_savedMatches);

        await _delegate.ReportScoreAsync("t-1", first.Id, 3, 1);

        Assert.Equal(1, first.ResultOrder);
        var next = _savedMatches.Single(m => !m.IsCompleted);
        Assert.Contains("a", new[] { next.HomeTeamId, next.VisitorTeamId });
        Assert.Contains("b", new[] { next.HomeTeamId, next.VisitorTeamId });
        _matchRepoMock.Verify(r => r.UpdateAsync(first), Times.Once);
    }

    [Fact]
    public async Task ReportScoreAsync_GrandFinalWonByUnbeatenTeam_FinishesTournament()
    {
        var tournament = GivenTournament(teamIds: new[] { "a", "b" });
        await _delegate.StartAsync("t-1");

        await _delegate.ReportScoreAsync("t-1", PendingMatch("wb-r1-m0").Id, 1, 0);   // a gana
        await _delegate.ReportScoreAsync("t-1", PendingMatch("grand-final").Id, 1, 0); // a (invicto) gana

        Assert.Equal(TournamentStatus.Finished, tournament.Status);
        Assert.Equal("a", tournament.ChampionTeamId);
    }

    [Fact]
    public async Task ReportScoreAsync_LosersChampionWinsGrandFinal_CreatesReset()
    {
        var tournament = GivenTournament(teamIds: new[] { "a", "b" });
        await _delegate.StartAsync("t-1");

        await _delegate.ReportScoreAsync("t-1", PendingMatch("wb-r1-m0").Id, 1, 0);   // a gana, b baja
        await _delegate.ReportScoreAsync("t-1", PendingMatch("grand-final").Id, 0, 1); // b gana: reset

        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        var reset = PendingMatch("grand-final-reset");

        await _delegate.ReportScoreAsync("t-1", reset.Id, 2, 0); // b (local en el reset) gana otra vez

        Assert.Equal(TournamentStatus.Finished, tournament.Status);
        Assert.Equal("b", tournament.ChampionTeamId);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    public async Task FullTournament_EndsWithChampionAndEveryoneElseEliminated(int teamCount)
    {
        var teams = Enumerable.Range(1, teamCount).Select(i => $"team-{i}").ToArray();
        var tournament = GivenTournament(teamIds: teams);
        await _delegate.StartAsync("t-1");

        // Siempre gana el visitante, para cubrir una ruta distinta a la de Domain
        while (tournament.Status == TournamentStatus.InProgress)
        {
            var next = _savedMatches.First(m => !m.IsCompleted);
            await _delegate.ReportScoreAsync("t-1", next.Id, 0, 1);
        }

        var losses = teams.ToDictionary(t => t, _ => 0);
        foreach (var m in _savedMatches)
        {
            losses[m.Winner == Winner.Home ? m.VisitorTeamId : m.HomeTeamId]++;
        }

        Assert.NotNull(tournament.ChampionTeamId);
        Assert.True(losses[tournament.ChampionTeamId!] <= 1);
        Assert.All(losses.Where(l => l.Key != tournament.ChampionTeamId), l => Assert.Equal(2, l.Value));
        Assert.Equal(
            Enumerable.Range(1, _savedMatches.Count).Cast<int?>(),
            _savedMatches.Select(m => m.ResultOrder).OrderBy(o => o));
    }

    // ---------- GetChampionAsync ----------

    [Fact]
    public async Task GetChampionAsync_NotFinished_ThrowsBusinessRuleException()
    {
        GivenTournament(teamIds: new[] { "a", "b" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _delegate.GetChampionAsync("t-1"));
    }

    [Fact]
    public async Task GetChampionAsync_Finished_ReturnsChampionTeam()
    {
        GivenTournament(teamIds: new[] { "a", "b" });
        _teamRepoMock.Setup(r => r.GetByIdAsync("a")).ReturnsAsync(new TeamBuilder().WithId("a").WithName("Pumas").Build());
        await _delegate.StartAsync("t-1");
        await _delegate.ReportScoreAsync("t-1", PendingMatch("wb-r1-m0").Id, 1, 0);
        await _delegate.ReportScoreAsync("t-1", PendingMatch("grand-final").Id, 1, 0);

        var champion = await _delegate.GetChampionAsync("t-1");

        Assert.Equal("Pumas", champion.Name);
    }
}
