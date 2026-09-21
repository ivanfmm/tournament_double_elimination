using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;

namespace TournamentServices.Domain.Brackets;

public class DoubleEliminationBracket
{
    private BracketSide Origin;

    private readonly string _tournamentId;
    private readonly int _rounds;
    private readonly BracketSlot[] _initialSlots;

    // --- Winners bracket ---
    private readonly List<string?[]> _wbLevels; // nivel 0..rounds, tamaño TotalSlots/2^nivel
    private readonly Dictionary<int, List<string>> _wbRoundLosers = new();
    private readonly Dictionary<int, int> _wbRoundExpectedMatchCount = new();
    private string? _winnersChampion;

    // --- Losers bracket ---
    private List<string> _lbCurrentSurvivors = new();
    private string?[]? _lbPendingSurvivorsBuffer;
    private int _nextWbRoundToMerge = 2; // los perdedores de WB Round 1 arrancan la LB directo
    private int? _lbAwaitingWbRound;
    private int _lbRoundCounter;
    private string? _losersChampion;

    // --- Gran final ---
    private string? _grandFinalMatchId;

    private readonly Dictionary<string, (BracketSide Origin, int Level, int Index)> _matchDestinations = new();
    private readonly Dictionary<string, Match> _matchesById = new();
    private readonly List<Match> _pendingMatches = new();

    public IReadOnlyList<Match> PendingMatches => _pendingMatches.AsReadOnly();
    public bool IsCompleted { get; private set; }
    public string? ChampionTeamId { get; private set; }

    public DoubleEliminationBracket(string tournamentId, IReadOnlyList<string> teamIds, ITeamSeeder seeder)
    {
        if (string.IsNullOrWhiteSpace(tournamentId))
        {
            throw new DomainValidationException("El id del torneo es requerido.");
        }

        if (teamIds is null || teamIds.Count < 2)
        {
            throw new DomainValidationException("Se necesitan al menos 2 equipos para armar el bracket.");
        }

        if (seeder is null)
        {
            throw new DomainValidationException("Se requiere un ITeamSeeder para armar el bracket.");
        }

        _tournamentId = tournamentId;

        var seeded = seeder.Seed(teamIds);
        var bracketSize = new BracketSize(seeded.Count);
        _rounds = bracketSize.Rounds;

        _initialSlots = BuildInitialSlots(seeded, bracketSize);

        _wbLevels = new List<string?[]>();
        for (var level = 0; level <= _rounds; level++)
        {
            _wbLevels.Add(new string?[bracketSize.TotalSlots >> level]);
        }

        RegisterWbRoundExpectedCounts(bracketSize.ByeCount);
        ResolveLevelZero();
    }

    // ----------------------------------------------------------------
    // Construcción inicial
    // ----------------------------------------------------------------

    private static BracketSlot[] BuildInitialSlots(IReadOnlyList<string> seededTeams, BracketSize bracketSize)
    {
        var slots = new BracketSlot[bracketSize.TotalSlots];
        var pairCount = bracketSize.TotalSlots / 2;
        var byeCount = bracketSize.ByeCount;
        var teamIndex = 0;

        // Reparte los byes en los primeros `byeCount` pares: como ByeCount
        // siempre es menor a la mitad de las casillas (por construcción de
        // BracketSize), nunca queda un par de "bye contra bye".
        for (var pair = 0; pair < pairCount; pair++)
        {
            slots[pair * 2] = BracketSlot.ForTeam(seededTeams[teamIndex++]);
            slots[pair * 2 + 1] = pair < byeCount
                ? BracketSlot.Bye()
                : BracketSlot.ForTeam(seededTeams[teamIndex++]);
        }

        return slots;
    }

    private void RegisterWbRoundExpectedCounts(int byeCount)
    {
        for (var level = 0; level < _rounds; level++)
        {
            _wbRoundExpectedMatchCount[level + 1] = _wbLevels[level].Length / 2;
        }

        // El Round 1 real tiene menos partidos que pares totales (los byes no juegan).
        _wbRoundExpectedMatchCount[1] -= byeCount;
    }

