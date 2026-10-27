// src/TournamentServices.Domain/Tournament.cs
using TournamentServices.Domain.Enums;
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

    // Estado del torneo: solo se pueden cambiar grupos/formato mientras este en Created
    public TournamentStatus Status { get; private set; } = TournamentStatus.Created;
    public string? ChampionTeamId { get; private set; }

    // Orden sorteado al iniciar; con el se reconstruye el bracket
    public List<string> SeededTeamIds { get; private set; } = new();

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

    public void Start(IReadOnlyList<string> seededTeamIds)
    {
        if (Status != TournamentStatus.Created)
        {
            throw new DomainValidationException("El torneo ya fue iniciado.");
        }

        if (seededTeamIds is null || seededTeamIds.Count < 2)
        {
            throw new DomainValidationException("Se necesitan al menos 2 equipos para iniciar el torneo.");
        }

        SeededTeamIds = seededTeamIds.ToList();
        Status = TournamentStatus.InProgress;
    }

    public void Finish(string championTeamId)
    {
        if (Status != TournamentStatus.InProgress)
        {
            throw new DomainValidationException("Solo se puede terminar un torneo en curso.");
        }

        if (string.IsNullOrWhiteSpace(championTeamId))
        {
            throw new DomainValidationException("El campeón es requerido.");
        }

        ChampionTeamId = championTeamId;
        Status = TournamentStatus.Finished;
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