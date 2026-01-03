// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins.Settings;

/// <summary>
/// Hot reload configuration for plugins.
/// </summary>
public sealed class HotReloadSettings
{
    /// <summary>
    /// Gets or sets whether hot reload is enabled for plugins.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds before reloading.
    /// </summary>
    [JsonPropertyName("debounceMs")]
    public int DebounceMs { get; set; } = 500;
}

/// <summary>
/// Developer options for plugin development.
/// </summary>
public sealed class DeveloperSettings
{
    /// <summary>
    /// Gets or sets whether developer mode is enabled.
    /// </summary>
    /// <remarks>
    /// Developer mode enables additional debugging features and relaxes some restrictions.
    /// </remarks>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether verbose logging is enabled.
    /// </summary>
    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// Gets or sets whether to show internal plugin errors in the UI.
    /// </summary>
    [JsonPropertyName("showInternalErrors")]
    public bool ShowInternalErrors { get; set; }
}

/// <summary>
/// Plugin-specific security settings.
/// </summary>
/// <remarks>
/// These settings complement <see cref="Security.SecurityConfiguration"/> with plugin-specific options.
/// </remarks>
public sealed class PluginSecuritySettings
{
    /// <summary>
    /// Gets or sets whether to require code signing for plugins.
    /// </summary>
    [JsonPropertyName("requireCodeSigning")]
    public bool RequireCodeSigning { get; set; }

    /// <summary>
    /// Gets or sets whether the permission system is enabled.
    /// </summary>
    [JsonPropertyName("enablePermissionSystem")]
    public bool EnablePermissionSystem { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to warn about untrusted publishers.
    /// </summary>
    [JsonPropertyName("warnUntrustedPublishers")]
    public bool WarnUntrustedPublishers { get; set; } = true;
}

/// <summary>
/// Configuration for a plugin search path.
/// </summary>
public sealed class PluginSearchPath
{
    /// <summary>
    /// Gets or sets the directory path to search for plugins.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets whether this search path is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional description for this path.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether to search subdirectories.
    /// </summary>
    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; } = true;
}

/// <summary>
/// Extended package source configuration with additional settings.
/// </summary>
public sealed class PluginSourceSettings
{
    /// <summary>
    /// Gets or sets the friendly name of the source.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the NuGet V3 feed URL.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default source.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this source is enabled.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether authentication is required.
    /// </summary>
    [JsonPropertyName("requiresAuth")]
    public bool RequiresAuth { get; set; }

    /// <summary>
    /// Gets or sets the authentication type (e.g., "apikey", "basic").
    /// </summary>
    [JsonPropertyName("authType")]
    public string? AuthType { get; set; }
}

/// <summary>
/// The serializable plugin settings data structure.
/// </summary>
internal sealed class PluginSettingsData
{
    /// <summary>
    /// Gets or sets the settings format version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the configured package sources.
    /// </summary>
    [JsonPropertyName("sources")]
    public List<PluginSourceSettings> Sources { get; set; } =
    [
        new PluginSourceSettings
        {
            Name = "nuget.org",
            Url = "https://api.nuget.org/v3/index.json",
            IsDefault = true,
            IsEnabled = true
        }
    ];

    /// <summary>
    /// Gets or sets the plugin search paths.
    /// </summary>
    [JsonPropertyName("searchPaths")]
    public List<PluginSearchPath> SearchPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the hot reload settings.
    /// </summary>
    [JsonPropertyName("hotReload")]
    public HotReloadSettings HotReload { get; set; } = new();

    /// <summary>
    /// Gets or sets the developer settings.
    /// </summary>
    [JsonPropertyName("developer")]
    public DeveloperSettings Developer { get; set; } = new();

    /// <summary>
    /// Gets or sets the plugin security settings.
    /// </summary>
    [JsonPropertyName("security")]
    public PluginSecuritySettings Security { get; set; } = new();
}
