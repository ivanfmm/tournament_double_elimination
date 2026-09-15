using FluentValidation;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;
using TournamentServices.Api.Validators;
using TournamentServices.Delegates;
using TournamentServices.Domain.Exceptions;

namespace TournamentServices.Api.Routes;

public static class TeamRoutes
{
    public static IEndpointRouteBuilder MapTeamRoutes(this IEndpointRouteBuilder app)
    {
        var teams = app.MapGroup("/teams").WithTags("Teams");

        teams.MapGet("", GetAllAsync);
        teams.MapGet("/{teamId}", GetByIdAsync);
        teams.MapPost("", CreateAsync);
        teams.MapPut("/{teamId}", UpdateAsync);
        teams.MapDelete("/{teamId}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> GetAllAsync(ITeamDelegate teamDelegate)
    {
        var teams = await teamDelegate.GetAllAsync();
        return Results.Ok(teams.Select(t => t.ToDto()));
    }

    private static async Task<IResult> GetByIdAsync(string teamId, ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("teamId", teamId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var team = await teamDelegate.GetByIdAsync(teamId)
            ?? throw new NotFoundException($"No se encontro un equipo con id '{teamId}'.");

        return Results.Ok(team.ToDto());
    }

    private static async Task<IResult> CreateAsync(
        CreateTeamDto dto,
        IValidator<CreateTeamDto> validator,
        ITeamDelegate teamDelegate)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var team = await teamDelegate.CreateAsync(dto.ToDomain());
            return Results.Created($"/teams/{team.Id}", team.ToDto());
        }
        catch (DomainValidationException ex)
        {
            // El PRD pide 400 (no 422) para nombre duplicado en /teams.
            return DuplicateName(ex);
        }
    }

    private static async Task<IResult> UpdateAsync(
        string teamId,
        UpdateTeamDto dto,
        IValidator<UpdateTeamDto> validator,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("teamId", teamId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var team = await teamDelegate.UpdateAsync(teamId, dto.ToDomain(teamId));
            return Results.Ok(team.ToDto());
        }
        catch (DomainValidationException ex)
        {
            return DuplicateName(ex);
        }
    }

    private static async Task<IResult> DeleteAsync(string teamId, ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("teamId", teamId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        await teamDelegate.DeleteAsync(teamId);
        return Results.NoContent();
    }

    private static IResult DuplicateName(DomainValidationException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = new[] { ex.Message }
        });
    }
}
