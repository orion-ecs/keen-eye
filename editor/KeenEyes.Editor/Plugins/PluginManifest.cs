// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Represents the metadata and configuration for an editor plugin.
/// </summary>
/// <remarks>
/// Plugin manifests are loaded from <c>keeneyes-plugin.json</c> files in plugin packages.
/// </remarks>
public sealed class PluginManifest
{
    /// <summary>
    /// Gets or sets the display name of the plugin.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the plugin.
    /// </summary>
    /// <remarks>
    /// Should follow reverse domain notation (e.g., "com.example.myplugin").
    /// </remarks>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the plugin author.
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the plugin entry point configuration.
    /// </summary>
    [JsonPropertyName("entryPoint")]
    public required PluginEntryPoint EntryPoint { get; set; }

    /// <summary>
    /// Gets or sets the editor version compatibility requirements.
    /// </summary>
    [JsonPropertyName("compatibility")]
    public PluginCompatibility? Compatibility { get; set; }

    /// <summary>
    /// Gets or sets the plugin capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public PluginCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the plugin dependencies.
    /// </summary>
    /// <remarks>
    /// Keys are plugin IDs, values are version constraints (e.g., ">=1.0.0", "^2.0.0").
    /// </remarks>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = [];

    /// <summary>
    /// Gets or sets optional settings configuration.
    /// </summary>
    [JsonPropertyName("settings")]
    public PluginSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets the plugin security information.
    /// </summary>
    [JsonPropertyName("security")]
    public PluginSecurity? Security { get; set; }

    /// <summary>
    /// Gets or sets the plugin permission declarations.
    /// </summary>
    [JsonPropertyName("permissions")]
    public PluginPermissions? Permissions { get; set; }

    /// <summary>
    /// Parses a plugin manifest from JSON.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static PluginManifest Parse(string json)
    {
        return JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions)
            ?? throw new JsonException("Failed to parse plugin manifest: result was null");
    }

    /// <summary>
    /// Parses a plugin manifest from a stream.
    /// </summary>
    /// <param name="stream">The stream containing JSON content.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static PluginManifest Parse(Stream stream)
    {
        return JsonSerializer.Deserialize<PluginManifest>(stream, JsonOptions)
            ?? throw new JsonException("Failed to parse plugin manifest: result was null");
    }

    /// <summary>
    /// Attempts to parse a plugin manifest from JSON.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <param name="manifest">The parsed manifest, if successful.</param>
    /// <returns>True if parsing succeeded; false otherwise.</returns>
    public static bool TryParse(string json, out PluginManifest? manifest)
    {
        try
        {
            manifest = Parse(json);
            return true;
        }
        catch (JsonException)
        {
            manifest = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes the manifest to JSON.
    /// </summary>
    /// <returns>The JSON representation.</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// Specifies the entry point for a plugin.
/// </summary>
public sealed class PluginEntryPoint
{
    /// <summary>
    /// Gets or sets the assembly file name (e.g., "MyPlugin.dll").
    /// </summary>
    [JsonPropertyName("assembly")]
    public required string Assembly { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the plugin class.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

/// <summary>
/// Specifies editor version compatibility requirements.
/// </summary>
public sealed class PluginCompatibility
{
    /// <summary>
    /// Gets or sets the minimum compatible editor version.
    /// </summary>
    [JsonPropertyName("minEditorVersion")]
    public string? MinEditorVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum compatible editor version.
    /// </summary>
    [JsonPropertyName("maxEditorVersion")]
    public string? MaxEditorVersion { get; set; }
}

/// <summary>
/// Specifies plugin runtime capabilities.
/// </summary>
public sealed class PluginCapabilities
{
    /// <summary>
    /// Gets or sets whether the plugin supports hot reloading.
    /// </summary>
    /// <remarks>
    /// Hot-reloadable plugins can be unloaded and reloaded without editor restart.
    /// This requires the plugin to properly clean up all resources on shutdown.
    /// </remarks>
    [JsonPropertyName("supportsHotReload")]
    public bool SupportsHotReload { get; set; }

    /// <summary>
    /// Gets or sets whether the plugin can be disabled at runtime.
    /// </summary>
    /// <remarks>
    /// If false, the plugin can only be enabled/disabled by restarting the editor.
    /// </remarks>
    [JsonPropertyName("supportsDisable")]
    public bool SupportsDisable { get; set; } = true;
}

/// <summary>
/// Specifies plugin settings configuration.
/// </summary>
public sealed class PluginSettings
{
    /// <summary>
    /// Gets or sets the settings configuration file name.
    /// </summary>
    [JsonPropertyName("configFile")]
    public string? ConfigFile { get; set; }
}

/// <summary>
/// Specifies plugin security information for signature verification.
/// </summary>
public sealed class PluginSecurity
{
    /// <summary>
    /// Gets or sets the expected public key token of the signing certificate.
    /// </summary>
    [JsonPropertyName("publicKeyToken")]
    public string? PublicKeyToken { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the assembly for integrity verification.
    /// </summary>
    [JsonPropertyName("assemblyHash")]
    public string? AssemblyHash { get; set; }
}

/// <summary>
/// Specifies plugin permission declarations.
/// </summary>
public sealed class PluginPermissions
{
    /// <summary>
    /// Gets or sets the permissions required for the plugin to function.
    /// </summary>
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = [];

    /// <summary>
    /// Gets or sets optional permissions that enhance functionality.
    /// </summary>
    [JsonPropertyName("optional")]
    public List<string> Optional { get; set; } = [];
}