    private void ResolveLevelZero()
    {
        var pairCount = _initialSlots.Length / 2;

        for (var pair = 0; pair < pairCount; pair++)
        {
            var slotA = _initialSlots[pair * 2];
            var slotB = _initialSlots[pair * 2 + 1];

            if (slotA.IsBye)
            {
                AdvanceWinnerBracketWinner(level: 1, index: pair, teamId: slotB.TeamId!);
            }
            else if (slotB.IsBye)
            {
                AdvanceWinnerBracketWinner(level: 1, index: pair, teamId: slotA.TeamId!);
            }
            else
            {
                CreateWinnersMatch(level: 0, index: pair, homeTeamId: slotA.TeamId!, visitorTeamId: slotB.TeamId!);
            }
        }
    }

    // ----------------------------------------------------------------
    // Winners bracket: creación y avance
    // ----------------------------------------------------------------

    private void CreateWinnersMatch(int level, int index, string homeTeamId, string visitorTeamId)
    {
        var matchId = $"wb-r{level + 1}-m{index}";
        var match = new Match(matchId, _tournamentId, homeTeamId, visitorTeamId);

        _matchesById[matchId] = match;
        _matchDestinations[matchId] = (BracketSide.Winners, level, index);
        _pendingMatches.Add(match);
    }

    private void AdvanceWinnerBracketWinner(int level, int index, string teamId)
    {
        _wbLevels[level][index] = teamId;

        if (level == _rounds)
        {
            _winnersChampion = teamId;
            TryCreateGrandFinal();
            return;
        }

        TryCreateNextWinnersMatch(level, index);
    }

    private void TryCreateNextWinnersMatch(int level, int filledIndex)
    {
        var pairIndex = filledIndex / 2;
        var siblingIndex = pairIndex * 2 == filledIndex ? filledIndex + 1 : filledIndex - 1;
        var levelArray = _wbLevels[level];

        if (levelArray[siblingIndex] is null)
        {
            return; // el rival de este slot todavía no se conoce
        }

        CreateWinnersMatch(level, pairIndex, levelArray[pairIndex * 2]!, levelArray[pairIndex * 2 + 1]!);
    }

    // ----------------------------------------------------------------
    // Losers bracket: alimentación y avance
    // ----------------------------------------------------------------

    private void AddWbLoser(int wbRound, string teamId)
    {
        if (!_wbRoundLosers.TryGetValue(wbRound, out var list))
        {
            list = new List<string>();
            _wbRoundLosers[wbRound] = list;
        }

        list.Add(teamId);

        if (list.Count != _wbRoundExpectedMatchCount[wbRound])
        {
            return;
        }

        if (wbRound == 1)
        {
            StartLbRound(entrants: list, isDropIn: false, wbNewcomers: null, wbRoundLabel: null);
        }
        else if (_lbAwaitingWbRound == wbRound)
        {
            AdvanceLosersBracket();
        }
    }

    private void AdvanceLosersBracket()
    {
        if (_lbCurrentSurvivors.Count == 1 && _nextWbRoundToMerge > _rounds)
        {
            _losersChampion = _lbCurrentSurvivors[0];
            TryCreateGrandFinal();
            return;
        }

        if (_nextWbRoundToMerge <= _rounds)
        {
            if (_wbRoundLosers.TryGetValue(_nextWbRoundToMerge, out var newcomers) &&
                newcomers.Count == _wbRoundExpectedMatchCount[_nextWbRoundToMerge])
            {
                var mergeRound = _nextWbRoundToMerge;
                _nextWbRoundToMerge++;
                _lbAwaitingWbRound = null;
                StartLbRound(_lbCurrentSurvivors, isDropIn: true, wbNewcomers: newcomers, wbRoundLabel: mergeRound);
            }
            else
            {
                _lbAwaitingWbRound = _nextWbRoundToMerge;
            }

            return;
        }

        if (_lbCurrentSurvivors.Count > 1)
        {
            StartLbRound(_lbCurrentSurvivors, isDropIn: false, wbNewcomers: null, wbRoundLabel: null);
        }
    }

