// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Represents a plugin that has been loaded or discovered by the editor.
/// </summary>
/// <remarks>
/// This class tracks the full lifecycle of a plugin from discovery through
/// loading, enabling, disabling, and unloading.
/// </remarks>
public sealed class LoadedPlugin
{
    /// <summary>
    /// Gets the plugin manifest.
    /// </summary>
    public PluginManifest Manifest { get; }

    /// <summary>
    /// Gets the path to the plugin's base directory.
    /// </summary>
    public string BasePath { get; }

    /// <summary>
    /// Gets the current state of the plugin.
    /// </summary>
    public PluginState State { get; internal set; }

    /// <summary>
    /// Gets the plugin instance, if loaded and enabled.
    /// </summary>
    public IEditorPlugin? Instance { get; internal set; }

    /// <summary>
    /// Gets the plugin context, if enabled.
    /// </summary>
    internal EditorPluginContext? Context { get; set; }

    /// <summary>
    /// Gets the assembly load context for this plugin.
    /// </summary>
    internal PluginLoadContext? LoadContext { get; set; }

    /// <summary>
    /// Gets the error message if the plugin is in a failed state.
    /// </summary>
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    /// Gets or sets the saved state from the last hot reload.
    /// </summary>
    /// <remarks>
    /// This is populated when a plugin implementing <see cref="IStatefulPlugin"/>
    /// is reloaded. The state is captured before unload and restored after reload.
    /// </remarks>
    internal object? SavedState { get; set; }

    /// <summary>
    /// Gets a value indicating whether the plugin supports hot reloading.
    /// </summary>
    public bool SupportsHotReload => Manifest.Capabilities.SupportsHotReload;

    /// <summary>
    /// Gets a value indicating whether the plugin can be disabled at runtime.
    /// </summary>
    public bool SupportsDisable => Manifest.Capabilities.SupportsDisable;

    /// <summary>
    /// Creates a new loaded plugin entry.
    /// </summary>
    /// <param name="manifest">The plugin manifest.</param>
    /// <param name="basePath">The path to the plugin's base directory.</param>
    public LoadedPlugin(PluginManifest manifest, string basePath)
    {
        Manifest = manifest;
        BasePath = basePath;
        State = PluginState.Discovered;
    }

    /// <summary>
    /// Gets the full path to the plugin's main assembly.
    /// </summary>
    /// <returns>The assembly path.</returns>
    public string GetAssemblyPath()
    {
        return Path.Combine(BasePath, Manifest.EntryPoint.Assembly);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Manifest.Name} ({Manifest.Id}) v{Manifest.Version} [{State}]";
    }
}
