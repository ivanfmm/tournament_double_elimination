using TournamentServices.Api.Filters;
using TournamentServices.Delegates;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;

namespace TournamentServices.Api.Routes;

public static class MatchRoutes
{
    public static IEndpointRouteBuilder MapMatchRoutes(this IEndpointRouteBuilder app)
    {
        var matches = app.MapGroup("/tournaments/{tournamentId}/matches")
            .AddEndpointFilter<ValidateIdFilter>();

        matches.MapGet("/", async (string tournamentId, IMatchDelegate d) =>
            Results.Ok(await d.GetByTournamentAsync(tournamentId)));

        matches.MapGet("/{matchId}", async (string tournamentId, string matchId, IMatchDelegate d) =>
            Results.Ok(await FindAsync(d, tournamentId, matchId)));

        // Del body se usan HomeTeamId, VisitorTeamId y GroupId.
        matches.MapPost("/", async (string tournamentId, Match match, IMatchDelegate d) =>
        {
            var created = await d.CreateAsync(tournamentId, match.HomeTeamId, match.VisitorTeamId, match.GroupId);
            return Results.Created($"/tournaments/{tournamentId}/matches/{created.Id}", created);
        });

        matches.MapPatch("/{matchId}/score", async (
            string tournamentId, string matchId, Score score, IMatchDelegate d) =>
        {
            await FindAsync(d, tournamentId, matchId);
            return Results.Ok(await d.UpdateScoreAsync(matchId, score.HomeTeamScore, score.VisitorTeamScore));
        });

        matches.MapDelete("/{matchId}", async (string tournamentId, string matchId, IMatchDelegate d) =>
        {
            await FindAsync(d, tournamentId, matchId);
            await d.DeleteAsync(matchId);
            return Results.NoContent();
        });

        return app;
    }
    private static async Task<Match> FindAsync(IMatchDelegate d, string tournamentId, string matchId)
    {
        var match = await d.GetByIdAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            throw new NotFoundException($"Partido '{matchId}' no encontrado en el torneo '{tournamentId}'.");
        return match;
    }
}