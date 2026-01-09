namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Event raised when the operating system's theme preference changes.
/// </summary>
/// <param name="PreviousTheme">The theme before the change.</param>
/// <param name="CurrentTheme">The new active theme.</param>
public readonly record struct SystemThemeChangedEvent(
    SystemTheme PreviousTheme,
    SystemTheme CurrentTheme);
