using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TournamentServices.Repositories;

namespace TournamentServices.Api.Tests.Postgres;

// Marca un test que solo corre si existe la variable de entorno
// TEST_POSTGRES con una connection string. Si no existe, el test
// aparece como "Skipped" en vez de fallar.
//
//   PowerShell:  $env:TEST_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=password"
//   bash:        export TEST_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=password"
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string VariableName = "TEST_POSTGRES";

    public static bool IsEnabled =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableName));

    public PostgresFactAttribute()
    {
        if (!IsEnabled)
        {
            Skip = $"Define {VariableName} para correr las pruebas contra Postgres.";
        }
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (!PostgresFactAttribute.IsEnabled)
        {
            Skip = $"Define {PostgresFactAttribute.VariableName} para correr las pruebas contra Postgres.";
        }
    }
}

// Base comun de todas las pruebas contra un Postgres real.
//
// Cada test (xUnit crea una instancia por test) levanta la API completa
// apuntando a SU PROPIA base tournament_test_xxxxxxxxxxxx, que se crea
// sola con EnsureCreated() al arrancar Program.cs y se borra en Dispose.
// Asi los tests son aislados, corren en paralelo y no ensucian la base
// "tournament" que usa la API de desarrollo.
public abstract class PostgresTestBase : TournamentApiTests
{
    protected override void ConfigureDatabase(DbContextOptionsBuilder options, string databaseName)
    {
        var connection = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(PostgresFactAttribute.VariableName))
        {
            Database = $"tournament_test_{databaseName.Replace("-", "")[..12]}"
        };

        options.UseNpgsql(connection.ConnectionString);
    }

    // ---------- helpers compartidos por las pruebas ABC ----------

    // POST que exige 201 y devuelve el cuerpo ya deserializado.
    protected async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await Client.PostAsJsonAsync(url, body, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    // GET que exige 200 y devuelve el cuerpo ya deserializado.
    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    protected async Task<HttpStatusCode> StatusOfGetAsync(string url)
    {
        var response = await Client.GetAsync(url);
        return response.StatusCode;
    }

    public override void Dispose()
    {
        // Si el test se salto no se levanto ninguna base que borrar.
        if (PostgresFactAttribute.IsEnabled)
        {
            using var scope = Factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TournamentDbContext>().Database.EnsureDeleted();
        }

        base.Dispose();
    }
}
