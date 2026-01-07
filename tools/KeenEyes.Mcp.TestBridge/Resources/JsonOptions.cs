using System.Text.Json;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// Shared JSON serialization options for MCP resources.
/// </summary>
internal static class JsonOptions
{
    /// <summary>
    /// Default serialization options with camelCase naming.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
