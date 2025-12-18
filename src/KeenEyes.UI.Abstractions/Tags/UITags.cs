namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Tag component marking an entity as a UI root (top-level canvas).
/// </summary>
/// <remarks>
/// <para>
/// Entities with this tag serve as the root of a UI hierarchy. The layout and render
/// systems traverse from root entities downward through the hierarchy.
/// </para>
/// <para>
/// A scene can have multiple UI roots for different purposes (e.g., game HUD, pause menu).
/// </para>
/// </remarks>
public struct UIRootTag : ITagComponent;

/// <summary>
/// Tag component marking a UI element as disabled.
/// </summary>
/// <remarks>
/// <para>
/// Disabled elements are visually rendered but do not respond to input.
/// Typically rendered with reduced opacity to indicate the disabled state.
/// </para>
/// <para>
/// Unlike <see cref="UIHiddenTag"/>, disabled elements still occupy layout space.
/// </para>
/// </remarks>
public struct UIDisabledTag : ITagComponent;

/// <summary>
/// Tag component marking a UI element as hidden (not rendered, no layout space).
/// </summary>
/// <remarks>
/// <para>
/// Hidden elements are completely removed from rendering and layout.
/// Use this for elements that should be temporarily invisible.
/// </para>
/// <para>
/// Unlike setting <c>UIElement.Visible = false</c>, this tag is cheaper to toggle
/// since it doesn't require modifying component data.
/// </para>
/// </remarks>
public struct UIHiddenTag : ITagComponent;

/// <summary>
/// Tag component marking a UI element as currently focused.
/// </summary>
/// <remarks>
/// <para>
/// Only one element in the UI hierarchy should have this tag at a time.
/// The UI focus system manages adding and removing this tag.
/// </para>
/// <para>
/// Focused elements receive keyboard input and are typically rendered with
/// a visual indicator (highlight, border, etc.).
/// </para>
/// </remarks>
public struct UIFocusedTag : ITagComponent;

/// <summary>
/// Tag component marking a UI element's layout as dirty (needs recalculation).
/// </summary>
/// <remarks>
/// <para>
/// Add this tag when an element's size, position, or children change.
/// The layout system will recalculate bounds for this element and its subtree.
/// </para>
/// <para>
/// This tag is automatically removed after layout calculation completes.
/// </para>
/// </remarks>
public struct UILayoutDirtyTag : ITagComponent;

/// <summary>
/// Tag component marking a UI element that should clip its children to its bounds.
/// </summary>
/// <remarks>
/// <para>
/// When present, child elements that extend beyond this element's bounds
/// are clipped (not rendered outside the bounds).
/// </para>
/// <para>
/// This is automatically applied to scrollable elements but can be used
/// independently for any container that needs clipping.
/// </para>
/// </remarks>
public struct UIClipChildrenTag : ITagComponent;
