// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins.Registry;

/// <summary>
/// Represents an installed plugin in the registry.
/// </summary>
public sealed class InstalledPluginEntry
{
    /// <summary>
    /// Gets or sets the NuGet package ID.
    /// </summary>
    [JsonPropertyName("packageId")]
    public required string PackageId { get; set; }

    /// <summary>
    /// Gets or sets the installed version.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the source URL this package was installed from.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets when the plugin was installed.
    /// </summary>
    [JsonPropertyName("installedAt")]
    public DateTimeOffset InstalledAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets whether the plugin is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the install path in the NuGet cache.
    /// </summary>
    [JsonPropertyName("installPath")]
    public string? InstallPath { get; set; }

    /// <summary>
    /// Gets or sets the list of dependencies this plugin requires.
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];
}
