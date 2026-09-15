using TournamentServices.Api.Dtos;
using TournamentServices.Domain;

namespace TournamentServices.Api.Extensions;

public static class MatchMappings
{
    // Igual que en grupos: se recibe la lista de equipos ya cargada
    // para llenar HomeTeam y VisitorTeam sin ir a la base por cada partido.
    public static MatchDto ToDto(this Match match, IReadOnlyList<Team> teams)
    {
        var home = teams.FirstOrDefault(t => t.Id == match.HomeTeamId);
        var visitor = teams.FirstOrDefault(t => t.Id == match.VisitorTeamId);

        return new MatchDto(
            match.Id,
            match.TournamentId,
            match.GroupId,
            match.HomeTeamId,
            match.VisitorTeamId,
            home?.ToDto(),
            visitor?.ToDto(),
            new ScoreDto(match.Score.HomeTeamScore, match.Score.VisitorTeamScore),
            match.Winner,
            match.IsCompleted);
    }
}
