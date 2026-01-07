using System.ComponentModel;
using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// MCP resources for world extensions (singletons).
/// </summary>
[McpServerResourceType]
public sealed class ExtensionResources(BridgeConnectionManager connection)
{
    // S1075: MCP protocol requires these URIs for resource identification
#pragma warning disable S1075
    private const string ExtensionUriTemplate = "keeneyes://extension/{typeName}";
#pragma warning restore S1075

    [McpServerResource(
        UriTemplate = ExtensionUriTemplate,
        Name = "extension",
        Title = "World Extension",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetExtension(
        [Description("Extension type name")] string typeName)
    {
        var bridge = connection.GetBridge();
        var extension = await bridge.State.GetExtensionAsync(typeName);
        var uri = $"keeneyes://extension/{typeName}";

        if (extension == null)
        {
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new { error = $"Extension '{typeName}' not found" }, JsonOptions.Default)
            };
        }

        var json = JsonSerializer.Serialize(extension, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }
}
