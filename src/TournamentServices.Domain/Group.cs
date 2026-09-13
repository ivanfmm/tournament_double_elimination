using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Domain;

public class Group
{
    private string _name = string.Empty;
    private readonly List<string> _teamIds = new();

    public string Id { get; set; } = string.Empty;
    public string TournamentId { get; set; } = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException("El nombre del grupo no puede estar vacío.");
            }

            _name = value.Trim();
        }
    }

    public IReadOnlyList<string> TeamIds => _teamIds.AsReadOnly();

    public Group() { }

    public Group(string id, string tournamentId, string name)
    {
        Id = id;
        TournamentId = tournamentId;
        Name = name;
    }

    public void AddTeam(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            throw new DomainValidationException("El id del equipo es requerido.");
        }

        if (_teamIds.Contains(teamId))
        {
            throw new DomainValidationException("El equipo ya pertenece a este grupo.");
        }

        _teamIds.Add(teamId);
    }

    public void RemoveTeam(string teamId)
    {
        if (!_teamIds.Remove(teamId))
        {
            throw new DomainValidationException("El equipo no pertenece a este grupo.");
        }
    }
}