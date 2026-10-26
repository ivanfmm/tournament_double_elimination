using System.Net;

namespace TournamentServices.Api.Tests.Routes;

// Actividad 3 (Jenkins): pruebas de los endpoints de salud que usa Kubernetes.
public class HealthRoutesTests : TournamentApiTests
{
    [Fact]
    public async Task Health_Regresa200()
    {
        var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ConBaseDisponible_Regresa200()
    {
        var response = await Client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
