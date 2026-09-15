using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.TestBuilders;

namespace TournamentServices.Api.Tests.Routes;

public class GroupRoutesTests : TournamentApiTests
{
    private const string BaseUrl = "/tournaments/t-1/groups";

    private Task SeedTournamentAsync(int maxTeamsPerGroup = 4, int numberOfGroups = 2)
    {
        var format = new TournamentFormat(maxTeamsPerGroup, numberOfGroups, TournamentType.RoundRobin);
        return SeedAsync(new TournamentBuilder().WithId("t-1").WithFormat(format).Build());
    }

    // ---------- GET lista ----------

    [Fact]
    public async Task GetAll_RegresaArregloVacio()
    {
        await SeedTournamentAsync();

        var response = await Client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var groups = await response.Content.ReadFromJsonAsync<List<GroupDto>>();
        Assert.Empty(groups!);
    }

    [Fact]
    public async Task GetAll_RegresaGruposDelTorneo()
    {
        await SeedTournamentAsync();
        await SeedAsync(
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("A").Build(),
            new GroupBuilder().WithId("g-2").WithTournamentId("t-1").WithName("B").Build());

        var response = await Client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var groups = await response.Content.ReadFromJsonAsync<List<GroupDto>>();
        Assert.Equal(2, groups!.Count);
    }

    [Fact]
    public async Task GetAll_Regresa404SiNoExisteElTorneo()
    {
        var response = await Client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET por id ----------

    [Fact]
    public async Task GetById_RegresaGrupoConEquipos()
    {
        await SeedTournamentAsync();
        await SeedAsync(
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("A").WithTeam("team-1").Build());

        var response = await Client.GetAsync($"{BaseUrl}/g-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.Equal("g-1", group!.Id);
        Assert.Equal("t-1", group.TournamentId);
        var team = Assert.Single(group.Teams);
        Assert.Equal(new TeamDto("team-1", "Pumas"), team);
    }

    [Fact]
    public async Task GetById_Regresa404SiNoExisteElGrupo()
    {
        await SeedTournamentAsync();

        var response = await Client.GetAsync($"{BaseUrl}/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Regresa404SiNoExisteElTorneo()
    {
        var response = await Client.GetAsync("/tournaments/no-existe/groups/g-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Regresa400SiElIdTieneFormatoInvalido()
    {
        var response = await Client.GetAsync($"{BaseUrl}/id_invalido");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST ----------

    [Fact]
    public async Task Post_Regresa201ConLocation()
    {
        await SeedTournamentAsync();

        var response = await Client.PostAsJsonAsync(BaseUrl, new CreateGroupDto("Grupo A"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.Equal("Grupo A", group!.Name);
        Assert.Empty(group.Teams);
        Assert.Equal($"{BaseUrl}/{group.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_Regresa404SiNoExisteElTorneo()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new CreateGroupDto("Grupo A"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Regresa422SiElNombreSeRepiteEnElTorneo()
    {
        await SeedTournamentAsync();
        await SeedAsync(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Grupo A").Build());

        var response = await Client.PostAsJsonAsync(BaseUrl, new CreateGroupDto("Grupo A"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_Regresa400SiElNombreEstaVacio()
    {
        await SeedTournamentAsync();

        var response = await Client.PostAsJsonAsync(BaseUrl, new CreateGroupDto(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT ----------

    [Fact]
    public async Task Put_Regresa200()
    {
        await SeedTournamentAsync();
        await SeedAsync(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("Viejo").Build());

        var response = await Client.PutAsJsonAsync($"{BaseUrl}/g-1", new UpdateGroupDto("Nuevo"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.Equal("Nuevo", group!.Name);
    }

    [Fact]
    public async Task Put_Regresa404SiNoExiste()
    {
        await SeedTournamentAsync();

        var response = await Client.PutAsJsonAsync($"{BaseUrl}/no-existe", new UpdateGroupDto("Nuevo"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- DELETE ----------

    [Fact]
    public async Task Delete_Regresa204YDesasignaEquipos()
    {
        await SeedTournamentAsync();
        await SeedAsync(
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithTeam("team-1").Build());

        var response = await Client.DeleteAsync($"{BaseUrl}/g-1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var groupExists = await QueryAsync(db => db.Groups.AnyAsync(g => g.Id == "g-1"));
        var teamExists = await QueryAsync(db => db.Teams.AnyAsync(t => t.Id == "team-1"));
        Assert.False(groupExists);
        Assert.True(teamExists); // el equipo sigue existiendo, solo sin grupo
    }

    [Fact]
    public async Task Delete_Regresa404SiNoExiste()
    {
        await SeedTournamentAsync();

        var response = await Client.DeleteAsync($"{BaseUrl}/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PATCH teams ----------

    [Fact]
    public async Task AssignTeams_Regresa204YGuardaLosEquipos()
    {
        await SeedTournamentAsync();
        await SeedAsync(
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new TeamBuilder().WithId("team-2").WithName("Chivas").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build());

        var response = await Client.PatchAsJsonAsync($"{BaseUrl}/g-1/teams",
            new AssignTeamsDto(new List<string> { "team-1", "team-2" }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var group = await (await Client.GetAsync($"{BaseUrl}/g-1")).Content.ReadFromJsonAsync<GroupDto>();
        Assert.Equal(2, group!.Teams.Count);
    }

    [Fact]
    public async Task AssignTeams_Regresa422SiElEquipoNoExiste()
    {
        await SeedTournamentAsync();
        await SeedAsync(new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build());

        var response = await Client.PatchAsJsonAsync($"{BaseUrl}/g-1/teams",
            new AssignTeamsDto(new List<string> { "fantasma" }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_Regresa422SiElEquipoYaEstaEnOtroGrupo()
    {
        await SeedTournamentAsync();
        await SeedAsync(
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithName("A").Build(),
            new GroupBuilder().WithId("g-2").WithTournamentId("t-1").WithName("B").WithTeam("team-1").Build());

        var response = await Client.PatchAsJsonAsync($"{BaseUrl}/g-1/teams",
            new AssignTeamsDto(new List<string> { "team-1" }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_Regresa422SiSeExcedeMaxTeamsPerGroup()
    {
        await SeedTournamentAsync(maxTeamsPerGroup: 1);
        await SeedAsync(
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new TeamBuilder().WithId("team-2").WithName("Chivas").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build());

        var response = await Client.PatchAsJsonAsync($"{BaseUrl}/g-1/teams",
            new AssignTeamsDto(new List<string> { "team-1", "team-2" }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_Regresa404SiNoExisteElGrupo()
    {
        await SeedTournamentAsync();
        await SeedAsync(new TeamBuilder().WithId("team-1").WithName("Pumas").Build());

        var response = await Client.PatchAsJsonAsync($"{BaseUrl}/no-existe/teams",
            new AssignTeamsDto(new List<string> { "team-1" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_Regresa404SiNoExisteElTorneo()
    {
        var response = await Client.PatchAsJsonAsync("/tournaments/no-existe/groups/g-1/teams",
            new AssignTeamsDto(new List<string> { "team-1" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
