using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.Domain;
using TournamentServices.Domain.Enums;
using TournamentServices.TestBuilders;

namespace TournamentServices.Api.Tests.Routes;

public class TournamentRoutesTests : TournamentApiTests
{
    private static CreateTournamentDto NewTournament(string name = "Copa", TournamentType type = TournamentType.RoundRobin)
    {
        return new CreateTournamentDto(name, new FormatInputDto(4, 2, type));
    }

    private Task<HttpResponseMessage> PostRawAsync(string url, string json)
    {
        return Client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private Task<HttpResponseMessage> PatchRawAsync(string url, string json)
    {
        return Client.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    // ---------- GET lista ----------

    [Fact]
    public async Task GetAll_RegresaArregloVacio()
    {
        var response = await Client.GetAsync("/tournaments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TournamentDto>>(JsonOptions);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task GetAll_RegresaTorneos()
    {
        await SeedAsync(
            new TournamentBuilder().WithId("t-1").WithName("Copa A").Build(),
            new TournamentBuilder().WithId("t-2").WithName("Copa B").Build());

        var response = await Client.GetAsync("/tournaments");

        var list = await response.Content.ReadFromJsonAsync<List<TournamentDto>>(JsonOptions);
        Assert.Equal(2, list!.Count);
    }

    // ---------- GET por id ----------

    [Fact]
    public async Task GetById_RegresaTorneoConGruposYPartidos()
    {
        await SeedAsync(
            new TournamentBuilder().WithId("t-1").WithName("Copa").Build(),
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new TeamBuilder().WithId("team-2").WithName("Chivas").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").WithTeam("team-1").WithTeam("team-2").Build(),
            new MatchBuilder().WithId("m-1").WithTournamentId("t-1").WithGroupId("g-1")
                .WithHomeTeam("team-1").WithVisitorTeam("team-2").WithScore(2, 1).Build());

        var response = await Client.GetAsync("/tournaments/t-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal("Copa", dto!.Name);
        Assert.Equal(2, Assert.Single(dto.Groups).Teams.Count);

        var match = Assert.Single(dto.Matches);
        Assert.Equal("Pumas", match.HomeTeam!.Name);
        Assert.Equal(Winner.Home, match.Winner);
        Assert.True(match.IsCompleted);
    }

    [Fact]
    public async Task GetById_SerializaEnumsComoTextoDelPrd()
    {
        await SeedAsync(
            new TournamentBuilder().WithId("t-1").Build(),
            new MatchBuilder().WithId("m-1").WithTournamentId("t-1")
                .WithHomeTeam("a").WithVisitorTeam("b").WithScore(0, 3).Build());

        var json = await Client.GetStringAsync("/tournaments/t-1");

        Assert.Contains("\"type\":\"ROUND_ROBIN\"", json);
        Assert.Contains("\"winner\":\"VISITOR\"", json);
    }

    [Fact]
    public async Task GetById_Regresa404SiNoExiste()
    {
        var response = await Client.GetAsync("/tournaments/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Regresa400SiElIdTieneFormatoInvalido()
    {
        var response = await Client.GetAsync("/tournaments/id_invalido");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST ----------

    [Theory]
    [InlineData(TournamentType.RoundRobin)]
    [InlineData(TournamentType.Nfl)]
    [InlineData(TournamentType.DoubleElimination)]
    public async Task Post_Regresa201ConLocationParaCadaTipo(TournamentType type)
    {
        var response = await Client.PostAsJsonAsync("/tournaments", NewTournament(type: type), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal(type, dto!.Format.Type);
        Assert.Empty(dto.Groups);
        Assert.Empty(dto.Matches);
        Assert.Equal($"/tournaments/{dto.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_AceptaElTipoEscritoComoEnElPrd()
    {
        var response = await PostRawAsync("/tournaments",
            """{"name":"Copa","format":{"maxTeamsPerGroup":4,"numberOfGroups":2,"type":"NFL"}}""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("""{"format":{"maxTeamsPerGroup":4,"numberOfGroups":2,"type":"NFL"}}""")]          // sin nombre
    [InlineData("""{"name":"","format":{"maxTeamsPerGroup":4,"numberOfGroups":2,"type":"NFL"}}""")] // nombre vacio
    [InlineData("""{"name":"Copa"}""")]                                                              // sin formato
    [InlineData("""{"name":"Copa","format":{"maxTeamsPerGroup":0,"numberOfGroups":2,"type":"NFL"}}""")]
    [InlineData("""{"name":"Copa","format":{"maxTeamsPerGroup":4,"numberOfGroups":-1,"type":"NFL"}}""")]
    [InlineData("""{"name":"Copa","format":{"maxTeamsPerGroup":4,"numberOfGroups":2}}""")]           // sin tipo
    [InlineData("""{"name":"Copa","format":{"maxTeamsPerGroup":4,"numberOfGroups":2,"type":"FOO"}}""")]
    [InlineData("""{"name":"Copa","format":{"maxTeamsPerGroup":4,"numberOfGroups":2,"type":1}}""")]
    public async Task Post_Regresa400SiElBodyEsInvalido(string json)
    {
        var response = await PostRawAsync("/tournaments", json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT ----------

    [Fact]
    public async Task Put_Regresa200ConTodoActualizado()
    {
        await SeedAsync(
            new TournamentBuilder().WithId("t-1").WithName("Viejo").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build());

        var body = new UpdateTournamentDto("Nuevo", new FormatInputDto(8, 4, TournamentType.Nfl));
        var response = await Client.PutAsJsonAsync("/tournaments/t-1", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal("Nuevo", dto!.Name);
        Assert.Equal(new TournamentFormatDto(8, 4, TournamentType.Nfl), dto.Format);
        Assert.Single(dto.Groups); // los grupos no se pierden al actualizar
    }

    [Fact]
    public async Task Put_Regresa404SiNoExiste()
    {
        var body = new UpdateTournamentDto("Nuevo", new FormatInputDto(8, 4, TournamentType.Nfl));

        var response = await Client.PutAsJsonAsync("/tournaments/no-existe", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PATCH ----------

    [Fact]
    public async Task Patch_SoloNombre_ConservaElFormato()
    {
        await SeedAsync(new TournamentBuilder().WithId("t-1").WithName("Viejo").Build());

        var response = await PatchRawAsync("/tournaments/t-1", """{"name":"Nuevo"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal("Nuevo", dto!.Name);
        Assert.Equal(new TournamentFormatDto(4, 2, TournamentType.RoundRobin), dto.Format);
    }

    [Fact]
    public async Task Patch_SoloUnCampoDelFormato_ConservaElResto()
    {
        await SeedAsync(new TournamentBuilder().WithId("t-1").WithName("Copa").Build());

        var response = await PatchRawAsync("/tournaments/t-1", """{"format":{"type":"NFL"}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal("Copa", dto!.Name);
        Assert.Equal(new TournamentFormatDto(4, 2, TournamentType.Nfl), dto.Format);
    }

    [Fact]
    public async Task Patch_NombreYFormato()
    {
        await SeedAsync(new TournamentBuilder().WithId("t-1").WithName("Copa").Build());

        var response = await PatchRawAsync("/tournaments/t-1",
            """{"name":"Otra","format":{"maxTeamsPerGroup":6,"numberOfGroups":3}}""");

        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>(JsonOptions);
        Assert.Equal("Otra", dto!.Name);
        Assert.Equal(new TournamentFormatDto(6, 3, TournamentType.RoundRobin), dto.Format);
    }

    [Theory]
    [InlineData("""{"name":""}""")]
    [InlineData("""{"format":{"maxTeamsPerGroup":0}}""")]
    public async Task Patch_Regresa400SiUnCampoEnviadoEsInvalido(string json)
    {
        await SeedAsync(new TournamentBuilder().WithId("t-1").Build());

        var response = await PatchRawAsync("/tournaments/t-1", json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Regresa404SiNoExiste()
    {
        var response = await PatchRawAsync("/tournaments/no-existe", """{"name":"Nuevo"}""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- DELETE ----------

    [Fact]
    public async Task Delete_Regresa204YBorraEnCascada()
    {
        await SeedAsync(
            new TournamentBuilder().WithId("t-1").Build(),
            new GroupBuilder().WithId("g-1").WithTournamentId("t-1").Build(),
            new MatchBuilder().WithId("m-1").WithTournamentId("t-1").Build());

        var response = await Client.DeleteAsync("/tournaments/t-1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await QueryAsync(db => db.Tournaments.AnyAsync(t => t.Id == "t-1")));
        Assert.False(await QueryAsync(db => db.Groups.AnyAsync(g => g.TournamentId == "t-1")));
        Assert.False(await QueryAsync(db => db.Matches.AnyAsync(m => m.TournamentId == "t-1")));
    }

    [Fact]
    public async Task Delete_Regresa404SiNoExiste()
    {
        var response = await Client.DeleteAsync("/tournaments/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
