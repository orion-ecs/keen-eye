using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes;

/// <summary>
/// Component that marks a UI entity for automatic theme styling.
/// </summary>
/// <remarks>
/// <para>
/// Entities with this component will have their <see cref="KeenEyes.UI.Abstractions.UIStyle"/>
/// automatically updated by the ThemeApplicatorSystem when the theme changes.
/// </para>
/// <para>
/// The <see cref="ComponentType"/> determines which style method is called on the theme
/// (e.g., <see cref="ITheme.GetButtonStyle"/> for <see cref="UIComponentType.Button"/>).
/// </para>
/// </remarks>
public struct UIThemed : IComponent
{
    /// <summary>
    /// The type of UI component for style resolution.
    /// </summary>
    public UIComponentType ComponentType;

    /// <summary>
    /// Creates a themed component marker for the specified component type.
    /// </summary>
    /// <param name="componentType">The type of UI component.</param>
    public static UIThemed For(UIComponentType componentType) => new() { ComponentType = componentType };

    /// <summary>
    /// Creates a themed button marker.
    /// </summary>
    public static UIThemed Button => new() { ComponentType = UIComponentType.Button };

    /// <summary>
    /// Creates a themed panel marker.
    /// </summary>
    public static UIThemed Panel => new() { ComponentType = UIComponentType.Panel };

    /// <summary>
    /// Creates a themed input marker.
    /// </summary>
    public static UIThemed Input => new() { ComponentType = UIComponentType.Input };

    /// <summary>
    /// Creates a themed menu marker.
    /// </summary>
    public static UIThemed Menu => new() { ComponentType = UIComponentType.Menu };

    /// <summary>
    /// Creates a themed menu item marker.
    /// </summary>
    public static UIThemed MenuItem => new() { ComponentType = UIComponentType.MenuItem };

    /// <summary>
    /// Creates a themed modal marker.
    /// </summary>
    public static UIThemed Modal => new() { ComponentType = UIComponentType.Modal };
}
