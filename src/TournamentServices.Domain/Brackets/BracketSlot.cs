using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Domain.Brackets;

/// <summary>
/// Representa UNA posición dentro del bracket: puede ser un equipo real
/// o un "bye" (posición vacía que hace que el rival avance automáticamente).
/// Se usan factory methods en vez de un constructor público para dejar
/// explícitas las dos únicas formas válidas de crear un slot.
/// </summary>
public record BracketSlot
{
    public string? TeamId { get; }
    public bool IsBye => TeamId is null;

    private BracketSlot(string? teamId)
    {
        TeamId = teamId;
    }

    public static BracketSlot ForTeam(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            throw new DomainValidationException("El id del equipo no puede estar vacío.");
        }

        return new BracketSlot(teamId);
    }

    public static BracketSlot Bye() => new(teamId: null);
}