namespace TournamentServices.Domain.Exceptions;

// Excepcion compartida para "recurso no encontrado" (404 en la capa Api).
// Se separa de DomainValidationException porque esa ultima significa
// "violacion de regla de negocio" y el middleware la traduce a 422.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}