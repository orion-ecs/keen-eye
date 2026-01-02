// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins.Registry;

/// <summary>
/// Represents a NuGet package source configuration.
/// </summary>
public sealed class PluginSource
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
}
