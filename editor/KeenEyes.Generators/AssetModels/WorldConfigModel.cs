// Copyright (c) KeenEyes Contributors. Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;

namespace KeenEyes.Generators.AssetModels;

/// <summary>
/// Represents a world configuration loaded from a .keworld file.
/// </summary>
internal sealed class WorldConfigModel
{
    /// <summary>
    /// Gets or sets the schema reference for IDE validation.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the configuration name (used for method naming).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the world settings.
    /// </summary>
    public WorldSettingsModel Settings { get; set; } = new WorldSettingsModel();

    /// <summary>
    /// Gets or sets singleton components to register.
    /// Key is the component type name, value is the component data.
    /// </summary>
    public Dictionary<string, Dictionary<string, object?>> Singletons { get; set; } = [];

    /// <summary>
    /// Gets or sets the plugin type names to install.
    /// </summary>
    public List<string> Plugins { get; set; } = [];

    /// <summary>
    /// Gets or sets the systems to register.
    /// </summary>
    public List<SystemRegistrationModel> Systems { get; set; } = [];
}

/// <summary>
/// Represents world settings configuration.
/// </summary>
internal sealed class WorldSettingsModel
{
    /// <summary>
    /// Gets or sets the fixed time step for physics updates.
    /// </summary>
    public float FixedTimeStep { get; set; } = 0.02f;

    /// <summary>
    /// Gets or sets the maximum delta time to prevent spiral of death.
    /// </summary>
    public float MaxDeltaTime { get; set; } = 0.1f;
}

/// <summary>
/// Represents a system registration entry.
/// </summary>
internal sealed class SystemRegistrationModel
{
    /// <summary>
    /// Gets or sets the fully qualified type name of the system.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution phase (Update, FixedUpdate, LateUpdate, etc.).
    /// </summary>
    public string Phase { get; set; } = "Update";

    /// <summary>
    /// Gets or sets the execution order within the phase.
    /// </summary>
    public int Order { get; set; } = 0;
}
