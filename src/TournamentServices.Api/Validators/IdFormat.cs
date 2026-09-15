using System.Text.RegularExpressions;

namespace TournamentServices.Api.Validators;

// PRD: los ids deben cumplir [A-Za-z0-9\-]+ y si no, responder 400.
// No se usa la restriccion regex de la ruta porque esa devuelve 404.
public static class IdFormat
{
    private static readonly Regex Pattern = new("^[A-Za-z0-9-]+$", RegexOptions.Compiled);

    public static bool IsValid(string? id)
    {
        return !string.IsNullOrEmpty(id) && Pattern.IsMatch(id);
    }

    // Revisa varios ids a la vez. Regresa null si todos son validos,
    // o un 400 con ProblemDetails si alguno no lo es.
    public static IResult? Validate(params (string Field, string Value)[] ids)
    {
        var errors = ids
            .Where(id => !IsValid(id.Value))
            .ToDictionary(id => id.Field, id => new[] { $"El id '{id.Value}' no tiene un formato valido." });

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }
}
