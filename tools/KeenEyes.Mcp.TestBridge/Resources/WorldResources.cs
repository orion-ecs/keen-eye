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
    private const string WorldSystemsUri = "keeneyes://world/systems";
    private const string WorldPerformanceUri = "keeneyes://world/performance";
    private const string EntityByIdUriTemplate = "keeneyes://entity/{id}";
    private const string EntityByNameUriTemplate = "keeneyes://entity/name/{name}";
    private const string EntityComponentUriTemplate = "keeneyes://entity/{id}/component/{type}";
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

    [McpServerResource(
        UriTemplate = EntityComponentUriTemplate,
        Name = "entity-component",
        Title = "Entity Component",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetEntityComponent(
        [Description("Entity ID")] int id,
        [Description("Component type name")] string type)
    {
        var bridge = connection.GetBridge();
        var component = await bridge.State.GetComponentAsync(id, type);
        var uri = $"keeneyes://entity/{id}/component/{type}";

        if (component == null)
        {
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = "application/json",
                Text = JsonSerializer.Serialize(new { error = $"Component '{type}' not found on entity {id}" }, JsonOptions.Default)
            };
        }

        var json = JsonSerializer.Serialize(component, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = WorldSystemsUri,
        Name = "world-systems",
        Title = "World Systems",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetWorldSystems()
    {
        var bridge = connection.GetBridge();
        var systems = await bridge.State.GetSystemsAsync();
        var json = JsonSerializer.Serialize(systems, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = WorldSystemsUri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = WorldPerformanceUri,
        Name = "world-performance",
        Title = "World Performance",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetWorldPerformance(
        [Description("Number of frames to analyze (default: 60)")] int frameCount = 60)
    {
        var bridge = connection.GetBridge();
        var metrics = await bridge.State.GetPerformanceMetricsAsync(frameCount);
        var json = JsonSerializer.Serialize(metrics, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = WorldPerformanceUri,
            MimeType = "application/json",
            Text = json
        };
    }
}
