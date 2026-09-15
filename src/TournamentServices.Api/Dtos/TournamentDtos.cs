using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Dtos;

public record TournamentFormatDto(int MaxTeamsPerGroup, int NumberOfGroups, TournamentType Type);

public record TournamentDto(
    string Id,
    string Name,
    TournamentFormatDto Format,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<MatchDto> Matches);

// Type es nullable para detectar cuando no viene en el JSON.
// Si fuera TournamentType normal, un "type" faltante se volveria
// RoundRobin (el valor 0) sin avisar.
public record FormatInputDto(int MaxTeamsPerGroup, int NumberOfGroups, TournamentType? Type);

public record CreateTournamentDto(string Name, FormatInputDto Format);

public record UpdateTournamentDto(string Name, FormatInputDto Format);

// PATCH: todo es opcional, solo se pisa lo que venga.
public record PatchFormatDto(int? MaxTeamsPerGroup, int? NumberOfGroups, TournamentType? Type);

public record PatchTournamentDto(string? Name, PatchFormatDto? Format);
