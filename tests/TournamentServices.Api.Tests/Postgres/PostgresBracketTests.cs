using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Tests.Postgres;

// Lo que solo se puede probar contra Postgres real: el orden sorteado como
// text[], los indices unicos del bracket, el soft delete y la concurrencia.
public class PostgresBracketTests : PostgresTestBase
{
    [PostgresFact]
    public async Task TorneoCompleto_GuardaElBracketYElCampeonEnPostgres()
    {
        var tournamentId = await BracketApi.CreateTournamentWithTeamsAsync(Client, JsonOptions, teamCount: 5);
        var start = await Client.PostAsync($"/tournaments/{tournamentId}/start", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        var played = await BracketApi.PlayUntilChampionAsync(Client, JsonOptions, tournamentId, home: 3, visitor: 1);

        Assert.Equal(8, played); // 2n-2 con 5 equipos, sin reset

        var stored = await QueryAsync(db => db.Tournaments.AsNoTracking().FirstAsync(t => t.Id == tournamentId));
        Assert.Equal(TournamentStatus.Finished, stored.Status);
        Assert.Equal(5, stored.SeededTeamIds.Count);
        Assert.NotNull(stored.ChampionTeamId);

        // Sin huecos ni repetidos en el orden de resultados
        var orders = await QueryAsync(db => db.Matches.Where(m => m.TournamentId == tournamentId)
            .Select(m => m.ResultOrder).OrderBy(o => o).ToListAsync());
        Assert.Equal(Enumerable.Range(1, played).Cast<int?>(), orders);
    }

    [PostgresFact]
    public async Task Score_MismoResultadoEnParalelo_SoloUnoSeGuarda()
    {
        var tournamentId = await BracketApi.CreateTournamentWithTeamsAsync(Client, JsonOptions, teamCount: 4);
        await Client.PostAsync($"/tournaments/{tournamentId}/start", null);
        var match = (await BracketApi.PendingMatchesAsync(Client, JsonOptions, tournamentId))[0];

        // Como un doble clic: 5 peticiones iguales al mismo tiempo
        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => BracketApi.ReportAsync(Client, JsonOptions, tournamentId, match.Id, 2, 0)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK),
            r => Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode));
    }

    [PostgresFact]
    public async Task BorrarPartido_LoOcultaConSoftDelete()
    {
        // Reemplaza las pruebas de borrado de MatchRepository: InMemory no soporta ExecuteUpdateAsync
        var tournament = await PostAsync<TournamentDto>("/tournaments",
            new CreateTournamentDto("Liga", new FormatInputDto(4, 1, TournamentType.RoundRobin)));
        var groupsUrl = $"/tournaments/{tournament.Id}/groups";
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("A"));
        var pumas = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));
        var chivas = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Chivas"));
        await Client.PatchAsJsonAsync($"{groupsUrl}/{group.Id}/teams", new List<string> { pumas.Id, chivas.Id }, JsonOptions);
        var match = await PostAsync<MatchDto>($"/tournaments/{tournament.Id}/matches",
            new { homeTeamId = pumas.Id, visitorTeamId = chivas.Id });

        var delete = await Client.DeleteAsync($"/tournaments/{tournament.Id}/matches/{match.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfGetAsync($"/tournaments/{tournament.Id}/matches/{match.Id}"));
        // La fila sigue en la base, solo marcada con deleted_at
        Assert.True(await QueryAsync(db => db.Matches.IgnoreQueryFilters().AnyAsync(m => m.Id == match.Id)));
    }
}
