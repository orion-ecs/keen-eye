// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Discovers and manages installed plugins from various sources.
/// </summary>
/// <remarks>
/// <para>
/// The plugin repository scans the following locations for plugins:
/// </para>
/// <list type="bullet">
/// <item><b>NuGet global cache</b> - ~/.nuget/packages/</item>
/// <item><b>Editor plugins folder</b> - {editor}/plugins/</item>
/// <item><b>Project plugins</b> - {project}/.keeneyes/plugins/</item>
/// <item><b>Development folder</b> - {project}/Plugins/ (for local dev)</item>
/// </list>
/// </remarks>
internal sealed class PluginRepository
{
    private const string ManifestFileName = "keeneyes-plugin.json";
    private const string NuGetPackagesFolderName = "packages";

    private readonly List<string> searchPaths = [];
    private readonly Dictionary<string, LoadedPlugin> discoveredPlugins = [];
    private readonly IEditorPluginLogger? logger;

    /// <summary>
    /// Gets all discovered plugins.
    /// </summary>
    public IReadOnlyDictionary<string, LoadedPlugin> Plugins => discoveredPlugins;

    /// <summary>
    /// Creates a new plugin repository.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public PluginRepository(IEditorPluginLogger? logger = null)
    {
        this.logger = logger;
        InitializeDefaultSearchPaths();
    }

    /// <summary>
    /// Adds a search path for plugin discovery.
    /// </summary>
    /// <param name="path">The path to search.</param>
    public void AddSearchPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !searchPaths.Contains(path))
        {
            searchPaths.Add(path);
        }
    }

    /// <summary>
    /// Scans all search paths for plugins.
    /// </summary>
    /// <returns>The number of plugins discovered.</returns>
    public int Scan()
    {
        discoveredPlugins.Clear();
        var count = 0;

        foreach (var path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                count += ScanDirectory(path);
            }
        }

        // Also scan NuGet global cache
        count += ScanNuGetCache();

        logger?.LogInfo($"Discovered {count} plugin(s) from {searchPaths.Count} search path(s)");
        return count;
    }

    /// <summary>
    /// Gets a discovered plugin by ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The plugin, or null if not found.</returns>
    public LoadedPlugin? GetPlugin(string pluginId)
    {
        return discoveredPlugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
    }

    /// <summary>
    /// Checks if a plugin with the specified ID exists.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>True if the plugin exists; false otherwise.</returns>
    public bool HasPlugin(string pluginId)
    {
        return discoveredPlugins.ContainsKey(pluginId);
    }

    private void InitializeDefaultSearchPaths()
    {
        // Editor plugins folder (next to editor executable)
        var editorDir = AppDomain.CurrentDomain.BaseDirectory;
        var editorPluginsPath = Path.Combine(editorDir, "plugins");
        if (Directory.Exists(editorPluginsPath))
        {
            searchPaths.Add(editorPluginsPath);
        }
    }

    private int ScanDirectory(string directory)
    {
        var count = 0;

        try
        {
            // Look for manifest files in immediate subdirectories
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var manifestPath = Path.Combine(subDir, ManifestFileName);
                if (File.Exists(manifestPath) &&
                    TryLoadManifest(manifestPath, subDir, out var plugin))
                {
                    RegisterPlugin(plugin);
                    count++;
                }

                // Also check lib/net10.0 structure (NuGet package layout)
                var libPath = Path.Combine(subDir, "lib", "net10.0");
                manifestPath = Path.Combine(subDir, "content", ManifestFileName);
                if (Directory.Exists(libPath) &&
                    File.Exists(manifestPath) &&
                    TryLoadManifest(manifestPath, libPath, out var plugin2))
                {
                    RegisterPlugin(plugin2);
                    count++;
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning($"Error scanning directory '{directory}': {ex.Message}");
        }

        return count;
    }

    private int ScanNuGetCache()
    {
        var count = 0;
        var nugetCache = GetNuGetPackagesPath();

        if (string.IsNullOrEmpty(nugetCache) || !Directory.Exists(nugetCache))
        {
            return 0;
        }

        try
        {
            // NuGet packages are stored as: ~/.nuget/packages/{package-id}/{version}/
            foreach (var packageDir in Directory.GetDirectories(nugetCache))
            {
                // Check each version
                foreach (var versionDir in Directory.GetDirectories(packageDir))
                {
                    // Look for manifest in content folder
                    var manifestPath = Path.Combine(versionDir, "content", ManifestFileName);
                    var libPath = Path.Combine(versionDir, "lib", "net10.0");

                    if (File.Exists(manifestPath) &&
                        Directory.Exists(libPath) &&
                        TryLoadManifest(manifestPath, libPath, out var plugin) &&
                        (!discoveredPlugins.TryGetValue(plugin.Manifest.Id, out var existing) ||
                            IsNewerVersion(plugin.Manifest.Version, existing.Manifest.Version)))
                    {
                        RegisterPlugin(plugin);
                        count++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning($"Error scanning NuGet cache: {ex.Message}");
        }

        return count;
    }

    private static string? GetNuGetPackagesPath()
    {
        // Check NUGET_PACKAGES environment variable first
        var envPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            return envPath;
        }

        // Default location: ~/.nuget/packages
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultPath = Path.Combine(home, ".nuget", NuGetPackagesFolderName);

        return Directory.Exists(defaultPath) ? defaultPath : null;
    }

    private bool TryLoadManifest(string manifestPath, string basePath, out LoadedPlugin plugin)
    {
        plugin = null!;

        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = PluginManifest.Parse(json);

            // Verify the assembly exists
            var assemblyPath = Path.Combine(basePath, manifest.EntryPoint.Assembly);
            if (!File.Exists(assemblyPath))
            {
                logger?.LogWarning(
                    $"Plugin manifest at '{manifestPath}' references missing assembly: {manifest.EntryPoint.Assembly}");
                return false;
            }

            plugin = new LoadedPlugin(manifest, basePath);
            return true;
        }
        catch (JsonException ex)
        {
            logger?.LogWarning($"Failed to parse plugin manifest '{manifestPath}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogWarning($"Error loading plugin manifest '{manifestPath}': {ex.Message}");
            return false;
        }
    }

    private void RegisterPlugin(LoadedPlugin plugin)
    {
        discoveredPlugins[plugin.Manifest.Id] = plugin;
        logger?.LogInfo($"Discovered plugin: {plugin.Manifest.Name} ({plugin.Manifest.Id}) v{plugin.Manifest.Version}");
    }

    private static bool IsNewerVersion(string version1, string version2)
    {
        // Simple version comparison - could be enhanced with proper SemVer parsing
        if (Version.TryParse(version1, out var v1) && Version.TryParse(version2, out var v2))
        {
            return v1 > v2;
        }

        return string.Compare(version1, version2, StringComparison.Ordinal) > 0;
    }
}
