using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Tests;

// Forma del JSON que expone HOY la API (las rutas usan las clases de Domain).
// Los tests leen y mandan estos records; si algun dia regresan los DTOs de la
// capa Api, estos se reemplazan por los reales.

public record TeamDto(string Id, string Name);

public record CreateTeamDto(string Name);

public record UpdateTeamDto(string Name);

public record GroupDto(string Id, string Name, string TournamentId, IReadOnlyList<string> TeamIds);

public record CreateGroupDto(string Name);

public record UpdateGroupDto(string Name);

public record TournamentFormatDto(int MaxTeamsPerGroup, int NumberOfGroups, TournamentType Type);

public record ScoreDto(int HomeTeamScore, int VisitorTeamScore);

public record MatchDto(
    string Id,
    string TournamentId,
    string? GroupId,
    string? BracketKey,
    int? ResultOrder,
    string HomeTeamId,
    string VisitorTeamId,
    ScoreDto Score,
    Winner? Winner,
    bool IsCompleted);

public record TournamentDto(
    string Id,
    string Name,
    TournamentStatus Status,
    string? ChampionTeamId,
    IReadOnlyList<string> SeededTeamIds,
    TournamentFormatDto Format,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<MatchDto> Matches);

// Type es nullable para poder mandar un formato incompleto en los tests de validacion
public record FormatInputDto(int MaxTeamsPerGroup, int NumberOfGroups, TournamentType? Type);

public record CreateTournamentDto(string Name, FormatInputDto Format);

public record UpdateTournamentDto(string Name, FormatInputDto Format);

// PATCH plano: solo se cambia lo que venga (TournamentPatch en Delegates)
public record PatchTournamentDto(
    string? Name = null,
    int? MaxTeamsPerGroup = null,
    int? NumberOfGroups = null,
    TournamentType? Type = null);
