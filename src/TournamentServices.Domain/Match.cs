using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Domain;

public class Match
{
    private string _homeTeamId = string.Empty;
    private string _visitorTeamId = string.Empty;

    public string Id { get; set; } = string.Empty;
    public string TournamentId { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    
    public string HomeTeamId
    {
        get => _homeTeamId;
        set
        {
            ValidateTeamId(value, "local");
            EnsureTeamsAreDifferent(value, _visitorTeamId);
            _homeTeamId = value;
        }
    }

    public string VisitorTeamId
    {
        get => _visitorTeamId;
        set
        {
            ValidateTeamId(value, "visitante");
            EnsureTeamsAreDifferent(_homeTeamId, value);
            _visitorTeamId = value;
        }
    }

    public Score Score { get; private set; } = new Score();
    public Enums.Winner? Winner { get; private set; }
    public bool IsCompleted { get; private set; }

    public Match() { }

    public Match(string id, string tournamentId, string homeTeamId, string visitorTeamId, string? groupId = null)
    {
        Id = id;
        TournamentId = tournamentId;
        GroupId = groupId;
        HomeTeamId = homeTeamId;
        VisitorTeamId = visitorTeamId;
    }

    public void SetScore(Score score)
    {
        Score = score;
        IsCompleted = true;
        Winner = CalculateWinner(score);
    }

    private static Enums.Winner? CalculateWinner(Score score)
    {
        if (score.HomeTeamScore > score.VisitorTeamScore)
        {
            return Enums.Winner.Home;
        }

        if (score.VisitorTeamScore > score.HomeTeamScore)
        {
            return Enums.Winner.Visitor;
        }

        return null;
    }

    private static void ValidateTeamId(string teamId, string role)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            throw new DomainValidationException($"El equipo {role} es requerido.");
        }
    }

    private static void EnsureTeamsAreDifferent(string homeTeamId, string visitorTeamId)
    {
        if (!string.IsNullOrWhiteSpace(homeTeamId) && !string.IsNullOrWhiteSpace(visitorTeamId) && homeTeamId == visitorTeamId)
        {
            throw new DomainValidationException("El equipo local y el visitante no pueden ser el mismo.");
        }
    }
}