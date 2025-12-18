using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a dock container that manages dock zones.
/// </summary>
/// <remarks>
/// <para>
/// A dock container provides zones where panels can be docked (left, right, top, bottom, center).
/// Each zone can contain multiple panels in tabs. Zones are separated by splitters for resizing.
/// </para>
/// </remarks>
public struct UIDockContainer : IComponent
{
    /// <summary>
    /// The left dock zone entity.
    /// </summary>
    public Entity LeftZone;

    /// <summary>
    /// The right dock zone entity.
    /// </summary>
    public Entity RightZone;

    /// <summary>
    /// The top dock zone entity.
    /// </summary>
    public Entity TopZone;

    /// <summary>
    /// The bottom dock zone entity.
    /// </summary>
    public Entity BottomZone;

    /// <summary>
    /// The center zone entity (main content area).
    /// </summary>
    public Entity CenterZone;

    /// <summary>
    /// The currently dragging panel, if any.
    /// </summary>
    public Entity DraggingPanel;

    /// <summary>
    /// The dock preview overlay entity.
    /// </summary>
    public Entity PreviewOverlay;
}

/// <summary>
/// Component for a dock zone that can contain panels.
/// </summary>
/// <param name="zone">The zone type.</param>
public struct UIDockZone(DockZone zone) : IComponent
{
    /// <summary>
    /// Which zone this represents.
    /// </summary>
    public DockZone Zone = zone;

    /// <summary>
    /// Size of this zone in pixels.
    /// </summary>
    public float Size = 200f;

    /// <summary>
    /// Minimum size when resizing.
    /// </summary>
    public float MinSize = 100f;

    /// <summary>
    /// Whether the zone is collapsed.
    /// </summary>
    public bool IsCollapsed = false;

    /// <summary>
    /// The tab group entity for panels in this zone.
    /// </summary>
    public Entity TabGroup;

    /// <summary>
    /// The parent dock container.
    /// </summary>
    public Entity Container;
}

/// <summary>
/// Component for a dockable panel.
/// </summary>
/// <param name="title">The panel's title.</param>
public struct UIDockPanel(string title) : IComponent
{
    /// <summary>
    /// The panel's display title.
    /// </summary>
    public string Title = title;

    /// <summary>
    /// Current dock state.
    /// </summary>
    public DockState State = DockState.Floating;

    /// <summary>
    /// The zone this panel is currently in (if docked).
    /// </summary>
    public DockZone CurrentZone = DockZone.None;

    /// <summary>
    /// The dock container this panel belongs to.
    /// </summary>
    public Entity DockContainer = Entity.Null;

    /// <summary>
    /// Whether the panel can be closed.
    /// </summary>
    public bool CanClose = true;

    /// <summary>
    /// Whether the panel can float.
    /// </summary>
    public bool CanFloat = true;

    /// <summary>
    /// Whether the panel can be docked.
    /// </summary>
    public bool CanDock = true;

    /// <summary>
    /// Allowed dock zones for this panel.
    /// </summary>
    public DockZone AllowedZones = DockZone.All;

    /// <summary>
    /// The floating window position (when floating).
    /// </summary>
    public Vector2 FloatingPosition = Vector2.Zero;

    /// <summary>
    /// The floating window size (when floating).
    /// </summary>
    public Vector2 FloatingSize = new(300, 200);
}

/// <summary>
/// Component for a tab group within a dock zone.
/// </summary>
public struct UIDockTabGroup : IComponent
{
    /// <summary>
    /// Index of the currently selected tab.
    /// </summary>
    public int SelectedIndex;

    /// <summary>
    /// The dock zone this tab group belongs to.
    /// </summary>
    public Entity DockZone;

    /// <summary>
    /// Number of tabs in this group.
    /// </summary>
    public int TabCount;
}

/// <summary>
/// Component for a tab in a dock tab group.
/// </summary>
/// <param name="panel">The panel this tab represents.</param>
/// <param name="tabGroup">The tab group this tab belongs to.</param>
public struct UIDockTab(Entity panel, Entity tabGroup) : IComponent
{
    /// <summary>
    /// The panel this tab represents.
    /// </summary>
    public Entity Panel = panel;

    /// <summary>
    /// The tab group this tab belongs to.
    /// </summary>
    public Entity TabGroup = tabGroup;

    /// <summary>
    /// Index of this tab in the group.
    /// </summary>
    public int Index = 0;
}

/// <summary>
/// Tag for the dock preview overlay shown during drag.
/// </summary>
public struct UIDockPreviewTag : ITagComponent;

/// <summary>
/// Tag for panels currently being dragged.
/// </summary>
public struct UIDockDraggingTag : ITagComponent;
