using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Tests.Postgres;

// ABC de torneos (alta, baja, cambio) contra un Postgres real.
// Incluye el PATCH parcial y el borrado en cascada de grupos y partidos.
public class PostgresTournamentCrudTests : PostgresTestBase
{
    private static CreateTournamentDto NewTournament(
        string name = "Copa",
        int maxTeamsPerGroup = 4,
        int numberOfGroups = 2,
        TournamentType type = TournamentType.RoundRobin)
    {
        return new CreateTournamentDto(name, new FormatInputDto(maxTeamsPerGroup, numberOfGroups, type));
    }

    private Task<HttpResponseMessage> PatchRawAsync(string url, string json)
    {
        return Client.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    // ---------------- ALTA ----------------

    [PostgresFact]
    public async Task Alta_CreaElTorneoYLoGuardaEnPostgres()
    {
        var response = await Client.PostAsJsonAsync("/tournaments", NewTournament("Copa Cetys"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions))!;

        Assert.Equal("Copa Cetys", created.Name);
        Assert.Equal(new TournamentFormatDto(4, 2, TournamentType.RoundRobin), created.Format);
        Assert.Empty(created.Groups);
        Assert.Empty(created.Matches);
        Assert.Equal($"/tournaments/{created.Id}", response.Headers.Location!.ToString());

        Assert.True(await QueryAsync(db => db.Tournaments.AnyAsync(t => t.Id == created.Id)));
    }

    [PostgresTheory]
    [InlineData(TournamentType.RoundRobin)]
    [InlineData(TournamentType.Nfl)]
    [InlineData(TournamentType.DoubleElimination)]
    public async Task Alta_GuardaElFormatoDeCadaTipo(TournamentType type)
    {
        var created = await PostAsync<TournamentDto>("/tournaments", NewTournament(type: type));

        // Se vuelve a leer de Postgres: el enum se guarda como texto
        // (conversion HasConversion<string> del DbContext).
        var loaded = await GetAsync<TournamentDto>($"/tournaments/{created.Id}");
        Assert.Equal(type, loaded.Format.Type);
    }

    [PostgresFact]
    public async Task Alta_RechazaUnFormatoInvalido()
    {
        var body = new CreateTournamentDto("Copa", new FormatInputDto(0, 2, TournamentType.RoundRobin));

        var response = await Client.PostAsJsonAsync("/tournaments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await QueryAsync(db => db.Tournaments.CountAsync()));
    }

    // ---------------- CAMBIO (PUT) ----------------

    [PostgresFact]
    public async Task Cambio_PutActualizaNombreYFormato()
    {
        var created = await PostAsync<TournamentDto>("/tournaments", NewTournament("Viejo"));

        var body = new UpdateTournamentDto("Nuevo", new FormatInputDto(8, 4, TournamentType.Nfl));
        var response = await Client.PutAsJsonAsync($"/tournaments/{created.Id}", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loaded = await GetAsync<TournamentDto>($"/tournaments/{created.Id}");
        Assert.Equal("Nuevo", loaded.Name);
        Assert.Equal(new TournamentFormatDto(8, 4, TournamentType.Nfl), loaded.Format);
    }

    [PostgresFact]
    public async Task Cambio_PutNoPierdeLosGruposDelTorneo()
    {
        var created = await PostAsync<TournamentDto>("/tournaments", NewTournament("Copa"));
        await PostAsync<GroupDto>($"/tournaments/{created.Id}/groups", new CreateGroupDto("Grupo A"));

        var body = new UpdateTournamentDto("Copa 2", new FormatInputDto(8, 4, TournamentType.Nfl));
        var response = await Client.PutAsJsonAsync($"/tournaments/{created.Id}", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions))!;
        Assert.Single(updated.Groups);
    }

    [PostgresFact]
    public async Task Cambio_PutRegresa404SiNoExiste()
    {
        var body = new UpdateTournamentDto("Nuevo", new FormatInputDto(8, 4, TournamentType.Nfl));

        var response = await Client.PutAsJsonAsync("/tournaments/no-existe", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- CAMBIO (PATCH parcial) ----------------

    [PostgresFact]
    public async Task Cambio_PatchSoloNombre_ConservaElFormato()
    {
        var created = await PostAsync<TournamentDto>("/tournaments", NewTournament("Viejo"));

        var response = await PatchRawAsync($"/tournaments/{created.Id}", """{"name":"Nuevo"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loaded = await GetAsync<TournamentDto>($"/tournaments/{created.Id}");
        Assert.Equal("Nuevo", loaded.Name);
        Assert.Equal(new TournamentFormatDto(4, 2, TournamentType.RoundRobin), loaded.Format);
    }

    [PostgresFact]
    public async Task Cambio_PatchSoloUnCampoDelFormato_ConservaElResto()
    {
        var created = await PostAsync<TournamentDto>("/tournaments", NewTournament("Copa"));

        var response = await PatchRawAsync($"/tournaments/{created.Id}", """{"format":{"type":"NFL"}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loaded = await GetAsync<TournamentDto>($"/tournaments/{created.Id}");
        Assert.Equal("Copa", loaded.Name);
        Assert.Equal(new TournamentFormatDto(4, 2, TournamentType.Nfl), loaded.Format);
    }

    [PostgresFact]
    public async Task Cambio_PatchRegresa400SiElCampoEnviadoEsInvalido()
    {
        var created = await PostAsync<TournamentDto>("/tournaments", NewTournament("Copa"));

        var response = await PatchRawAsync($"/tournaments/{created.Id}", """{"name":""}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Copa", (await GetAsync<TournamentDto>($"/tournaments/{created.Id}")).Name);
    }

    [PostgresFact]
    public async Task Cambio_PatchRegresa404SiNoExiste()
    {
        var response = await PatchRawAsync("/tournaments/no-existe", """{"name":"Nuevo"}""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- BAJA ----------------

    [PostgresFact]
    public async Task Baja_BorraElTorneoYSusGruposEnCascada()
    {
        var tournament = await PostAsync<TournamentDto>("/tournaments", NewTournament("Copa"));
        var group = await PostAsync<GroupDto>($"/tournaments/{tournament.Id}/groups", new CreateGroupDto("Grupo A"));

        var response = await Client.DeleteAsync($"/tournaments/{tournament.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await QueryAsync(db => db.Tournaments.AnyAsync(t => t.Id == tournament.Id)));
        Assert.False(await QueryAsync(db => db.Groups.AnyAsync(g => g.Id == group.Id)));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfGetAsync($"/tournaments/{tournament.Id}"));
    }

    [PostgresFact]
    public async Task Baja_Regresa404SiNoExiste()
    {
        var response = await Client.DeleteAsync("/tournaments/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- LECTURA ----------------

    [PostgresFact]
    public async Task Lectura_ListaTodosLosTorneosCreados()
    {
        await PostAsync<TournamentDto>("/tournaments", NewTournament("Copa A"));
        await PostAsync<TournamentDto>("/tournaments", NewTournament("Copa B"));

        var all = await GetAsync<List<TournamentDto>>("/tournaments");

        Assert.Equal(2, all.Count);
        Assert.Equal(new[] { "Copa A", "Copa B" }, all.Select(t => t.Name).OrderBy(n => n));
    }

    [PostgresFact]
    public async Task Lectura_Regresa400SiElIdTieneFormatoInvalido()
    {
        Assert.Equal(HttpStatusCode.BadRequest, await StatusOfGetAsync("/tournaments/id_invalido"));
    }
}
