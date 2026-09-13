using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Domain;

public record Score
{
    public int HomeTeamScore { get; }
    public int VisitorTeamScore { get; }

    public Score() : this(0, 0) { }

    public Score(int homeTeamScore, int visitorTeamScore)
    {
        if (homeTeamScore < 0)
        {
            throw new DomainValidationException("El marcador del equipo local no puede ser negativo.");
        }

        if (visitorTeamScore < 0)
        {
            throw new DomainValidationException("El marcador del equipo visitante no puede ser negativo.");
        }

        HomeTeamScore = homeTeamScore;
        VisitorTeamScore = visitorTeamScore;
    }
}