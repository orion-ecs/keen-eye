using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Defines a complete visual theme for the UI system.
/// </summary>
/// <remarks>
/// <para>
/// A theme provides both a color palette and component-specific styles.
/// Component styles incorporate the palette colors with additional properties
/// like borders, corner radius, and padding.
/// </para>
/// <para>
/// Built-in themes (<see cref="SystemTheme.Light"/> and <see cref="SystemTheme.Dark"/>)
/// are provided by the theme plugin. Custom themes can be created by implementing
/// this interface and registering them with <see cref="IThemeContext.RegisterTheme"/>.
/// </para>
/// </remarks>
public interface ITheme
{
    /// <summary>
    /// Gets the display name of this theme.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether this is a light or dark theme.
    /// </summary>
    /// <remarks>
    /// This is used to determine the appropriate system theme variant and
    /// to select correct text contrast colors.
    /// </remarks>
    SystemTheme BaseTheme { get; }

    /// <summary>
    /// Gets the color palette for this theme.
    /// </summary>
    ColorPalette Colors { get; }

    /// <summary>
    /// Gets the style for button components.
    /// </summary>
    /// <param name="state">The current interaction state of the button.</param>
    /// <returns>A UIStyle configured for the button's current state.</returns>
    UIStyle GetButtonStyle(UIInteractionState state);

    /// <summary>
    /// Gets the style for panel/container components.
    /// </summary>
    /// <returns>A UIStyle configured for panels.</returns>
    UIStyle GetPanelStyle();

    /// <summary>
    /// Gets the style for text input components.
    /// </summary>
    /// <param name="state">The current interaction state of the input.</param>
    /// <returns>A UIStyle configured for the input's current state.</returns>
    UIStyle GetInputStyle(UIInteractionState state);

    /// <summary>
    /// Gets the style for menu components (dropdowns, context menus).
    /// </summary>
    /// <returns>A UIStyle configured for menus.</returns>
    UIStyle GetMenuStyle();

    /// <summary>
    /// Gets the style for menu item components.
    /// </summary>
    /// <param name="state">The current interaction state of the menu item.</param>
    /// <returns>A UIStyle configured for the menu item's current state.</returns>
    UIStyle GetMenuItemStyle(UIInteractionState state);

    /// <summary>
    /// Gets the style for modal/dialog components.
    /// </summary>
    /// <returns>A UIStyle configured for modals.</returns>
    UIStyle GetModalStyle();

    /// <summary>
    /// Gets the style for scrollbar track components.
    /// </summary>
    /// <returns>A UIStyle configured for scrollbar tracks.</returns>
    UIStyle GetScrollbarTrackStyle();

    /// <summary>
    /// Gets the style for scrollbar thumb components.
    /// </summary>
    /// <param name="state">The current interaction state of the scrollbar thumb.</param>
    /// <returns>A UIStyle configured for the thumb's current state.</returns>
    UIStyle GetScrollbarThumbStyle(UIInteractionState state);

    /// <summary>
    /// Gets the style for tooltip components.
    /// </summary>
    /// <returns>A UIStyle configured for tooltips.</returns>
    UIStyle GetTooltipStyle();
}
