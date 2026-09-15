using Moq;
using TournamentServices.Delegates;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;
using TournamentServices.TestBuilders;

// Moq tambien define una clase Match (la de los matchers tipo It.Is),
// asi que el nombre queda ambiguo. Este alias fija cual es cual.
using Match = TournamentServices.Domain.Match;

namespace TournamentServices.Delegates.Tests;

public class MatchDelegateTests
{
    private readonly Mock<IMatchRepository> _repoMock;
    private readonly Mock<ITeamRepository> _teamRepoMock;
    private readonly Mock<IGroupRepository> _groupRepoMock;
    private readonly IMatchDelegate _delegate;

    public MatchDelegateTests()
    {
        _repoMock = new Mock<IMatchRepository>();
        _teamRepoMock = new Mock<ITeamRepository>();
        _groupRepoMock = new Mock<IGroupRepository>();

        _delegate = new MatchDelegate(
            _repoMock.Object,
            _teamRepoMock.Object,
            _groupRepoMock.Object);
    }

    // Deja listo el caso feliz: un torneo con un grupo que contiene a los
    // equipos indicados, y esos equipos existiendo en el repo de equipos.
    private void GivenTournamentWithGroup(string tournamentId, string groupId, params string[] teamIds)
    {
        var builder = new GroupBuilder().WithId(groupId).WithTournamentId(tournamentId);
        foreach (var teamId in teamIds)
        {
            builder.WithTeam(teamId);
        }

        _groupRepoMock
            .Setup(r => r.GetByTournamentAsync(tournamentId))
            .ReturnsAsync(new List<Group> { builder.Build() });

        GivenTeamsExist(teamIds);
    }

    private void GivenTeamsExist(params string[] teamIds)
    {
        foreach (var teamId in teamIds)
        {
            _teamRepoMock
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(new TeamBuilder().WithId(teamId).WithName($"Equipo {teamId}").Build());
        }
    }

    private static Match AMatch(string id = "m1", string tournamentId = "tour-1")
    {
        return new MatchBuilder()
            .WithId(id)
            .WithTournamentId(tournamentId)
            .WithHomeTeam("t1")
            .WithVisitorTeam("t2")
            .Build();
    }

    // ---------- GetByTournamentAsync ----------

