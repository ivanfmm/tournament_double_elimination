using TournamentServices.Domain.Brackets;
using TournamentServices.Domain.Enums;
using TournamentServices.Domain.Exceptions;
using Xunit;

namespace TournamentServices.Domain.Tests.Brackets;

public class DoubleEliminationBracketTests
{
    private static readonly FakeTeamSeeder NoOpSeeder = new();

    [Fact]
    public void Constructor_With4Teams_CreatesTwoRound1Matches()
    {
        var teams = new List<string> { "t1", "t2", "t3", "t4" };
        var bracket = new DoubleEliminationBracket("tournament-1", teams, NoOpSeeder);

        Assert.Equal(2, bracket.PendingMatches.Count);
        Assert.False(bracket.IsCompleted);
    }

    [Fact]
    public void Constructor_WithFewerThanTwoTeams_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new DoubleEliminationBracket("tournament-1", new List<string> { "t1" }, NoOpSeeder));
    }

    [Fact]
    public void Constructor_With3Teams_ResolvesByeInRound1()
    {
        // Bracket de 4 casillas, 1 bye: solo debe haber 1 match pendiente
        // en vez de 2 (el par con bye no genera partido).
        var teams = new List<string> { "t1", "t2", "t3" };
        var bracket = new DoubleEliminationBracket("tournament-1", teams, NoOpSeeder);

        Assert.Single(bracket.PendingMatches);
    }

    [Fact]
    public void ReportResult_UnknownMatchId_ThrowsDomainValidationException()
    {
        var bracket = new DoubleEliminationBracket(
            "tournament-1", new List<string> { "t1", "t2", "t3", "t4" }, NoOpSeeder);

        Assert.Throws<DomainValidationException>(() => bracket.ReportResult("no-existe", Winner.Home));
    }

    [Fact]
    public void ReportResult_SameMatchTwice_ThrowsDomainValidationException()
    {
        var bracket = new DoubleEliminationBracket(
            "tournament-1", new List<string> { "t1", "t2", "t3", "t4" }, NoOpSeeder);
        var matchId = bracket.PendingMatches[0].Id;

        bracket.ReportResult(matchId, Winner.Home);

        Assert.Throws<DomainValidationException>(() => bracket.ReportResult(matchId, Winner.Home));
    }

    [Fact]
    public void FullPlaythrough_4Teams_WinnersBracketChampionWinsGrandFinal_NoReset()
    {
        // t1 vs t2 -> t1 gana. t3 vs t4 -> t3 gana. WB Final: t1 vs t3 -> t1 gana.
        // Losers bracket: t2 vs t4 -> t2 gana, pasa a la gran final contra t1.
        // t1 (invicto) gana la gran final: torneo termina SIN reset.
        var bracket = new DoubleEliminationBracket(
            "tournament-1", new List<string> { "t1", "t2", "t3", "t4" }, NoOpSeeder);

        PlayByHomeTeamId(bracket, "t1"); // wb-r1-m0: t1 vs t2 -> t1 gana
        PlayByHomeTeamId(bracket, "t3"); // wb-r1-m1: t3 vs t4 -> t3 gana

        Assert.Single(bracket.PendingMatches); // WB Final: t1 vs t3
        PlayByHomeTeamId(bracket, "t1"); // t1 gana la WB Final

        Assert.Single(bracket.PendingMatches); // Losers bracket: t2 vs t4
        PlayByHomeTeamId(bracket, "t2"); // t2 gana, pasa a la gran final

        var grandFinal = Assert.Single(bracket.PendingMatches);
        Assert.Contains("t1", new[] { grandFinal.HomeTeamId, grandFinal.VisitorTeamId });
        Assert.Contains("t2", new[] { grandFinal.HomeTeamId, grandFinal.VisitorTeamId });

        PlayByHomeTeamId(bracket, "t1"); // el invicto gana

        Assert.True(bracket.IsCompleted);
        Assert.Equal("t1", bracket.ChampionTeamId);
        Assert.Empty(bracket.PendingMatches);
    }

    [Fact]
    public void FullPlaythrough_4Teams_LosersBracketWinsGrandFinal_TriggersReset()
    {
        var bracket = new DoubleEliminationBracket(
            "tournament-1", new List<string> { "t1", "t2", "t3", "t4" }, NoOpSeeder);

        PlayByHomeTeamId(bracket, "t1"); // t1 gana a t2
        PlayByHomeTeamId(bracket, "t3"); // t3 gana a t4
        PlayByHomeTeamId(bracket, "t1"); // WB Final: t1 gana a t3
        PlayByHomeTeamId(bracket, "t2"); // Losers: t2 gana a t4

        var grandFinal = bracket.PendingMatches[0];
        var loserSideChampion = grandFinal.HomeTeamId == "t1" ? grandFinal.VisitorTeamId : grandFinal.HomeTeamId;

        // El campeón del losers bracket (no el invicto) gana la gran final.
        PlayByHomeTeamId(bracket, loserSideChampion);

        Assert.False(bracket.IsCompleted); // no termina todavía, falta el reset
        var resetMatch = Assert.Single(bracket.PendingMatches);
        Assert.Equal("grand-final-reset", resetMatch.Id);

        PlayByHomeTeamId(bracket, loserSideChampion); // gana el reset también

        Assert.True(bracket.IsCompleted);
        Assert.Equal(loserSideChampion, bracket.ChampionTeamId);
    }

    [Fact]
    public void FullPlaythrough_2Teams_OnlyNeedsRematchGrandFinal()
    {
        var bracket = new DoubleEliminationBracket(
            "tournament-1", new List<string> { "t1", "t2" }, NoOpSeeder);

        PlayByHomeTeamId(bracket, "t1"); // única ronda de WB: t1 gana

        var grandFinal = Assert.Single(bracket.PendingMatches);
        Assert.Contains("t1", new[] { grandFinal.HomeTeamId, grandFinal.VisitorTeamId });
        Assert.Contains("t2", new[] { grandFinal.HomeTeamId, grandFinal.VisitorTeamId });

        PlayByHomeTeamId(bracket, "t1");

        Assert.True(bracket.IsCompleted);
        Assert.Equal("t1", bracket.ChampionTeamId);
    }

    private static void PlayByHomeTeamId(DoubleEliminationBracket bracket, string expectedWinnerTeamId)
    {
        var match = bracket.PendingMatches.First(m =>
            m.HomeTeamId == expectedWinnerTeamId || m.VisitorTeamId == expectedWinnerTeamId);

        var winner = match.HomeTeamId == expectedWinnerTeamId
            ? Winner.Home
            : Winner.Visitor;

        bracket.ReportResult(match.Id, winner);
    }
}