// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Stores panel state (open/closed, position, size) for restoration after hot reload.
/// </summary>
/// <remarks>
/// <para>
/// When a plugin is hot-reloaded, this store captures the state of panels registered
/// by that plugin before unload, and restores them after reload.
/// </para>
/// <para>
/// State captured includes:
/// </para>
/// <list type="bullet">
/// <item>Whether each panel is open</item>
/// <item>Panel position (if floating or moved from default)</item>
/// <item>Panel size (if resized from default)</item>
/// <item>Current dock location</item>
/// </list>
/// </remarks>
internal sealed class PanelStateStore
{
    private readonly Dictionary<string, PanelSnapshot> pluginSnapshots = [];

    /// <summary>
    /// Captures the current state of all panels registered by a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="panelCapability">The panel capability to query.</param>
    /// <param name="panelIds">The IDs of panels registered by this plugin.</param>
    /// <returns>A snapshot of the panel states, or null if no panels to capture.</returns>
    public PanelSnapshot? CapturePluginPanels(
        string pluginId,
        IPanelCapability panelCapability,
        IEnumerable<string> panelIds)
    {
        var panels = new Dictionary<string, PanelState>();

        foreach (var panelId in panelIds)
        {
            var isOpen = panelCapability.IsPanelOpen(panelId);

            // Get extended state if capability supports it
            var extendedState = (panelCapability as IExtendedPanelCapability)
                ?.GetPanelState(panelId);

            panels[panelId] = new PanelState
            {
                IsOpen = isOpen,
                X = extendedState?.X,
                Y = extendedState?.Y,
                Width = extendedState?.Width,
                Height = extendedState?.Height,
                DockLocation = extendedState?.DockLocation
            };
        }

        if (panels.Count == 0)
        {
            return null;
        }

        var snapshot = new PanelSnapshot { Panels = panels };
        pluginSnapshots[pluginId] = snapshot;
        return snapshot;
    }

    /// <summary>
    /// Restores panel state after plugin reload.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="panelCapability">The panel capability to use for restoration.</param>
    /// <returns>True if state was restored; false if no saved state exists.</returns>
    public bool RestorePanelState(string pluginId, IPanelCapability panelCapability)
    {
        if (!pluginSnapshots.TryGetValue(pluginId, out var snapshot))
        {
            return false;
        }

        foreach (var (panelId, state) in snapshot.Panels)
        {
            // Restore open/closed state
            var isCurrentlyOpen = panelCapability.IsPanelOpen(panelId);

            if (state.IsOpen && !isCurrentlyOpen)
            {
                panelCapability.OpenPanel(panelId);
            }
            else if (!state.IsOpen && isCurrentlyOpen)
            {
                panelCapability.ClosePanel(panelId);
            }

            // Restore extended state if capability supports it
            if (panelCapability is IExtendedPanelCapability extendedCapability)
            {
                extendedCapability.SetPanelState(panelId, new ExtendedPanelState
                {
                    X = state.X,
                    Y = state.Y,
                    Width = state.Width,
                    Height = state.Height,
                    DockLocation = state.DockLocation
                });
            }
        }

        // Clear the snapshot after restoration
        pluginSnapshots.Remove(pluginId);
        return true;
    }

    /// <summary>
    /// Gets the saved snapshot for a plugin, if any.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The saved snapshot, or null if none exists.</returns>
    public PanelSnapshot? GetSnapshot(string pluginId)
    {
        return pluginSnapshots.GetValueOrDefault(pluginId);
    }

    /// <summary>
    /// Clears any saved snapshot for a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    public void ClearSnapshot(string pluginId)
    {
        pluginSnapshots.Remove(pluginId);
    }
}

/// <summary>
/// A snapshot of panel states for a plugin.
/// </summary>
public sealed class PanelSnapshot
{
    /// <summary>
    /// Gets the panel states by panel ID.
    /// </summary>
    public required IReadOnlyDictionary<string, PanelState> Panels { get; init; }
}

/// <summary>
/// The state of a single panel.
/// </summary>
public sealed class PanelState
{
    /// <summary>
    /// Gets whether the panel is open.
    /// </summary>
    public required bool IsOpen { get; init; }

    /// <summary>
    /// Gets the X position of the panel, if available.
    /// </summary>
    /// <remarks>
    /// This is typically only set for floating panels or panels
    /// that have been moved from their default dock location.
    /// </remarks>
    public float? X { get; init; }

    /// <summary>
    /// Gets the Y position of the panel, if available.
    /// </summary>
    public float? Y { get; init; }

    /// <summary>
    /// Gets the width of the panel, if it has been resized.
    /// </summary>
    public float? Width { get; init; }

    /// <summary>
    /// Gets the height of the panel, if it has been resized.
    /// </summary>
    public float? Height { get; init; }

    /// <summary>
    /// Gets the current dock location of the panel.
    /// </summary>
    public string? DockLocation { get; init; }
}

