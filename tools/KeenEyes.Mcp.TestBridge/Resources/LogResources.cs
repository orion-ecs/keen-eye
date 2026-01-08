using System.ComponentModel;
using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// MCP resources for log viewing and analysis.
/// </summary>
[McpServerResourceType]
public sealed class LogResources(BridgeConnectionManager connection)
{
    // S1075: MCP protocol requires these URIs for resource identification
#pragma warning disable S1075
    private const string LogStatsUri = "keeneyes://logs/stats";
    private const string LogRecentUri = "keeneyes://logs/recent";
    private const string LogByLevelUriTemplate = "keeneyes://logs/level/{level}";
    private const string LogByCategoryUriTemplate = "keeneyes://logs/category/{pattern}";
#pragma warning restore S1075

    [McpServerResource(
        UriTemplate = LogStatsUri,
        Name = "log-stats",
        Title = "Log Statistics",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetLogStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Logs.GetStatsAsync();
        var json = JsonSerializer.Serialize(stats, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = LogStatsUri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = LogRecentUri,
        Name = "log-recent",
        Title = "Recent Log Entries",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetRecentLogs(
        [Description("Maximum number of entries to return (default: 100)")] int count = 100)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Logs.GetRecentAsync(count);
        var json = JsonSerializer.Serialize(entries, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = LogRecentUri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = LogByLevelUriTemplate,
        Name = "log-by-level",
        Title = "Logs by Level",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetLogsByLevel(
        [Description("Log level (0=Trace, 1=Debug, 2=Info, 3=Warning, 4=Error, 5=Fatal)")] int level,
        [Description("Maximum number of entries to return (default: 1000)")] int maxResults = 1000)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Logs.GetByLevelAsync(level, maxResults);
        var uri = $"keeneyes://logs/level/{level}";
        var json = JsonSerializer.Serialize(entries, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }

    [McpServerResource(
        UriTemplate = LogByCategoryUriTemplate,
        Name = "log-by-category",
        Title = "Logs by Category",
        MimeType = "application/json")]
    public async Task<TextResourceContents> GetLogsByCategory(
        [Description("Category pattern (supports wildcards: *, ?)")] string pattern,
        [Description("Maximum number of entries to return (default: 1000)")] int maxResults = 1000)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Logs.GetByCategoryAsync(pattern, maxResults);
        var uri = $"keeneyes://logs/category/{Uri.EscapeDataString(pattern)}";
        var json = JsonSerializer.Serialize(entries, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }
}
