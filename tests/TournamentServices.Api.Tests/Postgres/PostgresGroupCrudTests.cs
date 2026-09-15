using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Tests.Postgres;

// ABC de grupos (alta, baja, cambio) contra un Postgres real, mas la
// asignacion de equipos, que es la operacion que guarda TeamIds como
// JSON dentro de la tabla Groups.
public class PostgresGroupCrudTests : PostgresTestBase
{
    // Crea un torneo por la API y devuelve la url base de sus grupos.
    private async Task<(string TournamentId, string GroupsUrl)> NewTournamentAsync(
        int maxTeamsPerGroup = 4,
        int numberOfGroups = 2)
    {
        var tournament = await PostAsync<TournamentDto>("/tournaments",
            new CreateTournamentDto("Copa", new FormatInputDto(maxTeamsPerGroup, numberOfGroups, TournamentType.RoundRobin)));

        return (tournament.Id, $"/tournaments/{tournament.Id}/groups");
    }

    private Task<TeamDto> NewTeamAsync(string name) =>
        PostAsync<TeamDto>("/teams", new CreateTeamDto(name));

    // ---------------- ALTA ----------------

    [PostgresFact]
    public async Task Alta_CreaElGrupoYLoGuardaEnPostgres()
    {
        var (tournamentId, groupsUrl) = await NewTournamentAsync();

        var response = await Client.PostAsJsonAsync(groupsUrl, new CreateGroupDto("Grupo A"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<GroupDto>(JsonOptions))!;

        Assert.Equal("Grupo A", created.Name);
        Assert.Equal(tournamentId, created.TournamentId);
        Assert.Empty(created.Teams);
        Assert.Equal($"{groupsUrl}/{created.Id}", response.Headers.Location!.ToString());

        Assert.True(await QueryAsync(db =>
            db.Groups.AnyAsync(g => g.Id == created.Id && g.TournamentId == tournamentId)));
    }

    [PostgresFact]
    public async Task Alta_ElGrupoCreadoApareceEnLaListaDelTorneo()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var created = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var list = await GetAsync<List<GroupDto>>(groupsUrl);

        Assert.Equal(created.Id, Assert.Single(list).Id);
    }

    [PostgresFact]
    public async Task Alta_RechazaNombreRepetidoSinImportarMayusculas()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var response = await Client.PostAsJsonAsync(groupsUrl, new CreateGroupDto("grupo a"), JsonOptions);

