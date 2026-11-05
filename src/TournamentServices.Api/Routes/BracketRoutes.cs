using TournamentServices.Api.Filters;
using TournamentServices.Delegates;
using TournamentServices.Domain;

namespace TournamentServices.Api.Routes;

public static class BracketRoutes
{
    public static IEndpointRouteBuilder MapBracketRoutes(this IEndpointRouteBuilder app)
    {
        var tournament = app.MapGroup("/tournaments/{tournamentId}")
            .AddEndpointFilter<ValidateIdFilter>();

        // Sortea los equipos inscritos y crea la ronda 1 del winners bracket
        tournament.MapPost("/start", async (string tournamentId, IBracketDelegate d) =>
            Results.Ok(await d.StartAsync(tournamentId)));

        // Reporta el resultado y genera los siguientes partidos del bracket
        tournament.MapPatch("/bracket/matches/{matchId}/score", async (
            string tournamentId, string matchId, Score score, IBracketDelegate d) =>
            Results.Ok(await d.ReportScoreAsync(tournamentId, matchId, score.HomeTeamScore, score.VisitorTeamScore)));

        tournament.MapGet("/champion", async (string tournamentId, IBracketDelegate d) =>
            Results.Ok(await d.GetChampionAsync(tournamentId)));

        return app;
    }
}
