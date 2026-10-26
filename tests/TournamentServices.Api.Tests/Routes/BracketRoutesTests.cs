using System.Net;
using System.Net.Http.Json;
using TournamentServices.Domain.Enums;
using TournamentServices.TestBuilders;

namespace TournamentServices.Api.Tests.Routes;

public class BracketRoutesTests : TournamentApiTests
{
    private Task<string> NewTournamentAsync(int teams)
        => BracketApi.CreateTournamentWithTeamsAsync(Client, JsonOptions, teams);

    private Task<HttpResponseMessage> ReportAsync(string tournamentId, string matchId, int home, int visitor)
        => BracketApi.ReportAsync(Client, JsonOptions, tournamentId, matchId, home, visitor);

    // ---------- POST /start ----------

    [Fact]
    public async Task Start_Regresa200YCreaLaPrimeraRonda()
    {
        var tournamentId = await NewTournamentAsync(teams: 4);

        var response = await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tournament = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal(TournamentStatus.InProgress, tournament!.Status);
        Assert.Equal(4, tournament.SeededTeamIds.Count);

        var pending = await BracketApi.PendingMatchesAsync(Client, JsonOptions, tournamentId);
        Assert.Equal(new[] { "wb-r1-m0", "wb-r1-m1" }, pending.Select(m => m.BracketKey).OrderBy(k => k));
    }

    [Fact]
    public async Task Start_Regresa422SiYaInicio()
    {
        var tournamentId = await NewTournamentAsync(teams: 2);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        var response = await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Start_Regresa422SiNoEsDobleEliminacion()
    {
        await SeedAsync(new TournamentBuilder().WithId("t-1").Build()); // RoundRobin

        var response = await Client.PostAsync("/tournaments/t-1/start", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Start_Regresa404SiNoExisteElTorneo()
    {
        var response = await Client.PostAsync("/tournaments/no-existe/start", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PATCH /bracket/matches/{id}/score ----------

    [Fact]
    public async Task Score_Regresa422SiHayEmpate()
    {
        var tournamentId = await NewTournamentAsync(teams: 2);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);
        var match = (await BracketApi.PendingMatchesAsync(Client, JsonOptions, tournamentId))[0];

        var response = await ReportAsync(tournamentId, match.Id, 1, 1);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Score_Regresa422SiElPartidoYaTieneResultado()
    {
        var tournamentId = await NewTournamentAsync(teams: 4);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);
        var match = (await BracketApi.PendingMatchesAsync(Client, JsonOptions, tournamentId))[0];
        await ReportAsync(tournamentId, match.Id, 2, 0);

        var response = await ReportAsync(tournamentId, match.Id, 0, 2);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Score_Regresa404SiNoExisteElPartido()
    {
        var tournamentId = await NewTournamentAsync(teams: 2);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        var response = await ReportAsync(tournamentId, "no-existe", 1, 0);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RutasDeMatches_RechazanPartidosDelBracket()
    {
        var tournamentId = await NewTournamentAsync(teams: 2);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);
        var match = (await BracketApi.PendingMatchesAsync(Client, JsonOptions, tournamentId))[0];

        var score = await Client.PatchAsJsonAsync($"/tournaments/{tournamentId}/matches/{match.Id}/score", new ScoreDto(1, 0), JsonOptions);
        var create = await Client.PostAsJsonAsync($"/tournaments/{tournamentId}/matches",
            new { homeTeamId = match.HomeTeamId, visitorTeamId = match.VisitorTeamId }, JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, score.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    // ---------- Torneo completo + GET /champion ----------

    [Theory]
    [InlineData(6, 2, 1, 10)] // siempre gana el local: el invicto gana la gran final (2n-2)
    [InlineData(6, 1, 2, 11)] // siempre gana el visitante: hay reset de gran final (2n-1)
    public async Task TorneoCompleto_TerminaConCampeon(int teams, int home, int visitor, int expectedMatches)
    {
        var tournamentId = await NewTournamentAsync(teams);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        var played = await BracketApi.PlayUntilChampionAsync(Client, JsonOptions, tournamentId, home, visitor);

        Assert.Equal(expectedMatches, played);
        var champion = await Client.GetFromJsonAsync<TeamDto>($"/tournaments/{tournamentId}/champion", JsonOptions);
        var tournament = await Client.GetFromJsonAsync<TournamentDto>($"/tournaments/{tournamentId}", JsonOptions);
        Assert.Equal(TournamentStatus.Finished, tournament!.Status);
        Assert.Equal(champion!.Id, tournament.ChampionTeamId);
    }

    [Fact]
    public async Task Champion_Regresa422SiElTorneoNoHaTerminado()
    {
        var tournamentId = await NewTournamentAsync(teams: 2);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        var response = await Client.GetAsync($"/tournaments/{tournamentId}/champion");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---------- Bloqueos despues de iniciar ----------

    [Fact]
    public async Task DespuesDeIniciar_NoSeCambiaFormatoNiGrupos()
    {
        var tournamentId = await NewTournamentAsync(teams: 2);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);

        var patchFormat = await Client.PatchAsJsonAsync($"/tournaments/{tournamentId}", new PatchTournamentDto(NumberOfGroups: 2), JsonOptions);
        var newGroup = await Client.PostAsJsonAsync($"/tournaments/{tournamentId}/groups", new CreateGroupDto("Otro"), JsonOptions);
        var rename = await Client.PatchAsJsonAsync($"/tournaments/{tournamentId}", new PatchTournamentDto(Name: $"Renombrada {tournamentId}"), JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, patchFormat.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, newGroup.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode); // el nombre si se puede cambiar
    }
}
