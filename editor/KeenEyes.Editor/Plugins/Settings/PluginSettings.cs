// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Editor.Plugins.Settings;

/// <summary>
/// Manages global plugin settings including sources, search paths, and options.
/// </summary>
/// <remarks>
/// This is distinct from <see cref="KeenEyes.Editor.Plugins.PluginSettings"/> which
/// represents per-plugin settings in manifests.
/// </remarks>
public sealed class GlobalPluginSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private PluginSettingsData data = new();
    private readonly string settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalPluginSettings"/> class
    /// using the default settings path.
    /// </summary>
    public GlobalPluginSettings()
        : this(GetDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalPluginSettings"/> class
    /// with a custom settings path.
    /// </summary>
    /// <param name="path">The path to the settings file.</param>
    public GlobalPluginSettings(string path)
    {
        settingsPath = path;
    }

    /// <summary>
    /// Private constructor for factory methods.
    /// </summary>
    private GlobalPluginSettings(PluginSettingsData settingsData)
        : this(GetDefaultSettingsPath())
    {
        data = settingsData;
    }

    /// <summary>
    /// Gets the hot reload settings.
    /// </summary>
    public HotReloadSettings HotReload => data.HotReload;

    /// <summary>
    /// Gets the developer settings.
    /// </summary>
    public DeveloperSettings Developer => data.Developer;

    /// <summary>
    /// Gets the security settings.
    /// </summary>
    public PluginSecuritySettings Security => data.Security;

    /// <summary>
    /// Gets the default settings file path.
    /// </summary>
    /// <returns>Path to plugin-settings.json in the user's .keeneyes directory.</returns>
    public static string GetDefaultSettingsPath()
    {
        return Path.Combine(PluginRegistryPaths.GetConfigDirectory(), "plugin-settings.json");
    }

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(settingsPath))
        {
            data = new PluginSettingsData();
            return;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            data = JsonSerializer.Deserialize<PluginSettingsData>(json, JsonOptions) ?? new PluginSettingsData();
        }
        catch (JsonException)
        {
            // Corrupted file, start fresh
            data = new PluginSettingsData();
        }
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }

    #region Package Sources

    /// <summary>
    /// Gets all configured package sources.
    /// </summary>
    /// <returns>List of package sources.</returns>
    public IReadOnlyList<PluginSourceSettings> GetSources()
    {
        return [.. data.Sources];
    }

    /// <summary>
    /// Gets only enabled package sources.
    /// </summary>
    /// <returns>List of enabled package sources.</returns>
    public IReadOnlyList<PluginSourceSettings> GetEnabledSources()
    {
        return data.Sources.Where(s => s.IsEnabled).ToList();
    }

    /// <summary>
    /// Adds a new package source.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <param name="url">The source URL.</param>
    /// <param name="makeDefault">Whether to make this the default source.</param>
    public void AddSource(string name, string url, bool makeDefault = false)
    {
        if (makeDefault)
        {
            foreach (var source in data.Sources)
            {
                source.IsDefault = false;
            }
        }

        data.Sources.Add(new PluginSourceSettings
        {
            Name = name,
            Url = url,
            IsDefault = makeDefault,
            IsEnabled = true
        });
    }

    /// <summary>
    /// Removes a package source by name.
    /// </summary>
    /// <param name="name">The source name to remove.</param>
    /// <returns>True if the source was removed.</returns>
    public bool RemoveSource(string name)
    {
        var source = data.Sources.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (source != null)
        {
            data.Sources.Remove(source);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Enables or disables a package source.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <param name="enabled">Whether to enable the source.</param>
    /// <returns>True if the source was found and updated.</returns>
    public bool SetSourceEnabled(string name, bool enabled)
    {
        var source = data.Sources.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (source != null)
        {
            source.IsEnabled = enabled;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the default source URL.
    /// </summary>
    /// <returns>The default enabled source URL, or nuget.org if none configured.</returns>
    public string GetDefaultSourceUrl()
    {
        var defaultSource = data.Sources.FirstOrDefault(s => s.IsDefault && s.IsEnabled);
        defaultSource ??= data.Sources.FirstOrDefault(s => s.IsEnabled);
        return defaultSource?.Url ?? "https://api.nuget.org/v3/index.json";
    }

    /// <summary>
    /// Moves a source up in the priority list.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <returns>True if the source was moved.</returns>
    public bool MoveSourceUp(string name)
    {
        var index = data.Sources.FindIndex(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (index > 0)
        {
            (data.Sources[index - 1], data.Sources[index]) = (data.Sources[index], data.Sources[index - 1]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Moves a source down in the priority list.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <returns>True if the source was moved.</returns>
    public bool MoveSourceDown(string name)
    {
        var index = data.Sources.FindIndex(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (index >= 0 && index < data.Sources.Count - 1)
        {
            (data.Sources[index + 1], data.Sources[index]) = (data.Sources[index], data.Sources[index + 1]);
            return true;
        }

        return false;
    }

    #endregion

    #region Search Paths

    /// <summary>
    /// Gets all configured search paths.
    /// </summary>
    /// <returns>List of search paths.</returns>
    public IReadOnlyList<PluginSearchPath> GetSearchPaths()
    {
        return [.. data.SearchPaths];
    }

    /// <summary>
    /// Gets only enabled search paths.
    /// </summary>
    /// <returns>List of enabled search paths.</returns>
    public IReadOnlyList<PluginSearchPath> GetEnabledSearchPaths()
    {
        return data.SearchPaths.Where(p => p.Enabled).ToList();
    }

    /// <summary>
    /// Adds a new search path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    public void AddSearchPath(string path, string? description = null, bool recursive = true)
    {
        // Normalize path
        var normalizedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));

        // Check if already exists
        if (data.SearchPaths.Any(p => p.Path.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        data.SearchPaths.Add(new PluginSearchPath
        {
            Path = normalizedPath,
            Description = description,
            Recursive = recursive,
            Enabled = true
        });
    }

    /// <summary>
    /// Removes a search path.
    /// </summary>
    /// <param name="path">The path to remove.</param>
    /// <returns>True if the path was removed.</returns>
    public bool RemoveSearchPath(string path)
    {
        var normalizedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        var searchPath = data.SearchPaths.FirstOrDefault(p =>
            p.Path.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (searchPath != null)
        {
            data.SearchPaths.Remove(searchPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Enables or disables a search path.
    /// </summary>
    /// <param name="path">The search path.</param>
    /// <param name="enabled">Whether to enable the path.</param>
    /// <returns>True if the path was found and updated.</returns>
    public bool SetSearchPathEnabled(string path, bool enabled)
    {
        var normalizedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        var searchPath = data.SearchPaths.FirstOrDefault(p =>
            p.Path.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (searchPath != null)
        {
            searchPath.Enabled = enabled;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Moves a search path up in the priority list.
    /// </summary>
    /// <param name="path">The search path.</param>
    /// <returns>True if the path was moved.</returns>
    public bool MoveSearchPathUp(string path)
    {
        var normalizedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        var index = data.SearchPaths.FindIndex(p =>
            p.Path.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (index > 0)
        {
            (data.SearchPaths[index - 1], data.SearchPaths[index]) = (data.SearchPaths[index], data.SearchPaths[index - 1]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Moves a search path down in the priority list.
    /// </summary>
    /// <param name="path">The search path.</param>
    /// <returns>True if the path was moved.</returns>
    public bool MoveSearchPathDown(string path)
    {
        var normalizedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        var index = data.SearchPaths.FindIndex(p =>
            p.Path.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (index >= 0 && index < data.SearchPaths.Count - 1)
        {
            (data.SearchPaths[index + 1], data.SearchPaths[index]) = (data.SearchPaths[index], data.SearchPaths[index + 1]);
            return true;
        }

        return false;
    }

    #endregion

    #region Options

    /// <summary>
    /// Sets whether hot reload is enabled.
    /// </summary>
    /// <param name="enabled">Whether to enable hot reload.</param>
    public void SetHotReloadEnabled(bool enabled)
    {
        data.HotReload.Enabled = enabled;
    }

    /// <summary>
    /// Sets whether developer mode is enabled.
    /// </summary>
    /// <param name="enabled">Whether to enable developer mode.</param>
    public void SetDeveloperModeEnabled(bool enabled)
    {
        data.Developer.Enabled = enabled;
    }

    /// <summary>
    /// Sets whether verbose logging is enabled.
    /// </summary>
    /// <param name="enabled">Whether to enable verbose logging.</param>
    public void SetVerboseLoggingEnabled(bool enabled)
    {
        data.Developer.VerboseLogging = enabled;
    }

    /// <summary>
    /// Sets whether code signing is required.
    /// </summary>
    /// <param name="required">Whether to require code signing.</param>
    public void SetCodeSigningRequired(bool required)
    {
        data.Security.RequireCodeSigning = required;
    }

    /// <summary>
    /// Sets whether the permission system is enabled.
    /// </summary>
    /// <param name="enabled">Whether to enable the permission system.</param>
    public void SetPermissionSystemEnabled(bool enabled)
    {
        data.Security.EnablePermissionSystem = enabled;
    }

    #endregion

    #region Defaults

    /// <summary>
    /// Gets the default plugin settings.
    /// </summary>
    public static GlobalPluginSettings Default { get; } = new();

    /// <summary>
    /// Creates development-friendly settings.
    /// </summary>
    /// <returns>Settings configured for development.</returns>
    public static GlobalPluginSettings CreateDevelopmentSettings()
    {
        return new GlobalPluginSettings(new PluginSettingsData
        {
            HotReload = new HotReloadSettings { Enabled = true },
            Developer = new DeveloperSettings
            {
                Enabled = true,
                VerboseLogging = true,
                ShowInternalErrors = true
            },
            Security = new PluginSecuritySettings
            {
                RequireCodeSigning = false,
                EnablePermissionSystem = false,
                WarnUntrustedPublishers = false
            }
        });
    }

    /// <summary>
    /// Creates production settings with enhanced security.
    /// </summary>
    /// <returns>Settings configured for production.</returns>
    public static GlobalPluginSettings CreateProductionSettings()
    {
        return new GlobalPluginSettings(new PluginSettingsData
        {
            HotReload = new HotReloadSettings { Enabled = false },
            Developer = new DeveloperSettings
            {
                Enabled = false,
                VerboseLogging = false,
                ShowInternalErrors = false
            },
            Security = new PluginSecuritySettings
            {
                RequireCodeSigning = true,
                EnablePermissionSystem = true,
                WarnUntrustedPublishers = true
            }
        });
    }

    #endregion
}