        // Regla de negocio -> 422 (no 400).
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, await QueryAsync(db => db.Groups.CountAsync()));
    }

    [PostgresFact]
    public async Task Alta_RechazaMasGruposDeLosQuePermiteElFormato()
    {
        // numberOfGroups: 1 -> solo cabe un grupo.
        var (_, groupsUrl) = await NewTournamentAsync(numberOfGroups: 1);
        await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var response = await Client.PostAsJsonAsync(groupsUrl, new CreateGroupDto("Grupo B"), JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [PostgresFact]
    public async Task Alta_Regresa404SiElTorneoNoExiste()
    {
        var response = await Client.PostAsJsonAsync(
            "/tournaments/no-existe/groups", new CreateGroupDto("Grupo A"), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [PostgresFact]
    public async Task Alta_Regresa400SiElNombreVieneVacio()
    {
        var (_, groupsUrl) = await NewTournamentAsync();

        var response = await Client.PostAsJsonAsync(groupsUrl, new CreateGroupDto(""), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------------- CAMBIO ----------------

    [PostgresFact]
    public async Task Cambio_RenombraElGrupoYQuedaGuardado()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var created = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var response = await Client.PutAsJsonAsync($"{groupsUrl}/{created.Id}", new UpdateGroupDto("Grupo Z"), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Grupo Z", (await GetAsync<GroupDto>($"{groupsUrl}/{created.Id}")).Name);
    }

    [PostgresFact]
    public async Task Cambio_RechazaElNombreDeOtroGrupoDelMismoTorneo()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var groupB = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo B"));

        var response = await Client.PutAsJsonAsync($"{groupsUrl}/{groupB.Id}", new UpdateGroupDto("Grupo A"), JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("Grupo B", (await GetAsync<GroupDto>($"{groupsUrl}/{groupB.Id}")).Name);
    }

    [PostgresFact]
    public async Task Cambio_Regresa404SiElGrupoNoExiste()
    {
        var (_, groupsUrl) = await NewTournamentAsync();

        var response = await Client.PutAsJsonAsync($"{groupsUrl}/no-existe", new UpdateGroupDto("X"), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- BAJA ----------------

    [PostgresFact]
    public async Task Baja_BorraElGrupoDePostgres()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var created = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var response = await Client.DeleteAsync($"{groupsUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await QueryAsync(db => db.Groups.AnyAsync(g => g.Id == created.Id)));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfGetAsync($"{groupsUrl}/{created.Id}"));
    }

    [PostgresFact]
    public async Task Baja_NoBorraLosEquiposQueTeniaAsignados()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var pumas = await NewTeamAsync("Pumas");
        await AssignAsync(groupsUrl, group.Id, pumas.Id);

        await Client.DeleteAsync($"{groupsUrl}/{group.Id}");

        // El equipo existe por su cuenta: la relacion vivia en el grupo.
        Assert.True(await QueryAsync(db => db.Teams.AnyAsync(t => t.Id == pumas.Id)));
    }

    [PostgresFact]
    public async Task Baja_Regresa404SiElGrupoNoExiste()
    {
        var (_, groupsUrl) = await NewTournamentAsync();

        var response = await Client.DeleteAsync($"{groupsUrl}/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- ASIGNAR EQUIPOS ----------------

    [PostgresFact]
    public async Task AsignarEquipos_GuardaLaListaYSeLeeEnElGet()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var pumas = await NewTeamAsync("Pumas");
        var chivas = await NewTeamAsync("Chivas");

        var response = await AssignAsync(groupsUrl, group.Id, pumas.Id, chivas.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var loaded = await GetAsync<GroupDto>($"{groupsUrl}/{group.Id}");
        Assert.Equal(new[] { "Chivas", "Pumas" }, loaded.Teams.Select(t => t.Name).OrderBy(n => n));
    }

    [PostgresFact]
    public async Task AsignarEquipos_Regresa422SiElEquipoNoExiste()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var response = await AssignAsync(groupsUrl, group.Id, "team-fantasma");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Empty((await GetAsync<GroupDto>($"{groupsUrl}/{group.Id}")).Teams);
    }

    [PostgresFact]
    public async Task AsignarEquipos_Regresa422SiSeExcedeMaxTeamsPerGroup()
    {
        var (_, groupsUrl) = await NewTournamentAsync(maxTeamsPerGroup: 1);
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var pumas = await NewTeamAsync("Pumas");
        var chivas = await NewTeamAsync("Chivas");

        var response = await AssignAsync(groupsUrl, group.Id, pumas.Id, chivas.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [PostgresFact]
    public async Task AsignarEquipos_Regresa422SiElEquipoYaEstaEnOtroGrupo()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var groupA = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var groupB = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo B"));
        var pumas = await NewTeamAsync("Pumas");

        await AssignAsync(groupsUrl, groupA.Id, pumas.Id);
        var response = await AssignAsync(groupsUrl, groupB.Id, pumas.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [PostgresFact]
    public async Task AsignarEquipos_Regresa422SiLaListaTraeIdsRepetidos()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var pumas = await NewTeamAsync("Pumas");

        var response = await AssignAsync(groupsUrl, group.Id, pumas.Id, pumas.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [PostgresFact]
    public async Task AsignarEquipos_Regresa400SiLaListaVieneVacia()
    {
        var (_, groupsUrl) = await NewTournamentAsync();
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));

        var response = await AssignAsync(groupsUrl, group.Id);

        // Error de forma, no de negocio -> 400.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> AssignAsync(string groupsUrl, string groupId, params string[] teamIds)
    {
        return Client.PatchAsJsonAsync(
            $"{groupsUrl}/{groupId}/teams",
            new AssignTeamsDto(teamIds.ToList()),
            JsonOptions);
    }
}
