using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KeenEyes.Mcp.TestBridge;

/// <summary>
/// Validates the <c>Authorization: Bearer &lt;token&gt;</c> header against an expected token.
/// </summary>
internal static class BearerTokenValidator
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Determines whether an <c>Authorization</c> header value carries the expected bearer token.
    /// </summary>
    /// <param name="authorizationHeader">The raw <c>Authorization</c> header value, or <see langword="null"/> when absent.</param>
    /// <param name="expectedToken">The token the endpoint requires.</param>
    /// <returns><see langword="true"/> when the header presents the exact expected token; otherwise <see langword="false"/>.</returns>
    public static bool IsAuthorized(string? authorizationHeader, string expectedToken)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return false;
        }

        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var provided = authorizationHeader[BearerPrefix.Length..].Trim();
        return !string.IsNullOrEmpty(provided) && string.Equals(provided, expectedToken, StringComparison.Ordinal);
    }
}

/// <summary>
/// ASP.NET middleware that rejects requests lacking a valid bearer token with HTTP 401.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="expectedToken">The bearer token every request must present.</param>
internal sealed class BearerTokenAuthenticationMiddleware(RequestDelegate next, string expectedToken)
{
    /// <summary>
    /// Rejects the request with 401 when the bearer token is missing or wrong; otherwise forwards it.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!BearerTokenValidator.IsAuthorized(header, expectedToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsync("Unauthorized: a valid bearer token is required.");
            return;
        }

        await next(context);
    }
}
