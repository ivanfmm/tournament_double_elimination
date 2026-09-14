using TournamentServices.Domain;
using TournamentServices.Domain.Enums;

namespace TournamentServices.TestBuilders;

public class TournamentBuilder
{
    private Tournament _tournament = new()
    {
        Id = "tournament-1",
        Name = "Default Tournament",
        Format = new TournamentFormat(maxTeamsPerGroup: 4, numberOfGroups: 2, type: TournamentType.RoundRobin)
    };

    public TournamentBuilder WithId(string id) { _tournament.Id = id; return this; }
    public TournamentBuilder WithName(string name) { _tournament.Name = name; return this; }
    public TournamentBuilder WithFormat(TournamentFormat format) { _tournament.Format = format; return this; }
    public TournamentBuilder WithGroup(Group group) { _tournament.Groups.Add(group); return this; }
    public TournamentBuilder WithMatch(Match match) { _tournament.Matches.Add(match); return this; }

    public Tournament Build() => _tournament;
}