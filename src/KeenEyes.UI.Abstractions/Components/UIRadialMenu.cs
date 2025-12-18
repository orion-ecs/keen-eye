using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a radial (pie) menu, typically used with gamepads.
/// </summary>
/// <remarks>
/// <para>
/// Radial menus present options in a circular layout around a center point.
/// Users select options by moving a thumbstick or mouse in the direction of the desired slice.
/// </para>
/// <para>
/// The menu can be opened with a trigger (e.g., holding a button) and selection
/// is made when the trigger is released while hovering over a slice.
/// </para>
/// </remarks>
/// <param name="sliceCount">Number of slices in the menu.</param>
public struct UIRadialMenu(int sliceCount) : IComponent
{
    /// <summary>
    /// Whether the menu is currently visible.
    /// </summary>
    public bool IsOpen = false;

    /// <summary>
    /// The currently highlighted slice index (-1 for none, e.g., in dead zone).
    /// </summary>
    public int SelectedIndex = -1;

    /// <summary>
    /// Inner radius defining the dead zone (no selection).
    /// </summary>
    public float InnerRadius = 40f;

    /// <summary>
    /// Outer radius of the menu.
    /// </summary>
    public float OuterRadius = 120f;

    /// <summary>
    /// Number of slices in the menu.
    /// </summary>
    public int SliceCount = sliceCount;

    /// <summary>
    /// Animation progress for opening (0 = closed, 1 = fully open).
    /// </summary>
    public float OpenProgress = 0f;

    /// <summary>
    /// Angle offset for the first slice (radians, 0 = right).
    /// </summary>
    public float StartAngle = -MathF.PI / 2;  // Start at top

    /// <summary>
    /// The screen position where the menu is centered.
    /// </summary>
    public Vector2 CenterPosition = Vector2.Zero;
}

/// <summary>
/// Component for an individual slice in a radial menu.
/// </summary>
/// <param name="radialMenu">The radial menu this slice belongs to.</param>
/// <param name="index">Index of this slice in the menu (0-based).</param>
public struct UIRadialSlice(Entity radialMenu, int index) : IComponent
{
    /// <summary>
    /// The radial menu this slice belongs to.
    /// </summary>
    public Entity RadialMenu = radialMenu;

    /// <summary>
    /// Index of this slice in the menu (0-based).
    /// </summary>
    public int Index = index;

    /// <summary>
    /// Starting angle of this slice (radians).
    /// </summary>
    public float StartAngle = 0;

    /// <summary>
    /// Ending angle of this slice (radians).
    /// </summary>
    public float EndAngle = 0;

    /// <summary>
    /// Whether this slice opens a submenu.
    /// </summary>
    public bool HasSubmenu = false;

    /// <summary>
    /// Whether this slice is enabled.
    /// </summary>
    public bool IsEnabled = true;

    /// <summary>
    /// Display label for the slice.
    /// </summary>
    public string Label = "";

    /// <summary>
    /// Unique identifier for event handling.
    /// </summary>
    public string ItemId = "";

    /// <summary>
    /// Reference to the submenu (if HasSubmenu is true).
    /// </summary>
    public Entity Submenu = Entity.Null;
}

/// <summary>
/// State tracking for radial menu input.
/// </summary>
public struct UIRadialMenuInputState : IComponent
{
    /// <summary>
    /// Current input direction (normalized, from thumbstick or mouse).
    /// </summary>
    public Vector2 InputDirection;

    /// <summary>
    /// Distance from center (0-1, mapped to inner/outer radius).
    /// </summary>
    public float InputMagnitude;

    /// <summary>
    /// Whether the menu trigger is currently held.
    /// </summary>
    public bool IsTriggerHeld;

    /// <summary>
    /// Time the menu has been open (for animations).
    /// </summary>
    public float OpenTime;
}

/// <summary>
/// Tag for a radial menu that is currently open.
/// </summary>
public struct UIRadialMenuOpenTag : ITagComponent;

/// <summary>
/// Tag for a currently selected/highlighted radial slice.
/// </summary>
public struct UIRadialSliceSelectedTag : ITagComponent;
