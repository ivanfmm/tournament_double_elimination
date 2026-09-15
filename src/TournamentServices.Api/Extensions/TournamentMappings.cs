using TournamentServices.Api.Dtos;
using TournamentServices.Delegates;
using TournamentServices.Domain;

namespace TournamentServices.Api.Extensions;

public static class TournamentMappings
{
    public static TournamentDto ToDto(this Tournament tournament, IReadOnlyList<Team> teams)
    {
        var format = new TournamentFormatDto(
            tournament.Format.MaxTeamsPerGroup,
            tournament.Format.NumberOfGroups,
            tournament.Format.Type);

        return new TournamentDto(
            tournament.Id,
            tournament.Name,
            format,
            tournament.Groups.Select(g => g.ToDto(teams)).ToList(),
            tournament.Matches.Select(m => m.ToDto(teams)).ToList());
    }

    // TournamentDelegate.CreateAsync recibe el torneo armado,
    // por eso el id se genera aqui (mismo criterio que Team).
    public static Tournament ToDomain(this CreateTournamentDto dto)
    {
        return new Tournament(Guid.NewGuid().ToString(), dto.Name, dto.Format.ToDomain());
    }

    public static Tournament ToDomain(this UpdateTournamentDto dto, string id)
    {
        return new Tournament(id, dto.Name, dto.Format.ToDomain());
    }

    // El validador ya garantiza que Type no es null.
    public static TournamentFormat ToDomain(this FormatInputDto dto)
    {
        return new TournamentFormat(dto.MaxTeamsPerGroup, dto.NumberOfGroups, dto.Type!.Value);
    }

    // El PATCH puede traer solo una parte del formato. Como TournamentFormat
    // es un record inmutable, se arma uno nuevo mezclando lo que viene
    // con los valores actuales.
    public static TournamentPatch ToPatch(this PatchTournamentDto dto, TournamentFormat current)
    {
        TournamentFormat? format = null;

        if (dto.Format is not null)
        {
            format = new TournamentFormat(
                dto.Format.MaxTeamsPerGroup ?? current.MaxTeamsPerGroup,
                dto.Format.NumberOfGroups ?? current.NumberOfGroups,
                dto.Format.Type ?? current.Type);
        }

        return new TournamentPatch(dto.Name, format);
    }
}
