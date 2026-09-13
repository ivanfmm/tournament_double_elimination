// src/TournamentServices.Domain/Tournament.cs
using TournamentServices.Domain.Exceptions;

namespace TournamentServices.Domain;

public class Tournament
{
    private string _name = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException("El nombre del torneo no puede estar vacío.");
            }

            _name = value.Trim();
        }
    }

    public TournamentFormat Format { get; set; } = null!;

    public List<Group> Groups { get; init; } = new();
    public List<Match> Matches { get; init; } = new();

    public Tournament() { }

    public Tournament(string id, string name, TournamentFormat format)
    {
        Id = id;
        Name = name;
        Format = format ?? throw new DomainValidationException("El formato del torneo es requerido.");
    }

    public void AddGroup(Group group)
    {
        if (group is null)
        {
            throw new DomainValidationException("El grupo no puede ser nulo.");
        }

        if (Groups.Any(g => string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException($"Ya existe un grupo con el nombre '{group.Name}' en este torneo.");
        }

        if (Groups.Count >= Format.NumberOfGroups)
        {
            throw new DomainValidationException(
                $"El torneo ya alcanzó el máximo de {Format.NumberOfGroups} grupos.");
        }

        Groups.Add(group);
    }

    public void AddMatch(Match match)
    {
        if (match is null)
        {
            throw new DomainValidationException("El partido no puede ser nulo.");
        }

        Matches.Add(match);
    }
}