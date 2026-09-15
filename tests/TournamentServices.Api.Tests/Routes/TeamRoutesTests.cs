using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.TestBuilders;

namespace TournamentServices.Api.Tests.Routes;

public class TeamRoutesTests : TournamentApiTests
{
    [Fact]
    public async Task GetAll_RegresaArregloVacio()
    {
        var response = await Client.GetAsync("/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        Assert.Empty(teams!);
    }

    [Fact]
    public async Task GetAll_RegresaEquipos()
    {
        await SeedAsync(
            new TeamBuilder().WithId("team-1").WithName("Pumas").Build(),
            new TeamBuilder().WithId("team-2").WithName("Chivas").Build());

        var response = await Client.GetAsync("/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        Assert.Equal(2, teams!.Count);
    }

    [Fact]
    public async Task GetById_RegresaEquipo()
    {
        await SeedAsync(new TeamBuilder().WithId("team-1").WithName("Pumas").Build());

        var response = await Client.GetAsync("/teams/team-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        Assert.Equal(new TeamDto("team-1", "Pumas"), team);
    }

    [Fact]
    public async Task GetById_Regresa404SiNoExiste()
    {
        var response = await Client.GetAsync("/teams/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Regresa400SiElIdTieneFormatoInvalido()
    {
        var response = await Client.GetAsync("/teams/id_invalido");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Regresa201ConLocation()
    {
        var response = await Client.PostAsJsonAsync("/teams", new CreateTeamDto("Pumas"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        Assert.Equal("Pumas", team!.Name);
        Assert.Equal($"/teams/{team.Id}", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":\"\"}")]
    [InlineData("{\"name\":\"   \"}")]
    [InlineData("{\"name\":")]
    public async Task Post_Regresa400SiElNombreFaltaEstaVacioOJsonMalFormado(string body)
    {
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/teams", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Regresa400SiElNombreYaExiste()
    {
        await SeedAsync(new TeamBuilder().WithId("team-1").WithName("Pumas").Build());

        var response = await Client.PostAsJsonAsync("/teams", new CreateTeamDto("Pumas"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Regresa200ConEquipoActualizado()
    {
        await SeedAsync(new TeamBuilder().WithId("team-1").WithName("Viejo").Build());

        var response = await Client.PutAsJsonAsync("/teams/team-1", new UpdateTeamDto("Nuevo"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        Assert.Equal(new TeamDto("team-1", "Nuevo"), team);
    }

    [Fact]
    public async Task Put_Regresa404SiNoExiste()
    {
        var response = await Client.PutAsJsonAsync("/teams/no-existe", new UpdateTeamDto("Nuevo"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Regresa204()
    {
        await SeedAsync(new TeamBuilder().WithId("team-1").WithName("Pumas").Build());

        var response = await Client.DeleteAsync("/teams/team-1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var exists = await QueryAsync(db => db.Teams.AnyAsync(t => t.Id == "team-1"));
        Assert.False(exists);
    }

    [Fact]
    public async Task Delete_Regresa404SiNoExiste()
    {
        var response = await Client.DeleteAsync("/teams/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Errores_UsanFormatoProblemDetails()
    {
        var response = await Client.GetAsync("/teams/no-existe");

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }
}
