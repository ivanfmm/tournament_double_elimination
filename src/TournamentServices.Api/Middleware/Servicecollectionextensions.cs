using TournamentServices.Delegates;
using TournamentServices.Domain.Brackets;
using TournamentServices.Repositories;
using Microsoft.EntityFrameworkCore;

namespace TournamentServices.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TournamentDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();

        services.AddScoped<ITeamDelegate, TeamDelegate>();
        services.AddScoped<ITournamentDelegate, TournamentDelegate>();
        services.AddScoped<IGroupDelegate, GroupDelegate>();
        services.AddScoped<IMatchDelegate, MatchDelegate>();
        services.AddScoped<IBracketDelegate, BracketDelegate>();

        // Scoped y no Singleton: Random no es seguro entre hilos
        services.AddScoped<ITeamSeeder>(_ => new RandomTeamSeeder());

        return services;
    }
}