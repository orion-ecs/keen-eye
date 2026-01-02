// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace KeenEyes.Editor.Plugins.Registry;

/// <summary>
/// Manages the plugin registry for tracking installed plugins.
/// </summary>
public sealed class PluginRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private PluginRegistryData data = new();

    /// <summary>
    /// Loads the registry from disk.
    /// </summary>
    public void Load()
    {
        var path = PluginRegistryPaths.GetRegistryFilePath();

        if (!File.Exists(path))
        {
            data = new PluginRegistryData();
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            data = JsonSerializer.Deserialize<PluginRegistryData>(json, JsonOptions) ?? new PluginRegistryData();
        }
        catch (JsonException)
        {
            // Corrupted file, start fresh
            data = new PluginRegistryData();
        }
    }

    /// <summary>
    /// Saves the registry to disk.
    /// </summary>
    public void Save()
    {
        PluginRegistryPaths.EnsureDirectoriesExist();

        var path = PluginRegistryPaths.GetRegistryFilePath();
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Registers an installed plugin.
    /// </summary>
    /// <param name="entry">The plugin entry to register.</param>
    public void RegisterPlugin(InstalledPluginEntry entry)
    {
        data.Plugins[entry.PackageId] = entry;
    }

    /// <summary>
    /// Unregisters a plugin.
    /// </summary>
    /// <param name="packageId">The package ID to unregister.</param>
    public void UnregisterPlugin(string packageId)
    {
        data.Plugins.Remove(packageId);
    }

    /// <summary>
    /// Gets all installed plugins.
    /// </summary>
    /// <returns>List of installed plugin entries.</returns>
    public IReadOnlyList<InstalledPluginEntry> GetInstalledPlugins()
    {
        return [.. data.Plugins.Values];
    }

    /// <summary>
    /// Gets an installed plugin by package ID.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <returns>The plugin entry or null if not found.</returns>
    public InstalledPluginEntry? GetInstalledPlugin(string packageId)
    {
        return data.Plugins.TryGetValue(packageId, out var entry) ? entry : null;
    }

    /// <summary>
    /// Checks if a plugin is installed.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <returns>True if installed.</returns>
    public bool IsInstalled(string packageId)
    {
        return data.Plugins.ContainsKey(packageId);
    }

    /// <summary>
    /// Gets plugins that depend on the specified package.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <returns>List of dependent plugins.</returns>
    public IReadOnlyList<InstalledPluginEntry> GetDependentPlugins(string packageId)
    {
        return data.Plugins.Values
            .Where(p => p.Dependencies.Contains(packageId, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets all configured sources.
    /// </summary>
    /// <returns>List of sources.</returns>
    public IReadOnlyList<PluginSource> GetSources()
    {
        return [.. data.Sources];
    }

    /// <summary>
    /// Adds a new source.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <param name="url">The source URL.</param>
    /// <param name="makeDefault">Whether to make this the default.</param>
    public void AddSource(string name, string url, bool makeDefault = false)
    {
        if (makeDefault)
        {
            // Clear existing default
            foreach (var source in data.Sources)
            {
                source.IsDefault = false;
            }
        }

        data.Sources.Add(new PluginSource
        {
            Name = name,
            Url = url,
            IsDefault = makeDefault
        });
    }

    /// <summary>
    /// Removes a source by name.
    /// </summary>
    /// <param name="name">The source name.</param>
    public void RemoveSource(string name)
    {
        var source = data.Sources.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (source != null)
        {
            data.Sources.Remove(source);
        }
    }

    /// <summary>
    /// Gets the default source URL.
    /// </summary>
    /// <returns>The default source URL or nuget.org if none configured.</returns>
    public string GetDefaultSourceUrl()
    {
        var defaultSource = data.Sources.FirstOrDefault(s => s.IsDefault);
        return defaultSource?.Url ?? "https://api.nuget.org/v3/index.json";
    }
}
