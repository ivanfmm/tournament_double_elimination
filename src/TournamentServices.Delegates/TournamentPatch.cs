using TournamentServices.Domain;
namespace TournamentServices.Delegates;

public record TournamentPatch(string? Name = null, TournamentFormat? Format = null);