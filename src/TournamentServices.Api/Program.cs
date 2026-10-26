using TournamentServices.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using TournamentServices.Api.Extensions;
using TournamentServices.Api.Handlers;
using TournamentServices.Api.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTournamentServices(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// camelCase por defecto; enums como string: RoundRobin -> "ROUND_ROBIN", Home -> "HOME"
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

var app = builder.Build();
   app.MapGet("/health/ready", async (TournamentDbContext db) =>
       await db.Database.CanConnectAsync()
           ? Results.Ok("Ready")
           : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.UseExceptionHandler();

app.MapGet("/health", () => "Services running");
app.MapTeamRoutes();
app.MapTournamentRoutes();
app.MapGroupRoutes();
app.MapMatchRoutes();
app.MapBracketRoutes();

app.Run();

// Necesario para pruebas de integración con WebApplicationFactory<Program>
public partial class Program { }