    [Fact]
    public async Task GetByTournamentAsync_ReturnsWhatTheRepositoryReturns()
    {
        var matches = new List<Match> { AMatch("m1"), AMatch("m2") };
        _repoMock.Setup(r => r.GetByTournamentAsync("tour-1")).ReturnsAsync(matches);

        var result = await _delegate.GetByTournamentAsync("tour-1");

        Assert.Equal(2, result.Count);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_WhenMatchExists_ReturnsMatchWithWinnerAndIsCompleted()
    {
        var match = new MatchBuilder()
            .WithId("m1").WithHomeTeam("t1").WithVisitorTeam("t2")
            .WithScore(2, 0)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync("m1")).ReturnsAsync(match);

        var result = await _delegate.GetByIdAsync("m1");

        Assert.NotNull(result);
        Assert.Equal(Winner.Home, result!.Winner);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMatchDoesNotExist_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Match?)null);

        Assert.Null(await _delegate.GetByIdAsync("no-existe"));
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_WithValidTeams_GeneratesIdAndSaves()
    {
        GivenTournamentWithGroup("tour-1", "g1", "t1", "t2");

        var match = await _delegate.CreateAsync("tour-1", "t1", "t2", "g1");

        Assert.False(string.IsNullOrWhiteSpace(match.Id));
        Assert.Equal("tour-1", match.TournamentId);
        Assert.Equal("g1", match.GroupId);
        Assert.False(match.IsCompleted);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutGroupId_SkipsGroupValidation()
    {
        GivenTournamentWithGroup("tour-1", "g1", "t1", "t2");

        var match = await _delegate.CreateAsync("tour-1", "t1", "t2");

        Assert.Null(match.GroupId);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenTeamDoesNotExist_ThrowsDomainValidationException()
    {
        GivenTournamentWithGroup("tour-1", "g1", "t1", "t2");
        _teamRepoMock.Setup(r => r.GetByIdAsync("fantasma")).ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.CreateAsync("tour-1", "fantasma", "t2", "g1"));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTeamIsNotInTournament_ThrowsDomainValidationException()
    {
        GivenTournamentWithGroup("tour-1", "g1", "t1", "t2");
        GivenTeamsExist("t9"); // existe como equipo, pero no esta en ningun grupo del torneo

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.CreateAsync("tour-1", "t1", "t9", "g1"));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithSameTeamOnBothSides_ThrowsDomainValidationException()
    {
        GivenTournamentWithGroup("tour-1", "g1", "t1", "t2");

        // La validacion la hace el constructor de Match en Domain.
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.CreateAsync("tour-1", "t1", "t1", "g1"));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenGroupIsNotInTournament_ThrowsDomainValidationException()
    {
        GivenTournamentWithGroup("tour-1", "g1", "t1", "t2");

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.CreateAsync("tour-1", "t1", "t2", "grupo-de-otro-torneo"));
    }

    [Fact]
    public async Task CreateAsync_WhenTeamsAreInDifferentGroups_ThrowsDomainValidationException()
    {
        var groupA = new GroupBuilder().WithId("g1").WithTournamentId("tour-1").WithTeam("t1").Build();
        var groupB = new GroupBuilder().WithId("g2").WithTournamentId("tour-1").WithTeam("t2").Build();

        _groupRepoMock
            .Setup(r => r.GetByTournamentAsync("tour-1"))
            .ReturnsAsync(new List<Group> { groupA, groupB });

        GivenTeamsExist("t1", "t2");

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.CreateAsync("tour-1", "t1", "t2", "g1"));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Match>()), Times.Never);
    }

    // ---------- UpdateScoreAsync ----------

    [Fact]
    public async Task UpdateScoreAsync_WhenHomeScoresMore_SetsWinnerHomeAndCompletes()
    {
        var match = AMatch();
        _repoMock.Setup(r => r.GetByIdAsync("m1")).ReturnsAsync(match);

        var result = await _delegate.UpdateScoreAsync("m1", 3, 1);

        Assert.Equal(Winner.Home, result.Winner);
        Assert.True(result.IsCompleted);
        Assert.Equal(3, result.Score.HomeTeamScore);
        _repoMock.Verify(r => r.UpdateAsync(match), Times.Once);
    }

    [Fact]
    public async Task UpdateScoreAsync_WhenVisitorScoresMore_SetsWinnerVisitor()
    {
        var match = AMatch();
        _repoMock.Setup(r => r.GetByIdAsync("m1")).ReturnsAsync(match);

        var result = await _delegate.UpdateScoreAsync("m1", 0, 2);

        Assert.Equal(Winner.Visitor, result.Winner);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task UpdateScoreAsync_WhenScoresAreTied_LeavesWinnerNullButCompletes()
    {
        var match = AMatch();
        _repoMock.Setup(r => r.GetByIdAsync("m1")).ReturnsAsync(match);

        var result = await _delegate.UpdateScoreAsync("m1", 1, 1);

        Assert.Null(result.Winner);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task UpdateScoreAsync_WithNegativeScore_ThrowsAndDoesNotSave()
    {
        var match = AMatch();
        _repoMock.Setup(r => r.GetByIdAsync("m1")).ReturnsAsync(match);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _delegate.UpdateScoreAsync("m1", -1, 0));

        Assert.False(match.IsCompleted);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task UpdateScoreAsync_WhenMatchDoesNotExist_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Match?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _delegate.UpdateScoreAsync("no-existe", 1, 0));
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_WhenMatchExists_CallsRepository()
    {
        _repoMock.Setup(r => r.GetByIdAsync("m1")).ReturnsAsync(AMatch());

        await _delegate.DeleteAsync("m1");

        _repoMock.Verify(r => r.DeleteAsync("m1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMatchDoesNotExist_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("no-existe")).ReturnsAsync((Match?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _delegate.DeleteAsync("no-existe"));

        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
