// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Dependencies;

/// <summary>
/// Represents the kind of dependency error.
/// </summary>
internal enum DependencyErrorKind
{
    /// <summary>
    /// A required dependency is not installed.
    /// </summary>
    MissingDependency,

    /// <summary>
    /// A dependency is installed but doesn't satisfy the version constraint.
    /// </summary>
    VersionMismatch,

    /// <summary>
    /// A circular dependency was detected in the dependency graph.
    /// </summary>
    CyclicDependency,

    /// <summary>
    /// The plugin is incompatible with the current editor version.
    /// </summary>
    EditorVersionIncompatible
}

/// <summary>
/// Base record for dependency resolution errors.
/// </summary>
/// <param name="PluginId">The ID of the plugin that has the dependency issue.</param>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="Kind">The kind of dependency error.</param>
internal abstract record DependencyError(
    string PluginId,
    string Message,
    DependencyErrorKind Kind);

/// <summary>
/// Error indicating a required dependency is not installed.
/// </summary>
/// <param name="PluginId">The ID of the plugin requiring the dependency.</param>
/// <param name="MissingPluginId">The ID of the missing dependency.</param>
/// <param name="RequiredConstraint">The version constraint that was required.</param>
internal sealed record MissingDependencyError(
    string PluginId,
    string MissingPluginId,
    string RequiredConstraint)
    : DependencyError(
        PluginId,
        $"Plugin '{PluginId}' requires '{MissingPluginId}' ({RequiredConstraint}) but it is not installed",
        DependencyErrorKind.MissingDependency);

/// <summary>
/// Error indicating a dependency doesn't satisfy the version constraint.
/// </summary>
/// <param name="PluginId">The ID of the plugin requiring the dependency.</param>
/// <param name="DependencyId">The ID of the dependency with version mismatch.</param>
/// <param name="RequiredConstraint">The version constraint that was required.</param>
/// <param name="InstalledVersion">The version that is actually installed.</param>
internal sealed record VersionMismatchError(
    string PluginId,
    string DependencyId,
    string RequiredConstraint,
    string InstalledVersion)
    : DependencyError(
        PluginId,
        $"Plugin '{PluginId}' requires '{DependencyId}' ({RequiredConstraint}) but version {InstalledVersion} is installed",
        DependencyErrorKind.VersionMismatch);

/// <summary>
/// Error indicating a circular dependency was detected.
/// </summary>
/// <param name="PluginId">The ID of the plugin where the cycle was detected.</param>
/// <param name="CyclePath">The path of the cycle (e.g., A → B → C → A).</param>
internal sealed record CyclicDependencyError(
    string PluginId,
    IReadOnlyList<string> CyclePath)
    : DependencyError(
        PluginId,
        $"Circular dependency detected: {string.Join(" → ", CyclePath)}",
        DependencyErrorKind.CyclicDependency);

/// <summary>
/// Error indicating the plugin is incompatible with the editor version.
/// </summary>
/// <param name="PluginId">The ID of the incompatible plugin.</param>
/// <param name="EditorVersion">The current editor version.</param>
/// <param name="MinVersion">The minimum required editor version, if specified.</param>
/// <param name="MaxVersion">The maximum supported editor version, if specified.</param>
internal sealed record EditorVersionIncompatibleError(
    string PluginId,
    string EditorVersion,
    string? MinVersion,
    string? MaxVersion)
    : DependencyError(
        PluginId,
        BuildMessage(PluginId, EditorVersion, MinVersion, MaxVersion),
        DependencyErrorKind.EditorVersionIncompatible)
{
    private static string BuildMessage(
        string pluginId,
        string editorVersion,
        string? minVersion,
        string? maxVersion)
    {
        if (minVersion is not null && maxVersion is not null)
        {
            return $"Plugin '{pluginId}' requires editor version {minVersion} to {maxVersion}, current is {editorVersion}";
        }

        if (minVersion is not null)
        {
            return $"Plugin '{pluginId}' requires editor version >= {minVersion}, current is {editorVersion}";
        }

        return $"Plugin '{pluginId}' requires editor version <= {maxVersion}, current is {editorVersion}";
    }
}
