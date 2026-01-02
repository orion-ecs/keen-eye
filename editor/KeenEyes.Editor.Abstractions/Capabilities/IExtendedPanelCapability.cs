// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Extended panel capability interface for querying and setting panel state.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IPanelCapability"/> to provide
/// position, size, and dock location state management needed for
/// hot reload state preservation.
/// </para>
/// <para>
/// Implementations should track panel state changes and persist them
/// across hot reloads. The <see cref="PanelStateStore"/> uses this
/// interface to capture and restore panel states.
/// </para>
/// </remarks>
public interface IExtendedPanelCapability : IPanelCapability
{
    /// <summary>
    /// Gets the extended state of a panel.
    /// </summary>
    /// <param name="panelId">The panel ID.</param>
    /// <returns>The extended panel state, or null if panel not found.</returns>
    ExtendedPanelState? GetPanelState(string panelId);

    /// <summary>
    /// Sets the extended state of a panel.
    /// </summary>
    /// <param name="panelId">The panel ID.</param>
    /// <param name="state">The state to apply.</param>
    void SetPanelState(string panelId, ExtendedPanelState state);
}

/// <summary>
/// Extended panel state including position, size, and dock location.
/// </summary>
public sealed class ExtendedPanelState
{
    /// <summary>
    /// Gets or sets the X position of the panel.
    /// </summary>
    public float? X { get; init; }

    /// <summary>
    /// Gets or sets the Y position of the panel.
    /// </summary>
    public float? Y { get; init; }

    /// <summary>
    /// Gets or sets the width of the panel.
    /// </summary>
    public float? Width { get; init; }

    /// <summary>
    /// Gets or sets the height of the panel.
    /// </summary>
    public float? Height { get; init; }

    /// <summary>
    /// Gets or sets the dock location of the panel.
    /// </summary>
    public string? DockLocation { get; init; }
}
