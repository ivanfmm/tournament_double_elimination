using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain.Exceptions;
namespace TournamentServices.Api.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, detail) = exception switch
        {
            DuplicateNameException e         => (StatusCodes.Status400BadRequest, e.Message),
            DomainValidationException e      => (StatusCodes.Status400BadRequest, e.Message),
            NotFoundException e              => (StatusCodes.Status404NotFound, e.Message),
            BusinessRuleException e          => (StatusCodes.Status422UnprocessableEntity, e.Message),
            BadHttpRequestException e        => (e.StatusCode, "La petición no es válida."),
            JsonException                    => (StatusCodes.Status400BadRequest, "El cuerpo de la petición no es válido."),
            // Choque con un indice unico (ej. dos peticiones al mismo tiempo sobre el bracket)
            DbUpdateException                => (StatusCodes.Status409Conflict, "Otra operación modificó estos datos al mismo tiempo. Intenta de nuevo."),
            _                                => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}