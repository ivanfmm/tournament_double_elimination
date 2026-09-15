using TournamentServices.Api.Dtos;
using TournamentServices.Domain;

namespace TournamentServices.Api.Extensions;

// Decision 6 del equipo: el mapeo DTO <-> Domain solo vive en Api.
public static class TeamMappings
{
    public static TeamDto ToDto(this Team team)
    {
        return new TeamDto(team.Id, team.Name);
    }

    // TeamDelegate.CreateAsync recibe el Team ya armado, por eso el id
    // se genera aqui al crear el objeto de Domain.
    public static Team ToDomain(this CreateTeamDto dto)
    {
        return new Team(Guid.NewGuid().ToString(), dto.Name);
    }

    public static Team ToDomain(this UpdateTeamDto dto, string id)
    {
        return new Team(id, dto.Name);
    }
}