    private void StartLbRound(List<string> entrants, bool isDropIn, List<string>? wbNewcomers, int? wbRoundLabel)
    {
        var pairCount = isDropIn ? Math.Min(entrants.Count, wbNewcomers!.Count) : entrants.Count / 2;
        var totalSurvivors = isDropIn
            ? Math.Max(entrants.Count, wbNewcomers!.Count)
            : pairCount + entrants.Count % 2;

        var buffer = new string?[totalSurvivors];
        var roundLabel = isDropIn ? $"lb-dropin-{wbRoundLabel}" : $"lb-round-{_lbRoundCounter++}";

        for (var i = 0; i < pairCount; i++)
        {
            var home = entrants[i];
            var visitor = isDropIn ? wbNewcomers![i] : entrants[pairCount + i];
            var matchId = $"{roundLabel}-m{i}";
            var match = new Match(matchId, _tournamentId, home, visitor);

            _matchesById[matchId] = match;
            _matchDestinations[matchId] = (BracketSide.Losers, 0, i);
            _pendingMatches.Add(match);
        }

        // Sobrantes (solo pueden existir si hubo un número impar en algún
        // punto anterior del bracket, típicamente por byes): avanzan directo.
        var carryIndex = pairCount;
        if (isDropIn)
        {
            for (var i = pairCount; i < entrants.Count; i++) buffer[carryIndex++] = entrants[i];
            for (var i = pairCount; i < wbNewcomers!.Count; i++) buffer[carryIndex++] = wbNewcomers[i];
        }
        else if (entrants.Count % 2 == 1)
        {
            buffer[carryIndex] = entrants[^1];
        }

        _lbPendingSurvivorsBuffer = buffer;

        if (Array.TrueForAll(buffer, v => v is not null))
        {
            CompleteLbRound(buffer!.ToList()!);
        }
    }

    private void CompleteLbRound(List<string> survivors)
    {
        _lbCurrentSurvivors = survivors;
        _lbPendingSurvivorsBuffer = null;
        AdvanceLosersBracket();
    }

    // ----------------------------------------------------------------
    // Gran final
    // ----------------------------------------------------------------

    private void TryCreateGrandFinal()
    {
        if (_winnersChampion is null || _losersChampion is null || _grandFinalMatchId is not null)
        {
            return;
        }

        _grandFinalMatchId = "grand-final";
        var match = new Match(_grandFinalMatchId, _tournamentId, _winnersChampion, _losersChampion);

        _matchesById[_grandFinalMatchId] = match;
        _matchDestinations[_grandFinalMatchId] = (BracketSide.GrandFinal, 0, 0);
        _pendingMatches.Add(match);
    }

    // ----------------------------------------------------------------
    // API pública: reportar resultados
    // ----------------------------------------------------------------

    public void ReportResult(string matchId, Winner winner)
    {
        if (!_matchesById.TryGetValue(matchId, out var match) || !_pendingMatches.Contains(match))
        {
            throw new DomainValidationException($"No hay un partido pendiente con id '{matchId}'.");
        }

        var (origin, level, index) = _matchDestinations[matchId];
        var winnerTeamId = winner == Winner.Home ? match.HomeTeamId : match.VisitorTeamId;
        var loserTeamId = winner == Winner.Home ? match.VisitorTeamId : match.HomeTeamId;

        _pendingMatches.Remove(match);

        switch (origin)
        {
            case BracketSide.Winners:
                AdvanceWinnerBracketWinner(level + 1, index, winnerTeamId);
                AddWbLoser(level + 1, loserTeamId);
                break;

            case BracketSide.Losers:
                _lbPendingSurvivorsBuffer![index] = winnerTeamId;
                if (Array.TrueForAll(_lbPendingSurvivorsBuffer, v => v is not null))
                {
                    CompleteLbRound(_lbPendingSurvivorsBuffer!.ToList()!);
                }
                break;

            case BracketSide.GrandFinal:
                if (winnerTeamId == _winnersChampion)
                {
                    IsCompleted = true;
                    ChampionTeamId = winnerTeamId;
                }
                else
                {
                    var resetId = "grand-final-reset";
                    var resetMatch = new Match(resetId, _tournamentId, winnerTeamId, loserTeamId);
                    _matchesById[resetId] = resetMatch;
                    _matchDestinations[resetId] = (BracketSide.GrandFinalReset, 0, 0);
                    _pendingMatches.Add(resetMatch);
                }
                break;

            case BracketSide.GrandFinalReset:
                IsCompleted = true;
                ChampionTeamId = winnerTeamId;
                break;
        }
    }
}