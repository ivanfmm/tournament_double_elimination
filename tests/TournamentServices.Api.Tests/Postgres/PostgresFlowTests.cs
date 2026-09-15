using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.Domain.Enums;

namespace TournamentServices.Api.Tests.Postgres;

// Prueba de punta a punta contra un Postgres real: un solo escenario que
// recorre equipos -> torneo -> grupos -> asignacion -> borrado en cascada.
// Las pruebas ABC entidad por entidad viven en Postgres*CrudTests.cs.
public class PostgresFlowTests : PostgresTestBase
{
    [PostgresFact]
    public async Task FlujoCompleto_EquiposTorneoGruposYBorrado()
    {
        // 1. Crear dos equipos
        var pumas = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Pumas"));
        var chivas = await PostAsync<TeamDto>("/teams", new CreateTeamDto("Chivas"));

        // 2. Nombre duplicado -> 400 (lo valida el delegate contra Postgres)
        var duplicated = await Client.PostAsJsonAsync("/teams", new CreateTeamDto("Pumas"), JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, duplicated.StatusCode);

        // 3. Crear torneo
        var tournament = await PostAsync<TournamentDto>("/tournaments",
            new CreateTournamentDto("Copa Postgres", new FormatInputDto(2, 2, TournamentType.RoundRobin)));

        // 4. Crear grupo y repetir nombre sin importar mayusculas -> 422
        var groupsUrl = $"/tournaments/{tournament.Id}/groups";
        var group = await PostAsync<GroupDto>(groupsUrl, new CreateGroupDto("Grupo A"));
        var repeated = await Client.PostAsJsonAsync(groupsUrl, new CreateGroupDto("grupo a"), JsonOptions);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, repeated.StatusCode);

        // 5. Asignar equipos (la lista TeamIds se guarda como JSON en Postgres)
        var assign = await Client.PatchAsJsonAsync($"{groupsUrl}/{group.Id}/teams",
            new AssignTeamsDto(new List<string> { pumas.Id, chivas.Id }), JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        // 6. PATCH parcial del torneo
        var patch = await Client.PatchAsJsonAsync($"/tournaments/{tournament.Id}",
            new PatchTournamentDto("Copa Final", null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        // 7. Leer el torneo completo: grupo con sus 2 equipos y formato intacto
        var loaded = await GetAsync<TournamentDto>($"/tournaments/{tournament.Id}");
        Assert.Equal("Copa Final", loaded.Name);
        Assert.Equal(new TournamentFormatDto(2, 2, TournamentType.RoundRobin), loaded.Format);
        var loadedGroup = Assert.Single(loaded.Groups);
        Assert.Equal(new[] { "Chivas", "Pumas" }, loadedGroup.Teams.Select(t => t.Name).OrderBy(n => n));

        // 8. Borrar el torneo borra sus grupos en cascada
        var delete = await Client.DeleteAsync($"/tournaments/{tournament.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(await QueryAsync(db => db.Groups.AnyAsync(g => g.Id == group.Id)));

        // Los equipos no pertenecen al torneo, siguen existiendo
        Assert.True(await QueryAsync(db => db.Teams.AnyAsync(t => t.Id == pumas.Id)));
    }
}
