using TournamentServices.Domain.Enums;

namespace TournamentServices.Delegates;

public record TournamentPatch(string? Name = null, int? MaxTeamsPerGroup = null, int? NumberOfGroups = null, TournamentType? Type = null)
{
    public bool HasFormatChanges => MaxTeamsPerGroup is not null || NumberOfGroups is not null || Type is not null;
}