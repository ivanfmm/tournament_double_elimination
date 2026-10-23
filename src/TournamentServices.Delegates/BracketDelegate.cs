using System.Collections.Concurrent;
using TournamentServices.Domain;
using TournamentServices.Domain.Brackets;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using TournamentServices.Repositories;

namespace TournamentServices.Delegates;

// Orquesta el torneo de doble eliminacion. El bracket no se guarda tal cual:
// se guarda el orden sorteado (Tournament.SeededTeamIds) y los resultados
// (Match.ResultOrder), y en cada peticion se reconstruye con Replay.
//
// Concurrencia: las operaciones de un mismo torneo se ejecutan de una en una
// (candado por torneo). Esto basta con UNA instancia de la API; con varias
// instancias haria falta un candado en la base (SELECT ... FOR UPDATE).
// Los indices unicos de bracket_key y result_order son la red de seguridad.
public class BracketDelegate(
    ITournamentRepository tournamentRepository,
    IGroupRepository groupRepository,
    IMatchRepository matchRepository,
    ITeamRepository teamRepository,
    ITeamSeeder seeder) : IBracketDelegate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public Task<Tournament> StartAsync(string tournamentId)
        => WithTournamentLockAsync(tournamentId, () => StartCoreAsync(tournamentId));

    public Task<Match> ReportScoreAsync(string tournamentId, string matchId, int homeTeamScore, int visitorTeamScore)
        => WithTournamentLockAsync(tournamentId, () => ReportScoreCoreAsync(tournamentId, matchId, homeTeamScore, visitorTeamScore));

    private async Task<Tournament> StartCoreAsync(string tournamentId)
    {
        var tournament = await GetTournamentAsync(tournamentId);

        if (tournament.Format.Type != TournamentType.DoubleElimination)
        {
            throw new BusinessRuleException("Solo los torneos de doble eliminación se inician con bracket.");
        }

        if (tournament.Status != TournamentStatus.Created)
        {
            throw new BusinessRuleException("El torneo ya fue iniciado.");
        }

        // Todos los equipos inscritos en los grupos del torneo entran al bracket
        var groups = await groupRepository.GetByTournamentAsync(tournamentId);
        var teamIds = groups.SelectMany(g => g.TeamIds).Distinct().ToList();

        if (teamIds.Count < 2)
        {
            throw new BusinessRuleException("Se necesitan al menos 2 equipos inscritos para iniciar el torneo.");
        }

        tournament.Start(seeder.Seed(teamIds));

        // Primero los partidos y despues el torneo: si otro /start gano la
        // carrera, el indice unico de bracket_key falla aqui y el torneo no
        // queda con un orden sorteado distinto al de sus partidos.
        await SyncBracketAsync(tournament, new List<Match>());
        await tournamentRepository.UpdateAsync(tournament);

        return tournament;
    }

    private async Task<Match> ReportScoreCoreAsync(string tournamentId, string matchId, int homeTeamScore, int visitorTeamScore)
    {
        var tournament = await GetTournamentAsync(tournamentId);

        if (tournament.Status != TournamentStatus.InProgress)
        {
            throw new BusinessRuleException("El torneo no está en curso.");
        }

        var matches = (await matchRepository.GetByTournamentAsync(tournamentId)).ToList();
        var match = matches.FirstOrDefault(m => m.Id == matchId)
            ?? throw new NotFoundException($"Partido '{matchId}' no encontrado en el torneo '{tournamentId}'.");

        if (match.BracketKey is null)
        {
            throw new BusinessRuleException("El partido no pertenece al bracket.");
        }

        if (match.IsCompleted)
        {
            throw new BusinessRuleException("El partido ya tiene resultado y no se puede cambiar.");
        }

        if (homeTeamScore == visitorTeamScore)
        {
            throw new BusinessRuleException("En eliminación no puede haber empate.");
        }

        // El orden se calcula antes de SetScore para no contarse a si mismo
        match.ResultOrder = matches.Count(m => m.IsCompleted) + 1;
        match.SetScore(new Score(homeTeamScore, visitorTeamScore));
        await matchRepository.UpdateAsync(match);

        await SyncBracketAsync(tournament, matches);

        return match;
    }

    public async Task<Team> GetChampionAsync(string tournamentId)
    {
        var tournament = await GetTournamentAsync(tournamentId);

        if (tournament.Status != TournamentStatus.Finished || tournament.ChampionTeamId is null)
        {
            throw new BusinessRuleException("El torneo todavía no tiene campeón.");
        }

        return await teamRepository.GetByIdAsync(tournament.ChampionTeamId)
            ?? throw new NotFoundException($"Equipo '{tournament.ChampionTeamId}' no encontrado.");
    }

    // Reconstruye el bracket, guarda los partidos nuevos que hayan quedado
    // pendientes y, si ya hay campeon, termina el torneo.
    private async Task SyncBracketAsync(Tournament tournament, List<Match> matches)
    {
        var results = matches
            .Where(m => m.BracketKey is not null && m.IsCompleted && m.Winner is not null)
            .OrderBy(m => m.ResultOrder)
            .Select(m => (m.BracketKey!, m.Winner!.Value));

        var bracket = DoubleEliminationBracket.Replay(tournament.Id, tournament.SeededTeamIds, results);

        var knownKeys = matches
            .Where(m => m.BracketKey is not null)
            .Select(m => m.BracketKey!)
            .ToHashSet();

        foreach (var pending in bracket.PendingMatches.Where(p => !knownKeys.Contains(p.Id)))
        {
            var match = new Match(Guid.NewGuid().ToString(), tournament.Id, pending.HomeTeamId, pending.VisitorTeamId)
            {
                BracketKey = pending.Id
            };
            await matchRepository.AddAsync(match);
        }

        if (bracket.IsCompleted)
        {
            tournament.Finish(bracket.ChampionTeamId!);
            await tournamentRepository.UpdateAsync(tournament);
        }
    }

    private static async Task<T> WithTournamentLockAsync<T>(string tournamentId, Func<Task<T>> work)
    {
        var gate = Locks.GetOrAdd(tournamentId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await work();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<Tournament> GetTournamentAsync(string tournamentId)
        => await tournamentRepository.GetByIdAsync(tournamentId)
           ?? throw new NotFoundException($"Torneo '{tournamentId}' no encontrado.");
}
