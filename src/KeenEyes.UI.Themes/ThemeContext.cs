using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes;

/// <summary>
/// Provides access to theme management and OS theme detection.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary interface for working with themes. Access it via
/// <c>world.GetExtension&lt;IThemeContext&gt;()</c> after installing the theme plugin.
/// </para>
/// </remarks>
[PluginExtension("UI.Themes")]
public sealed class ThemeContext : IThemeContext, IDisposable
{
    private readonly IWorld world;
    private readonly ISystemThemeProvider systemThemeProvider;
    private readonly Dictionary<string, ITheme> themes = new(StringComparer.OrdinalIgnoreCase);
    private ITheme currentTheme = null!;
    private bool followSystemTheme;
    private bool disposed;

    /// <inheritdoc />
    public ITheme CurrentTheme => currentTheme;

    /// <inheritdoc />
    public SystemTheme SystemTheme => systemThemeProvider.GetCurrentTheme();

    /// <inheritdoc />
    public bool FollowSystemTheme
    {
        get => followSystemTheme;
        set
        {
            if (followSystemTheme == value)
            {
                return;
            }

            followSystemTheme = value;

            if (value)
            {
                ApplySystemTheme();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> RegisteredThemes => themes.Keys;

    /// <inheritdoc />
    public event Action<ThemeChangedEvent>? OnThemeChanged;

    internal ThemeContext(IWorld world, ISystemThemeProvider systemThemeProvider)
    {
        this.world = world;
        this.systemThemeProvider = systemThemeProvider;

        if (systemThemeProvider.SupportsRuntimeNotification)
        {
            systemThemeProvider.OnThemeChanged += HandleSystemThemeChanged;
        }
    }

    /// <inheritdoc />
    public void SetTheme(ITheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        // Setting a theme manually disables follow system
        followSystemTheme = false;

        ApplyTheme(theme);
    }

    /// <inheritdoc />
    public bool SetTheme(string name)
    {
        if (!themes.TryGetValue(name, out var theme))
        {
            return false;
        }

        SetTheme(theme);
        return true;
    }

    /// <inheritdoc />
    public void RegisterTheme(string name, ITheme theme)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(theme);

        themes[name] = theme;
    }

    /// <inheritdoc />
    public ITheme? GetTheme(string name)
    {
        return themes.TryGetValue(name, out var theme) ? theme : null;
    }

    internal void SetInitialTheme(ITheme theme)
    {
        currentTheme = theme;
    }

    private void ApplyTheme(ITheme theme)
    {
        var previous = currentTheme;
        currentTheme = theme;

        if (previous != theme)
        {
            var evt = new ThemeChangedEvent(previous, theme);
            OnThemeChanged?.Invoke(evt);
            world.Send(evt);
        }
    }

    private void HandleSystemThemeChanged(SystemThemeChangedEvent evt)
    {
        if (!followSystemTheme)
        {
            return;
        }

        // Forward system theme change to ECS messaging
        world.Send(evt);

        ApplySystemTheme();
    }

    private void ApplySystemTheme()
    {
        var systemTheme = systemThemeProvider.GetCurrentTheme();
        var themeName = systemTheme switch
        {
            SystemTheme.Dark => "Dark",
            SystemTheme.HighContrast => "HighContrast",
            _ => "Light"
        };

        // Try to find a matching theme, fall back to Light/Dark if HighContrast not found
        if (!themes.TryGetValue(themeName, out var theme) && systemTheme == SystemTheme.HighContrast)
        {
            theme = themes.GetValueOrDefault("Dark") ?? themes.GetValueOrDefault("Light");
        }

        if (theme is not null && theme != currentTheme)
        {
            ApplyTheme(theme);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (systemThemeProvider.SupportsRuntimeNotification)
        {
            systemThemeProvider.OnThemeChanged -= HandleSystemThemeChanged;
        }

        systemThemeProvider.Dispose();
    }
}
