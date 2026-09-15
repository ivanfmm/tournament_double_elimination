using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Dtos;

// Solo el DTO de salida, porque TournamentDto lo necesita.
// Persona C agrega aqui CreateMatchDto y UpdateScoreDto.
public record ScoreDto(int HomeTeamScore, int VisitorTeamScore);

public record MatchDto(
    string Id,
    string TournamentId,
    string? GroupId,
    string HomeTeamId,
    string VisitorTeamId,
    TeamDto? HomeTeam,
    TeamDto? VisitorTeam,
    ScoreDto Score,
    Winner? Winner,
    bool IsCompleted);
