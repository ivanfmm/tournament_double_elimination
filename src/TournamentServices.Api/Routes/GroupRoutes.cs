using FluentValidation;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;
using TournamentServices.Api.Validators;
using TournamentServices.Delegates;
using TournamentServices.Domain.Exceptions;

namespace TournamentServices.Api.Routes;

public static class GroupRoutes
{
    public static IEndpointRouteBuilder MapGroupRoutes(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/tournaments/{tournamentId}/groups").WithTags("Groups");

        groups.MapGet("", GetByTournamentAsync);
        groups.MapGet("/{groupId}", GetByIdAsync);
        groups.MapPost("", CreateAsync);
        groups.MapPut("/{groupId}", UpdateAsync);
        groups.MapDelete("/{groupId}", DeleteAsync);
        groups.MapPatch("/{groupId}/teams", AssignTeamsAsync);

        return app;
    }

    private static async Task<IResult> GetByTournamentAsync(
        string tournamentId,
        IGroupDelegate groupDelegate,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var groups = await groupDelegate.GetByTournamentAsync(tournamentId);
        var teams = await teamDelegate.GetAllAsync();

        return Results.Ok(groups.Select(g => g.ToDto(teams)));
    }

    private static async Task<IResult> GetByIdAsync(
        string tournamentId,
        string groupId,
        IGroupDelegate groupDelegate,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId), ("groupId", groupId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var group = await groupDelegate.GetByIdAsync(tournamentId, groupId)
            ?? throw new NotFoundException($"No existe el grupo '{groupId}' en el torneo '{tournamentId}'.");

        var teams = await teamDelegate.GetAllAsync();
        return Results.Ok(group.ToDto(teams));
    }

    private static async Task<IResult> CreateAsync(
        string tournamentId,
        CreateGroupDto dto,
        IValidator<CreateGroupDto> validator,
        IGroupDelegate groupDelegate)
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

        var group = await groupDelegate.CreateAsync(tournamentId, dto.Name);

        // Un grupo recien creado no tiene equipos.
        var body = group.ToDto(Array.Empty<Domain.Team>());
        return Results.Created($"/tournaments/{tournamentId}/groups/{group.Id}", body);
    }

    private static async Task<IResult> UpdateAsync(
        string tournamentId,
        string groupId,
        UpdateGroupDto dto,
        IValidator<UpdateGroupDto> validator,
        IGroupDelegate groupDelegate,
        ITeamDelegate teamDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId), ("groupId", groupId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var group = await groupDelegate.UpdateAsync(tournamentId, groupId, dto.Name);
        var teams = await teamDelegate.GetAllAsync();
        return Results.Ok(group.ToDto(teams));
    }

    private static async Task<IResult> DeleteAsync(
        string tournamentId,
        string groupId,
        IGroupDelegate groupDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId), ("groupId", groupId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        await groupDelegate.DeleteAsync(tournamentId, groupId);
        return Results.NoContent();
    }

    private static async Task<IResult> AssignTeamsAsync(
        string tournamentId,
        string groupId,
        AssignTeamsDto dto,
        IValidator<AssignTeamsDto> validator,
        IGroupDelegate groupDelegate)
    {
        var invalidId = IdFormat.Validate(("tournamentId", tournamentId), ("groupId", groupId));
        if (invalidId is not null)
        {
            return invalidId;
        }

        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        await groupDelegate.AssignTeamsAsync(tournamentId, groupId, dto.TeamIds);
        return Results.NoContent();
    }
}
