using TournamentServices.Domain;

namespace TournamentServices.TestBuilders;

public class MatchBuilder
{
    private Match _match = new()
    {
        Id = "match-1",
        TournamentId = "tournament-1",
        HomeTeamId = "team-home",
        VisitorTeamId = "team-visitor"
    };

    private Score? _pendingScore;

    public MatchBuilder WithId(string id) { _match.Id = id; return this; }
    public MatchBuilder WithTournamentId(string tournamentId) { _match.TournamentId = tournamentId; return this; }
    public MatchBuilder WithGroupId(string? groupId) { _match.GroupId = groupId; return this; }
    public MatchBuilder WithHomeTeam(string teamId) { _match.HomeTeamId = teamId; return this; }
    public MatchBuilder WithVisitorTeam(string teamId) { _match.VisitorTeamId = teamId; return this; }

    public MatchBuilder WithScore(int homeTeamScore, int visitorTeamScore)
    {
        _pendingScore = new Score(homeTeamScore, visitorTeamScore);
        return this;
    }

    public Match Build()
    {
        if (_pendingScore is not null)
        {
            _match.SetScore(_pendingScore);
        }

        return _match;
    }
}