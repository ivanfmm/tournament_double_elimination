using TournamentServices.Api.Dtos;
using TournamentServices.Domain;

namespace TournamentServices.Api.Extensions;

public static class GroupMappings
{
    // Group solo guarda ids de equipos. Para llenar GroupDto.Teams se
    // le pasa la lista de equipos ya cargada. Si un id ya no existe
    // (equipo borrado), simplemente no se incluye.
    public static GroupDto ToDto(this Group group, IReadOnlyList<Team> teams)
    {
        var groupTeams = group.TeamIds
            .Select(id => teams.FirstOrDefault(t => t.Id == id))
            .Where(t => t is not null)
            .Select(t => t!.ToDto())
            .ToList();

        return new GroupDto(group.Id, group.Name, group.TournamentId, groupTeams);
    }
}
