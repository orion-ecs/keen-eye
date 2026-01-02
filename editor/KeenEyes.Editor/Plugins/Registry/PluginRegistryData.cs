// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins.Registry;

/// <summary>
/// The serializable registry data structure.
/// </summary>
internal sealed class PluginRegistryData
{
    /// <summary>
    /// Gets or sets the registry format version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the installed plugins.
    /// </summary>
    [JsonPropertyName("plugins")]
    public Dictionary<string, InstalledPluginEntry> Plugins { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the configured sources.
    /// </summary>
    [JsonPropertyName("sources")]
    public List<PluginSource> Sources { get; set; } =
    [
        new PluginSource
        {
            Name = "nuget.org",
            Url = "https://api.nuget.org/v3/index.json",
            IsDefault = true
        }
    ];
}
