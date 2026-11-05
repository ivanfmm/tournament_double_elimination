using TournamentServices.Api.Filters;
using TournamentServices.Delegates;
using TournamentServices.Domain;

namespace TournamentServices.Api.Routes;

public static class TournamentRoutes
{
    public static IEndpointRouteBuilder MapTournamentRoutes(this IEndpointRouteBuilder app)
    {
        var tournaments = app.MapGroup("/tournaments").AddEndpointFilter<ValidateIdFilter>();

        tournaments.MapGet("/", async (ITournamentDelegate d) => Results.Ok(await d.GetAllAsync()));

        tournaments.MapGet("/{id}", async (string id, ITournamentDelegate d) =>
            Results.Ok(await d.GetByIdAsync(id)));

        tournaments.MapPost("/", async (Tournament tournament, ITournamentDelegate d) =>
        {
            var created = await d.CreateAsync(tournament);
            return Results.Created($"/tournaments/{created.Id}", created);
        });

        tournaments.MapPut("/{id}", async (string id, Tournament tournament, ITournamentDelegate d) =>
        {
            tournament.Id = id;
            return Results.Ok(await d.UpdateAsync(tournament));
        });

        tournaments.MapPatch("/{id}", async (string id, TournamentPatch patch, ITournamentDelegate d) =>
            Results.Ok(await d.PatchAsync(id, patch)));

        tournaments.MapDelete("/{id}", async (string id, ITournamentDelegate d) =>
        {
            await d.DeleteAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}