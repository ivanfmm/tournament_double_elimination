using TournamentServices.Domain;

namespace TournamentServices.TestBuilders;

public class TeamBuilder
{
    private Team _team = new()
    {
        Id = "team-1",
        Name = "Default Team"
    };

    public TeamBuilder WithId(string id) { _team.Id = id; return this; }
    public TeamBuilder WithName(string name) { _team.Name = name; return this; }

    public Team Build() => _team;
}