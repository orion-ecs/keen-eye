// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Diagnostics information about a plugin unload operation.
/// </summary>
/// <remarks>
/// <para>
/// When a plugin is unloaded, this class captures details about the unload process
/// that can help diagnose issues with plugins that fail to fully unload.
/// </para>
/// <para>
/// A plugin may fail to fully unload if references to types from its assembly
/// are still held elsewhere (event handlers, cached types, etc.). The diagnostics
/// provide information to help identify the cause.
/// </para>
/// </remarks>
public sealed class UnloadDiagnostics
{
    /// <summary>
    /// Gets the ID of the plugin that was unloaded.
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// Gets the total time spent attempting to unload the plugin.
    /// </summary>
    public required TimeSpan UnloadDuration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the plugin's assembly was fully unloaded.
    /// </summary>
    /// <remarks>
    /// If false, the assembly may still be loaded in memory due to lingering references.
    /// This can cause memory leaks and prevent reloading a newer version of the plugin.
    /// </remarks>
    public required bool FullyUnloaded { get; init; }

    /// <summary>
    /// Gets the number of garbage collection attempts made while waiting for unload.
    /// </summary>
    public required int GcAttempts { get; init; }

    /// <summary>
    /// Gets a list of potential reference holders that may be preventing unload.
    /// </summary>
    /// <remarks>
    /// This list is informational and based on tracked resources. It may not be
    /// exhaustive - other untracked references could also prevent unload.
    /// </remarks>
    public required IReadOnlyList<string> PotentialHolders { get; init; }

    /// <summary>
    /// Gets the tracked resource counts at the time of unload.
    /// </summary>
    /// <remarks>
    /// Keys are resource categories (e.g., "Subscriptions", "Panels"), values are counts.
    /// Non-zero counts after context disposal may indicate cleanup issues.
    /// </remarks>
    public IReadOnlyDictionary<string, int>? ResourceCounts { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        var status = FullyUnloaded ? "fully unloaded" : "may have lingering references";
        return $"Plugin '{PluginId}' {status} in {UnloadDuration.TotalMilliseconds:F0}ms ({GcAttempts} GC attempts)";
    }
}
