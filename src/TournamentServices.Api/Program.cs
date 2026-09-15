using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Middleware;
using TournamentServices.Api.Routes;
using TournamentServices.Api.Validators;
using TournamentServices.Delegates;
using TournamentServices.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// PRD: camelCase (default de Minimal APIs) y enums como string.
// SnakeCaseUpper convierte RoundRobin -> ROUND_ROBIN, Home -> HOME.
// allowIntegerValues: false para que "type": 1 sea un 400.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
});

// Mismo nombre que usa docker-compose: ConnectionStrings__Default.
// Si no hay connection string, se usa la base en memoria (util para
// arrancar la API sin Postgres levantado).
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<TournamentDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("TournamentDb");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// Repositories
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();

// Delegates
builder.Services.AddScoped<ITeamDelegate, TeamDelegate>();
builder.Services.AddScoped<IGroupDelegate, GroupDelegate>();
builder.Services.AddScoped<IMatchDelegate, MatchDelegate>();
builder.Services.AddScoped<ITournamentDelegate, TournamentDelegate>();

// Validators (registro manual para no agregar otro paquete)
builder.Services.AddScoped<IValidator<CreateTeamDto>, CreateTeamDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamDto>, UpdateTeamDtoValidator>();
builder.Services.AddScoped<IValidator<CreateGroupDto>, CreateGroupDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateGroupDto>, UpdateGroupDtoValidator>();
builder.Services.AddScoped<IValidator<AssignTeamsDto>, AssignTeamsDtoValidator>();
builder.Services.AddScoped<IValidator<CreateTournamentDto>, CreateTournamentDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTournamentDto>, UpdateTournamentDtoValidator>();
builder.Services.AddScoped<IValidator<PatchTournamentDto>, PatchTournamentDtoValidator>();

var app = builder.Build();

// Crea la base y las tablas si no existen (sin migraciones, por ahora).
// OJO: EnsureCreated NO actualiza tablas que ya existen. Si cambia el
// modelo hay que borrar la base (o pasar a migraciones de EF).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
    db.Database.EnsureCreated();
}

// Va primero para atrapar las excepciones de todo lo que sigue.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok("Services running"));

app.MapTeamRoutes();
app.MapTournamentRoutes();
app.MapGroupRoutes();

app.Run();

// Necesario para WebApplicationFactory<Program> en los tests de integracion.
public partial class Program { }
