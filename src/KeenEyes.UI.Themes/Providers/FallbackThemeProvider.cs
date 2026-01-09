using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes.Providers;

/// <summary>
/// Fallback provider for platforms where theme detection is not available.
/// </summary>
/// <remarks>
/// This provider always returns <see cref="SystemTheme.Unknown"/> and never fires
/// change notifications. It is used on unsupported platforms or when all platform-specific
/// detection methods fail.
/// </remarks>
internal sealed class FallbackThemeProvider : ISystemThemeProvider
{
    /// <inheritdoc />
    public bool SupportsRuntimeNotification => false;

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is never used - required by interface, but fallback never fires it
    public event Action<SystemThemeChangedEvent>? OnThemeChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public SystemTheme GetCurrentTheme() => SystemTheme.Unknown;

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose
    }
}
