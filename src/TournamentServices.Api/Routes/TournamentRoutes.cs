using FluentValidation;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;
using TournamentServices.Api.Validators;
using TournamentServices.Delegates;

namespace TournamentServices.Api.Routes;

public static class TournamentRoutes
{
    public static IEndpointRouteBuilder MapTournamentRoutes(this IEndpointRouteBuilder app)
    {
        var tournaments = app.MapGroup("/tournaments").WithTags("Tournaments");

        tournaments.MapGet("", GetAllAsync);
        tournaments.MapGet("/{tournamentId}", GetByIdAsync);
        tournaments.MapPost("", CreateAsync);
        tournaments.MapPut("/{tournamentId}", UpdateAsync);
        tournaments.MapPatch("/{tournamentId}", PatchAsync);
        tournaments.MapDelete("/{tournamentId}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> GetAllAsync(
        ITournamentDelegate tournamentDelegate,
        ITeamDelegate teamDelegate)
    {
        var tournaments = await tournamentDelegate.GetAllAsync();
        var teams = await teamDelegate.GetAllAsync();

        return Results.Ok(tournaments.Select(t => t.ToDto(teams)));
    }

    private static async Task<IResult> GetByIdAsync(
        string tournamentId,
        ITournamentDelegate tournamentDelegate,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        // GetByIdAsync del delegate ya lanza NotFoundException (404).
        var tournament = await tournamentDelegate.GetByIdAsync(tournamentId);
        var teams = await teamDelegate.GetAllAsync();

        return Results.Ok(tournament.ToDto(teams));
    }

    private static async Task<IResult> CreateAsync(
        CreateTournamentDto dto,
        IValidator<CreateTournamentDto> validator,
        ITournamentDelegate tournamentDelegate)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var tournament = await tournamentDelegate.CreateAsync(dto.ToDomain());

        // Un torneo nuevo no tiene grupos ni partidos.
        var body = tournament.ToDto(Array.Empty<Domain.Team>());
        return Results.Created($"/tournaments/{tournament.Id}", body);
    }

    private static async Task<IResult> UpdateAsync(
        string tournamentId,
        UpdateTournamentDto dto,
        IValidator<UpdateTournamentDto> validator,
        ITournamentDelegate tournamentDelegate,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        await tournamentDelegate.UpdateAsync(dto.ToDomain(tournamentId));

        // Se vuelve a leer para regresar tambien grupos y partidos.
        var updated = await tournamentDelegate.GetByIdAsync(tournamentId);
        var teams = await teamDelegate.GetAllAsync();
        return Results.Ok(updated.ToDto(teams));
    }

    private static async Task<IResult> PatchAsync(
        string tournamentId,
        PatchTournamentDto dto,
        IValidator<PatchTournamentDto> validator,
        ITournamentDelegate tournamentDelegate,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        // Se necesita el formato actual para completar un PATCH parcial.
        // Si no existe, esta llamada ya lanza el 404.
        var current = await tournamentDelegate.GetByIdAsync(tournamentId);

        var patched = await tournamentDelegate.PatchAsync(tournamentId, dto.ToPatch(current.Format));
        var teams = await teamDelegate.GetAllAsync();
        return Results.Ok(patched.ToDto(teams));
    }

    private static async Task<IResult> DeleteAsync(
        string tournamentId,
        ITournamentDelegate tournamentDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        // El repository borra en cascada grupos y partidos.
        await tournamentDelegate.DeleteAsync(tournamentId);
        return Results.NoContent();
    }
}
