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
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
});

// Base en memoria por ahora: los datos se pierden al reiniciar.
// Cuando se pase a Postgres solo cambia esta linea.
builder.Services.AddDbContext<TournamentDbContext>(options =>
    options.UseInMemoryDatabase("TournamentDb"));

// Repositories
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();

// Delegates
builder.Services.AddScoped<ITeamDelegate, TeamDelegate>();
builder.Services.AddScoped<IGroupDelegate, GroupDelegate>();

// Validators (registro manual para no agregar otro paquete)
builder.Services.AddScoped<IValidator<CreateTeamDto>, CreateTeamDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateTeamDto>, UpdateTeamDtoValidator>();
builder.Services.AddScoped<IValidator<CreateGroupDto>, CreateGroupDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateGroupDto>, UpdateGroupDtoValidator>();
builder.Services.AddScoped<IValidator<AssignTeamsDto>, AssignTeamsDtoValidator>();

var app = builder.Build();

// Va primero para atrapar las excepciones de todo lo que sigue.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok("Services running"));

app.MapTeamRoutes();
app.MapGroupRoutes();

app.Run();

// Necesario para WebApplicationFactory<Program> en los tests de integracion.
public partial class Program { }
