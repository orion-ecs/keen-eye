namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Represents the operating system's color theme preference.
/// </summary>
public enum SystemTheme
{
    /// <summary>
    /// Theme could not be detected (unsupported platform or detection failed).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The system is using a light color scheme.
    /// </summary>
    Light = 1,

    /// <summary>
    /// The system is using a dark color scheme.
    /// </summary>
    Dark = 2,

    /// <summary>
    /// The system has high contrast mode enabled.
    /// </summary>
    /// <remarks>
    /// On Windows, this indicates High Contrast mode is active.
    /// On other platforms, this may indicate an accessibility theme.
    /// </remarks>
    HighContrast = 3
}
