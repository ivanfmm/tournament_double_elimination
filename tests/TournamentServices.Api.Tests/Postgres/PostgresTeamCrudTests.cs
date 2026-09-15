using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Tests.Postgres;

// ABC de equipos (alta, baja, cambio) contra un Postgres real.
// Cada test usa su propia base y pasa siempre por la API (no siembra
// datos a mano), asi se prueba el camino completo:
// Ruta -> Validator -> Delegate -> Repository -> EF Core -> Postgres.
public class PostgresTeamCrudTests : PostgresTestBase
{
    // ---------------- ALTA ----------------

    [PostgresFact]
    public async Task Alta_CreaElEquipoYLoGuardaEnPostgres()
    {
        var response = await Client.PostAsJsonAsync("/teams", new CreateTeamDto("Pumas"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<TeamDto>(JsonOptions))!;

        Assert.Equal("Pumas", created.Name);
        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.Equal($"/teams/{created.Id}", response.Headers.Location!.ToString());

        // El renglon existe de verdad en la tabla Teams.
        Assert.True(await QueryAsync(db => db.Teams.AnyAsync(t => t.Id == created.Id && t.Name == "Pumas")));
    }

    [PostgresFact]
    public async Task Alta_ElEquipoCreadoSeLeeConGet()
    {
        var created = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Chivas"));

        var loaded = await GetAsync<TeamDto>($"/teams/{created.Id}");
        Assert.Equal(created, loaded);

        var all = await GetAsync<List<TeamDto>>("/teams");
        Assert.Contains(all, t => t.Id == created.Id);
    }

    [PostgresFact]
    public async Task Alta_RechazaNombreDuplicado()
    {
        await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));

        var response = await Client.PostAsJsonAsync("/teams", new CreateTeamDto("Pumas"), JsonOptions);

        // El PRD pide 400 para nombre duplicado en /teams.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, await QueryAsync(db => db.Teams.CountAsync(t => t.Name == "Pumas")));
    }

    [PostgresFact]
    public async Task Alta_RechazaNombreVacio()
    {
        var response = await Client.PostAsJsonAsync("/teams", new CreateTeamDto(""), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await QueryAsync(db => db.Teams.CountAsync()));
    }

    // ---------------- CAMBIO ----------------

    [PostgresFact]
    public async Task Cambio_ActualizaElNombreYQuedaGuardado()
    {
        var created = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));

        var response = await Client.PutAsJsonAsync($"/teams/{created.Id}", new UpdateTeamDto("Pumas UNAM"), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TeamDto>(JsonOptions))!;
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Pumas UNAM", updated.Name);

        // Se releé de Postgres para confirmar que el cambio se persistio.
        var loaded = await GetAsync<TeamDto>($"/teams/{created.Id}");
        Assert.Equal("Pumas UNAM", loaded.Name);
    }

    [PostgresFact]
    public async Task Cambio_DejaGuardarElMismoNombreOtraVez()
    {
        var created = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));

        var response = await Client.PutAsJsonAsync($"/teams/{created.Id}", new UpdateTeamDto("Pumas"), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [PostgresFact]
    public async Task Cambio_RechazaElNombreDeOtroEquipo()
    {
        await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));
        var chivas = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Chivas"));

        var response = await Client.PutAsJsonAsync($"/teams/{chivas.Id}", new UpdateTeamDto("Pumas"), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Chivas", (await GetAsync<TeamDto>($"/teams/{chivas.Id}")).Name);
    }

    [PostgresFact]
    public async Task Cambio_Regresa404SiElEquipoNoExiste()
    {
        var response = await Client.PutAsJsonAsync("/teams/no-existe", new UpdateTeamDto("X"), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- BAJA ----------------

    [PostgresFact]
    public async Task Baja_BorraElEquipoDePostgres()
    {
        var created = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));

        var response = await Client.DeleteAsync($"/teams/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await QueryAsync(db => db.Teams.AnyAsync(t => t.Id == created.Id)));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfGetAsync($"/teams/{created.Id}"));
    }

    [PostgresFact]
    public async Task Baja_Regresa404SiElEquipoNoExiste()
    {
        var response = await Client.DeleteAsync("/teams/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- LECTURA ----------------

    [PostgresFact]
    public async Task Lectura_ListaVaciaCuandoNoHayEquipos()
    {
        var all = await GetAsync<List<TeamDto>>("/teams");

        Assert.Empty(all);
    }

    [PostgresFact]
    public async Task Lectura_Regresa400SiElIdTieneFormatoInvalido()
    {
        Assert.Equal(HttpStatusCode.BadRequest, await StatusOfGetAsync("/teams/id_invalido"));
    }

    [PostgresFact]
    public async Task Lectura_Regresa404SiElIdEsValidoPeroNoExiste()
    {
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfGetAsync("/teams/no-existe"));
    }
}
