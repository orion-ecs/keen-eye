// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Registry;

/// <summary>
/// Provides paths for the plugin registry.
/// </summary>
public static class PluginRegistryPaths
{
    private static string? testOverrideDirectory;

    /// <summary>
    /// Sets a test override directory. For testing only.
    /// </summary>
    /// <param name="directory">The override directory, or null to use default.</param>
    internal static void SetTestOverride(string? directory)
    {
        testOverrideDirectory = directory;
    }

    /// <summary>
    /// Gets the KeenEyes configuration directory path.
    /// </summary>
    /// <returns>Path to the .keeneyes directory in user home.</returns>
    public static string GetConfigDirectory()
    {
        if (testOverrideDirectory != null)
        {
            return Path.Combine(testOverrideDirectory, ".keeneyes");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".keeneyes");
    }

    /// <summary>
    /// Gets the plugins directory path.
    /// </summary>
    /// <returns>Path to the plugins subdirectory.</returns>
    public static string GetPluginsDirectory()
    {
        return Path.Combine(GetConfigDirectory(), "plugins");
    }

    /// <summary>
    /// Gets the plugin registry file path.
    /// </summary>
    /// <returns>Path to registry.json.</returns>
    public static string GetRegistryFilePath()
    {
        return Path.Combine(GetPluginsDirectory(), "registry.json");
    }

    /// <summary>
    /// Ensures all required directories exist.
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(GetPluginsDirectory());
    }
}
