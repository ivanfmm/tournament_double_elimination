using Microsoft.AspNetCore.Mvc;
using TournamentServices.Api.Filters;
using TournamentServices.Delegates;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Domain;

namespace TournamentServices.Api.Routes;

public static class GroupRoutes
{
    public static IEndpointRouteBuilder MapGroupRoutes(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/tournaments/{tournamentId}/groups")
            .AddEndpointFilter<ValidateIdFilter>();

        groups.MapGet("/", async (string tournamentId, IGroupDelegate d) =>
            Results.Ok(await d.GetByTournamentAsync(tournamentId)));

        groups.MapGet("/{groupId}", async (string tournamentId, string groupId, IGroupDelegate d) =>
            Results.Ok(await d.GetByIdAsync(tournamentId, groupId)
                ?? throw new NotFoundException($"Grupo '{groupId}' no encontrado.")));

        groups.MapPost("/", async (string tournamentId, Group group, IGroupDelegate d) =>
        {
            var created = await d.CreateAsync(tournamentId, group.Name);
            return Results.Created($"/tournaments/{tournamentId}/groups/{created.Id}", created);
        });

        groups.MapPut("/{groupId}", async (string tournamentId, string groupId, Group group, IGroupDelegate d) =>
            Results.Ok(await d.UpdateAsync(tournamentId, groupId, group.Name)));

        groups.MapDelete("/{groupId}", async (string tournamentId, string groupId, IGroupDelegate d) =>
        {
            await d.DeleteAsync(tournamentId, groupId);
            return Results.NoContent();
        });

        groups.MapPatch("/{groupId}/teams", async (
            string tournamentId, string groupId, [FromBody] List<string> teamIds, IGroupDelegate d) =>
        {
            await d.AssignTeamsAsync(tournamentId, groupId, teamIds);
            return Results.NoContent();
        });

        return app;
    }
}