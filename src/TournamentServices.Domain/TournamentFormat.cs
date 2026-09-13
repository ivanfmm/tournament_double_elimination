using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;

namespace TournamentServices.Domain;

public record TournamentFormat
{
    public int MaxTeamsPerGroup { get; }
    public int NumberOfGroups { get; }
    public TournamentType Type { get; }

    public TournamentFormat(int maxTeamsPerGroup, int numberOfGroups, TournamentType type)
    {
        if (maxTeamsPerGroup <= 0)
        {
            throw new DomainValidationException("maxTeamsPerGroup debe ser mayor a cero.");
        }

        if (numberOfGroups <= 0)
        {
            throw new DomainValidationException("numberOfGroups debe ser mayor a cero.");
        }

        MaxTeamsPerGroup = maxTeamsPerGroup;
        NumberOfGroups = numberOfGroups;
        Type = type;
    }
}