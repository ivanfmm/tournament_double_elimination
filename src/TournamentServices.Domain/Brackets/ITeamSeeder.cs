namespace TournamentServices.Domain.Brackets;

public interface ITeamSeeder
{
    IReadOnlyList<string> Seed(IReadOnlyList<string> teamIds);
}