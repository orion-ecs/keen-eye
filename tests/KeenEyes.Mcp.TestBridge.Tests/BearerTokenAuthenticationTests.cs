using System.Threading.Tasks;
using KeenEyes.Mcp.TestBridge;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace KeenEyes.Mcp.TestBridge.Tests;

/// <summary>
/// Tests for the HTTP bearer-token authentication used by the remote MCP transport.
/// </summary>
public sealed class BearerTokenAuthenticationTests
{
    private const string Token = "correct-horse-battery-staple";

    #region BearerTokenValidator Tests

    [Fact]
    public void IsAuthorized_WithCorrectToken_ReturnsTrue()
    {
        Assert.True(BearerTokenValidator.IsAuthorized($"Bearer {Token}", Token));
    }

    [Fact]
    public void IsAuthorized_WithMissingHeader_ReturnsFalse()
    {
        Assert.False(BearerTokenValidator.IsAuthorized(null, Token));
    }

    [Fact]
    public void IsAuthorized_WithWrongToken_ReturnsFalse()
    {
        Assert.False(BearerTokenValidator.IsAuthorized("Bearer wrong-token", Token));
    }

    [Fact]
    public void IsAuthorized_WithoutBearerPrefix_ReturnsFalse()
    {
        Assert.False(BearerTokenValidator.IsAuthorized(Token, Token));
    }

    [Fact]
    public void IsAuthorized_WithEmptyBearerValue_ReturnsFalse()
    {
        Assert.False(BearerTokenValidator.IsAuthorized("Bearer ", Token));
    }

    #endregion

    #region Middleware Tests

    [Fact]
    public async Task InvokeAsync_WithMissingToken_Returns401AndDoesNotCallNext()
    {
        var (context, nextCalled) = await RunMiddlewareAsync(authorizationHeader: null);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled());
    }

    [Fact]
    public async Task InvokeAsync_WithWrongToken_Returns401AndDoesNotCallNext()
    {
        var (context, nextCalled) = await RunMiddlewareAsync(authorizationHeader: "Bearer not-the-token");

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled());
    }

    [Fact]
    public async Task InvokeAsync_WithCorrectToken_CallsNext()
    {
        var (context, nextCalled) = await RunMiddlewareAsync(authorizationHeader: $"Bearer {Token}");

        Assert.True(nextCalled());
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    #endregion

    private static async Task<(HttpContext Context, Func<bool> NextCalled)> RunMiddlewareAsync(string? authorizationHeader)
    {
        var wasCalled = false;
        RequestDelegate next = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new BearerTokenAuthenticationMiddleware(next, Token);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (authorizationHeader is not null)
        {
            context.Request.Headers.Authorization = authorizationHeader;
        }

        await middleware.InvokeAsync(context);

        return (context, () => wasCalled);
    }
}
