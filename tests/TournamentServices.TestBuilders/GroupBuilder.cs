using TournamentServices.Domain;
namespace TournamentServices.TestBuilders;

public class GroupBuilder
{
    private Group _group = new()
    {
        Id = "group-1",
        TournamentId = "tournament-1",
        Name = "Default Group"
    };

    public GroupBuilder WithId(string id) { _group.Id = id; return this; }
    public GroupBuilder WithTournamentId(string tournamentId) { _group.TournamentId = tournamentId; return this; }
    public GroupBuilder WithName(string name) { _group.Name = name; return this; }

    public GroupBuilder WithTeam(string teamId)
    {
        _group.AddTeam(teamId);
        return this;
    }

    public Group Build() => _group;
}