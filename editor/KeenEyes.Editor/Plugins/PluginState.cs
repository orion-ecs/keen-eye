// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Represents the lifecycle state of a plugin.
/// </summary>
public enum PluginState
{
    /// <summary>
    /// The plugin has been discovered but not loaded into memory.
    /// </summary>
    Discovered,

    /// <summary>
    /// The plugin assembly is loaded but the plugin is not initialized.
    /// </summary>
    Loaded,

    /// <summary>
    /// The plugin is fully initialized and running.
    /// </summary>
    Enabled,

    /// <summary>
    /// The plugin was disabled by the user but remains in memory.
    /// </summary>
    Disabled,

    /// <summary>
    /// The plugin failed to load or initialize.
    /// </summary>
    Failed,

    /// <summary>
    /// The plugin is being unloaded.
    /// </summary>
    Unloading
}
