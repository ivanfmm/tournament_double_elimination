using Microsoft.AspNetCore.Mvc;
using TournamentServices.Domain.Exceptions;

namespace TournamentServices.Api.Middleware;

// Version minima (Persona D la puede extender).
// Traduce excepciones a ProblemDetails con el status del PRD:
//   NotFoundException          -> 404
//   DomainValidationException  -> 422
//   BadHttpRequestException    -> 400 (JSON mal formado, body faltante)
//   cualquier otra             -> 500
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (DomainValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity, "Business Rule Violation", ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteProblemAsync(context, ex.StatusCode, "Bad Request", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "Ocurrio un error inesperado.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
