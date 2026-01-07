using System.ComponentModel;
using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// MCP resources for world and entity state information.
/// </summary>
[McpServerResourceType]
public sealed class WorldResources(BridgeConnectionManager connection)
{
    // S1075: MCP protocol requires these URIs for resource identification
#pragma warning disable S1075
    private const string WorldStatsUri = "keeneyes://world/stats";
    private const string EntityByIdUriTemplate = "keeneyes://entity/{id}";
    private const string EntityByNameUriTemplate = "keeneyes://entity/name/{name}";
#pragma warning restore S1075

    [McpServerResource(
        UriTemplate = WorldStatsUri,
        Name = "world-stats",
        Title = "World Statistics",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetWorldStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.State.GetWorldStatsAsync();
        var json = JsonSerializer.Serialize(stats, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = WorldStatsUri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = EntityByIdUriTemplate,
        Name = "entity-by-id",
        Title = "Entity by ID",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetEntityById(
        [Description("Entity ID")] int id)
    {
        var bridge = connection.GetBridge();
        var entity = await bridge.State.GetEntityAsync(id);
        var uri = $"keeneyes://entity/{id}";

        if (entity == null)
        {
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new { error = $"Entity {id} not found" }, JsonOptions.Default)
            };
        }

        var json = JsonSerializer.Serialize(entity, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = EntityByNameUriTemplate,
        Name = "entity-by-name",
        Title = "Entity by Name",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetEntityByName(
        [Description("Entity name")] string name)
    {
        var bridge = connection.GetBridge();
        var entity = await bridge.State.GetEntityByNameAsync(name);
        var uri = $"keeneyes://entity/name/{name}";

        if (entity == null)
        {
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new { error = $"Entity '{name}' not found" }, JsonOptions.Default)
            };
        }

        var json = JsonSerializer.Serialize(entity, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }
}
