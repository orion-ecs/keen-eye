using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Capture;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// MCP resources for capture and screenshot access.
/// </summary>
/// <param name="connection">The connection manager used to reach the active test bridge.</param>
[McpServerResourceType]
public sealed class CaptureResources(BridgeConnectionManager connection)
{
    // S1075: MCP protocol requires these URIs for resource identification
#pragma warning disable S1075
    private const string ScreenshotUri = "keeneyes://capture/screenshot";
#pragma warning restore S1075

    /// <summary>
    /// Captures the current screenshot and returns it as a PNG blob resource.
    /// </summary>
    /// <returns>A <see cref="BlobResourceContents"/> containing the raw PNG screenshot bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when screenshot capture is not available on the connected bridge.</exception>
    [McpServerResource(
        UriTemplate = ScreenshotUri,
        Name = "screenshot",
        Title = "Screenshot",
        MimeType = "image/png")]
    public async Task<BlobResourceContents> GetScreenshot()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Capture.IsAvailable)
        {
            throw new InvalidOperationException("Capture is not available");
        }

        var bytes = await bridge.Capture.GetScreenshotBytesAsync(ImageFormat.Png);
        return BlobResourceContents.FromBytes(bytes, ScreenshotUri, "image/png");
    }
}
