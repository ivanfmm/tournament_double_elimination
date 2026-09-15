namespace TournamentServices.Api.Dtos;

public record GroupDto(string Id, string Name, string TournamentId, IReadOnlyList<TeamDto> Teams);

public record CreateGroupDto(string Name);

public record UpdateGroupDto(string Name);

public record AssignTeamsDto(List<string> TeamIds);
