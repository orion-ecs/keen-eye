namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Provides access to the operating system's theme preference.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts platform-specific theme detection mechanisms.
/// Implementations exist for Windows (registry/UISettings), macOS (NSApplication.effectiveAppearance),
/// and Linux (XDG portal or gsettings).
/// </para>
/// <para>
/// The provider supports both polling and event-driven notification models.
/// Call <see cref="GetCurrentTheme"/> for immediate queries, or subscribe to
/// <see cref="OnThemeChanged"/> for runtime notifications.
/// </para>
/// </remarks>
public interface ISystemThemeProvider : IDisposable
{
    /// <summary>
    /// Gets whether theme detection is available on this platform.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> on unsupported platforms or when detection fails.
    /// When <c>false</c>, <see cref="GetCurrentTheme"/> returns <see cref="SystemTheme.Unknown"/>.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets whether this provider supports runtime theme change notifications.
    /// </summary>
    /// <remarks>
    /// When <c>false</c>, the provider can only detect the theme at startup.
    /// Use polling-based detection by periodically calling <see cref="GetCurrentTheme"/>.
    /// </remarks>
    bool SupportsRuntimeNotification { get; }

    /// <summary>
    /// Gets the current operating system theme.
    /// </summary>
    /// <returns>
    /// The current theme, or <see cref="SystemTheme.Unknown"/> if detection is unavailable.
    /// </returns>
    SystemTheme GetCurrentTheme();

    /// <summary>
    /// Event raised when the operating system theme changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is only raised when <see cref="SupportsRuntimeNotification"/> is <c>true</c>.
    /// On platforms without runtime support, this event will never fire.
    /// </para>
    /// <para>
    /// Handlers are invoked on an unspecified thread. UI updates should be marshaled
    /// to the appropriate thread if necessary.
    /// </para>
    /// </remarks>
    event Action<SystemThemeChangedEvent>? OnThemeChanged;
}
