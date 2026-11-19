using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiCookieHttpOnly.Filters;

/// <summary>
/// Valida o token antiforgery em operações semânticas autenticadas por cookie.
/// </summary>
public sealed class CookieAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (SafeMethods.Contains(request.Method))
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
