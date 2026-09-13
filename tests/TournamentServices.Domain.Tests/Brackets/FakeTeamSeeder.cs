namespace TournamentServices.Domain.Tests.Brackets;

/// <summary>
/// Seeder falso para usar en los tests de DoubleEliminationBracket
/// (y cualquier otra clase que dependa de ITeamSeeder). No baraja nada:
/// devuelve el orden tal cual se lo dieron, para que esos tests sean
/// 100% predecibles sin importarles el algoritmo de shuffle en sí.
/// </summary>
public class FakeTeamSeeder : TournamentServices.Domain.Brackets.ITeamSeeder
{
    public IReadOnlyList<string> Seed(IReadOnlyList<string> teamIds) => teamIds;
}