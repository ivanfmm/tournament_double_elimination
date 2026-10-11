using System.Text.RegularExpressions;
namespace TournamentServices.Api.Filters;
public class ValidateIdFilter : IEndpointFilter
{
    private static readonly Regex ValidId = new(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var (key, value) in context.HttpContext.Request.RouteValues)
        {
            if (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                && value is string id
                && !ValidId.IsMatch(id))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: $"El parámetro '{key}' tiene un formato inválido.");
            }
        }

        return await next(context);
    }
}