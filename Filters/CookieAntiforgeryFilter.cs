using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiCookieHttpOnly.Filters;

/// <summary>
/// Valida o token antiforgery em operações semânticas quando a chamada usa cookies.
/// Clientes que enviam explicitamente um token Bearer não dependem de cookies e não
/// precisam enviar o token antiforgery.
/// </summary>
public sealed class CookieAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (SafeMethods.Contains(request.Method)
            || request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult("Invalid antiforgery token");
        }
    }
}
