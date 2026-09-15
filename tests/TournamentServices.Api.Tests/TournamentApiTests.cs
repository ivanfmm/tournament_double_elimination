using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TournamentServices.Repositories;

namespace TournamentServices.Api.Tests;

// Clase base para los *RoutesTests. Cada test (xUnit crea una instancia
// por test) levanta su propia API con su propia base, asi los tests son
// aislados y pueden correr en paralelo.
public abstract class TournamentApiTests : IDisposable
{
    // Mismas reglas de JSON que la API (enums como ROUND_ROBIN, HOME...).
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected TournamentApiTests()
    {
        var databaseName = Guid.NewGuid().ToString();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Se quita el registro de Program.cs y se registra otro.
                // En EF Core 9+ tambien hay que quitar IDbContextOptionsConfiguration.
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<TournamentDbContext>)
                             || d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                    .ToList();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TournamentDbContext>(options => ConfigureDatabase(options, databaseName));
            });
        });

        Client = Factory.CreateClient();
    }

    // Por default: InMemory con nombre unico. Las pruebas de Postgres lo sobreescriben.
    // Cada test crea su propia API y EF avisa (como error) cuando se crean mas de
    // 20 service providers internos. En tests es esperado, por eso se ignora.
    protected virtual void ConfigureDatabase(DbContextOptionsBuilder options, string databaseName)
    {
        options
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }

    // Inserta datos directo en la base, sin pasar por la API.
    protected async Task SeedAsync(params object[] entities)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    // Lee datos directo de la base para verificar efectos.
    protected async Task<T> QueryAsync<T>(Func<TournamentDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
        return await query(db);
    }

    public virtual void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
