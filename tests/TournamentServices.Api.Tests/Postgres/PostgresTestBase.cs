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
//   bash:        export TEST_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=password"
//   PowerShell:  $env:TEST_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=password"
//
// Ojo: TEST_POSTGRES NO es lo mismo que ConnectionStrings__Default (la que
// usa la API). Esta apunta al SERVIDOR, no a una base: el nombre de la base
// lo pone ConfigureDatabase abajo, una distinta por cada test.
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
    // Toda base creada por estas pruebas empieza con este prefijo, y es
    // lo unico que Dispose tiene permitido borrar. Ver EnsureSafeToDelete.
    private const string TestDatabasePrefix = "tournament_test_";

    protected override void ConfigureDatabase(DbContextOptionsBuilder options, string databaseName)
    {
        // Se toma el servidor de TEST_POSTGRES pero se IGNORA cualquier
        // Database= que traiga: cada test manda sobre su propia base.
        var connection = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(PostgresFactAttribute.VariableName))
        {
            Database = $"{TestDatabasePrefix}{databaseName.Replace("-", "")[..12]}"
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
            var db = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();

            EnsureSafeToDelete(db);
            db.Database.EnsureDeleted();
        }

        base.Dispose();
    }

    // RED DE SEGURIDAD. EnsureDeleted() borra la base COMPLETA, sin
    // preguntar y sin vuelta atras. Aqui no se confia en lo que
    // ConfigureDatabase *intento* configurar: se lee el nombre real de la
    // base a la que el contexto va a mandar el DROP, y se compara contra
    // el prefijo. Si alguien rompe ConfigureDatabase, o apunta las pruebas
    // a la base de desarrollo, esto truena ANTES del borrado en vez de
    // dejarte sin datos.
    private static void EnsureSafeToDelete(TournamentDbContext db)
    {
        var target = db.Database.GetDbConnection().Database;

        if (!target.StartsWith(TestDatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ABORTADO: las pruebas intentaron borrar la base '{target}', que no es una base de prueba. " +
                $"Solo se pueden borrar bases cuyo nombre empiece con '{TestDatabasePrefix}'. " +
                $"Revisa {nameof(PostgresTestBase)}.{nameof(ConfigureDatabase)} y la variable " +
                $"{PostgresFactAttribute.VariableName}.");
        }
    }
}
