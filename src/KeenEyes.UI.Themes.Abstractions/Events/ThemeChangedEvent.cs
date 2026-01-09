namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Event raised when the application theme changes.
/// </summary>
/// <param name="PreviousTheme">The theme before the change, or null if this is the initial theme.</param>
/// <param name="NewTheme">The new active theme.</param>
public readonly record struct ThemeChangedEvent(
    ITheme? PreviousTheme,
    ITheme NewTheme);
