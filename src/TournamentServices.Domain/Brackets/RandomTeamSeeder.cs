namespace TournamentServices.Domain.Brackets;

public class RandomTeamSeeder : ITeamSeeder
{
    private readonly Random _random;

    public RandomTeamSeeder(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public IReadOnlyList<string> Seed(IReadOnlyList<string> teamIds)
    {
        var shuffled = teamIds.ToList();

        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }
}