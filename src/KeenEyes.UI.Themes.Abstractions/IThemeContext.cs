namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Provides access to theme management and OS theme detection.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary interface for working with themes. Access it via
/// <c>world.GetExtension&lt;IThemeContext&gt;()</c> after installing the theme plugin.
/// </para>
/// <para>
/// The context supports automatic theme switching based on OS preference when
/// <see cref="FollowSystemTheme"/> is enabled. When enabled, theme changes from
/// <see cref="ISystemThemeProvider"/> automatically trigger the appropriate
/// built-in theme.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var theme = world.GetExtension&lt;IThemeContext&gt;();
///
/// // Get current theme
/// var isDark = theme.CurrentTheme.BaseTheme == SystemTheme.Dark;
///
/// // Switch themes manually
/// theme.SetTheme(theme.GetTheme("Dark")!);
///
/// // Or follow system preference
/// theme.FollowSystemTheme = true;
/// </code>
/// </example>
public interface IThemeContext
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    ITheme CurrentTheme { get; }

    /// <summary>
    /// Gets the current OS system theme preference.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="SystemTheme.Unknown"/> if OS theme detection is not available.
    /// </remarks>
    SystemTheme SystemTheme { get; }

    /// <summary>
    /// Gets or sets whether the application should automatically follow the OS theme.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, theme changes are automatic based on OS preference.
    /// The built-in "Light" and "Dark" themes are selected based on
    /// <see cref="ISystemThemeProvider.GetCurrentTheme"/>.
    /// </para>
    /// <para>
    /// When <c>false</c>, themes must be changed manually via <see cref="SetTheme(ITheme)"/>.
    /// </para>
    /// </remarks>
    bool FollowSystemTheme { get; set; }

    /// <summary>
    /// Sets the active theme.
    /// </summary>
    /// <param name="theme">The theme to activate.</param>
    /// <remarks>
    /// Setting a theme manually sets <see cref="FollowSystemTheme"/> to <c>false</c>.
    /// </remarks>
    void SetTheme(ITheme theme);

    /// <summary>
    /// Sets the active theme by name.
    /// </summary>
    /// <param name="name">The name of a registered theme.</param>
    /// <returns><c>true</c> if the theme was found and activated; otherwise <c>false</c>.</returns>
    bool SetTheme(string name);

    /// <summary>
    /// Registers a custom theme.
    /// </summary>
    /// <param name="name">A unique name for the theme.</param>
    /// <param name="theme">The theme to register.</param>
    /// <remarks>
    /// Built-in "Light" and "Dark" themes are registered automatically.
    /// Custom themes can override built-in themes by using the same name.
    /// </remarks>
    void RegisterTheme(string name, ITheme theme);

    /// <summary>
    /// Gets a registered theme by name.
    /// </summary>
    /// <param name="name">The name of the theme to retrieve.</param>
    /// <returns>The theme, or <c>null</c> if not found.</returns>
    ITheme? GetTheme(string name);

    /// <summary>
    /// Gets all registered theme names.
    /// </summary>
    IReadOnlyCollection<string> RegisteredThemes { get; }

    /// <summary>
    /// Event raised when the active theme changes.
    /// </summary>
    event Action<ThemeChangedEvent>? OnThemeChanged;
}
