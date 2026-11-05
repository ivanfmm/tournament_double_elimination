using TournamentServices.Api.Filters;
using TournamentServices.Delegates;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;

namespace TournamentServices.Api.Routes;

public static class TeamRoutes
{
    public static IEndpointRouteBuilder MapTeamRoutes(this IEndpointRouteBuilder app)
    {
        var teams = app.MapGroup("/teams").AddEndpointFilter<ValidateIdFilter>();

        teams.MapGet("/", async (ITeamDelegate d) => Results.Ok(await d.GetAllAsync()));

        teams.MapGet("/{id}", async (string id, ITeamDelegate d) =>
            Results.Ok(await d.GetByIdAsync(id)
                ?? throw new NotFoundException($"Equipo '{id}' no encontrado.")));

        teams.MapPost("/", async (Team team, ITeamDelegate d) =>
        {
            var created = await d.CreateAsync(team);
            return Results.Created($"/teams/{created.Id}", created);
        });

        teams.MapPut("/{id}", async (string id, Team team, ITeamDelegate d) =>
            Results.Ok(await d.UpdateAsync(id, team)));

        teams.MapDelete("/{id}", async (string id, ITeamDelegate d) =>
        {
            await d.DeleteAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}