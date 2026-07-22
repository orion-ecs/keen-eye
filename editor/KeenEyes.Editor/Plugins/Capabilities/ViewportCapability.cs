// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Plugins.Capabilities;

/// <summary>
/// Default in-memory implementation of <see cref="IViewportCapability"/>.
/// </summary>
/// <remarks>
/// <para>
/// Acts as the registry the editor exposes to plugins for viewport customization.
/// Plugins add gizmo renderers, overlays, and pick handlers here during initialization
/// and remove them during shutdown; the viewport render path enumerates the registered
/// entries to draw them.
/// </para>
/// </remarks>
internal sealed class ViewportCapability : IViewportCapability
{
    private readonly List<IGizmoRenderer> gizmoRenderers = [];
    private readonly Dictionary<string, IViewportOverlay> overlays = [];
    private readonly List<IPickHandler> pickHandlers = [];

    /// <inheritdoc />
    public event Action<IGizmoRenderer>? GizmoRendererAdded;

    /// <inheritdoc />
    public event Action<IGizmoRenderer>? GizmoRendererRemoved;

    /// <inheritdoc />
    public void AddGizmoRenderer(IGizmoRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (gizmoRenderers.Contains(renderer))
        {
            return;
        }

        gizmoRenderers.Add(renderer);
        GizmoRendererAdded?.Invoke(renderer);
    }

    /// <inheritdoc />
    public void RemoveGizmoRenderer(IGizmoRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (gizmoRenderers.Remove(renderer))
        {
            GizmoRendererRemoved?.Invoke(renderer);
        }
    }

    /// <inheritdoc />
    public IEnumerable<IGizmoRenderer> GetGizmoRenderers()
    {
        // Return a snapshot so callers can iterate while renderers mutate the registry.
        return gizmoRenderers.ToArray();
    }

    /// <inheritdoc />
    public void AddOverlay(string id, IViewportOverlay overlay)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(overlay);

        overlays[id] = overlay;
    }

    /// <inheritdoc />
    public bool RemoveOverlay(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        return overlays.Remove(id);
    }

    /// <inheritdoc />
    public void SetOverlayVisible(string id, bool visible)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        if (overlays.TryGetValue(id, out var overlay))
        {
            overlay.IsVisible = visible;
        }
    }

    /// <inheritdoc />
    public bool IsOverlayVisible(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        return overlays.TryGetValue(id, out var overlay) && overlay.IsVisible;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetOverlayIds()
    {
        return overlays.Keys.ToArray();
    }

    /// <inheritdoc />
    public void AddPickHandler(IPickHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (pickHandlers.Contains(handler))
        {
            return;
        }

        pickHandlers.Add(handler);
    }

    /// <inheritdoc />
    public void RemovePickHandler(IPickHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        pickHandlers.Remove(handler);
    }

    /// <inheritdoc />
    public IEnumerable<IPickHandler> GetPickHandlers()
    {
        // Highest priority first so the viewport can offer picks in precedence order.
        return pickHandlers.OrderByDescending(handler => handler.Priority).ToArray();
    }
}
