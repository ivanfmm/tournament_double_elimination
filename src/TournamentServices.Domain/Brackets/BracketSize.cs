using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Domain.Brackets;

public record BracketSize
{
    public int TeamCount { get; }
    public int TotalSlots { get; }
    public int ByeCount => TotalSlots - TeamCount;
    public int Rounds => (int)Math.Log2(TotalSlots);

    public BracketSize(int teamCount)
    {
        if (teamCount < 2)
        {
            throw new DomainValidationException("Se necesitan al menos 2 equipos para armar un bracket de eliminación.");
        }

        TeamCount = teamCount;
        TotalSlots = NextPowerOfTwo(teamCount);
    }

    private static int NextPowerOfTwo(int value)
    {
        var power = 1;
        while (power < value)
        {
            power *= 2;
        }

        return power;
    }
}