using KeenEyes.UI.Themes.Abstractions;
using KeenEyes.UI.Themes.Providers;
using KeenEyes.UI.Themes.Systems;
using KeenEyes.UI.Themes.Themes;

namespace KeenEyes.UI.Themes;

/// <summary>
/// Plugin that provides UI theming with OS theme detection.
/// </summary>
/// <remarks>
/// <para>
/// This plugin enables automatic theme switching based on OS preferences and provides
/// a theming system for UI components. It detects light/dark mode on Windows, macOS,
/// and Linux, and can automatically switch themes when the OS preference changes.
/// </para>
/// <para>
/// Built-in "Light" and "Dark" themes are registered automatically. Custom themes
/// can be registered via <see cref="IThemeContext.RegisterTheme"/>.
/// </para>
/// <para>
/// <b>Dependencies:</b> This plugin should be installed after <see cref="KeenEyes.UI.UIPlugin"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install plugins (order matters: UI before Themes)
/// world.InstallPlugin(new UIPlugin());
/// world.InstallPlugin(new ThemePlugin());
///
/// // Access theme context
/// var theme = world.GetExtension&lt;IThemeContext&gt;();
///
/// // Enable automatic OS theme following
/// theme.FollowSystemTheme = true;
///
/// // Or manually switch themes
/// theme.SetTheme("Dark");
///
/// // Create themed buttons
/// var button = world.Spawn()
///     .WithUIElement()
///     .WithUIRect(...)
///     .WithUIStyle(theme.CurrentTheme.GetButtonStyle(UIInteractionState.None))
///     .With(UIThemed.Button)
///     .Build();
/// </code>
/// </example>
public sealed class ThemePlugin : IWorldPlugin
{
    private ThemeContext? themeContext;
    private ISystemThemeProvider? systemThemeProvider;

    /// <inheritdoc />
    public string Name => "UI.Themes";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Create platform-specific theme provider
        systemThemeProvider = CreatePlatformProvider();

        // Create theme context
        themeContext = new ThemeContext(context.World, systemThemeProvider);

        // Register built-in themes
        themeContext.RegisterTheme("Light", new LightTheme());
        themeContext.RegisterTheme("Dark", new DarkTheme());

        // Set initial theme based on OS preference
        var systemTheme = systemThemeProvider.GetCurrentTheme();
        var initialThemeName = systemTheme == SystemTheme.Dark ? "Dark" : "Light";
        var initialTheme = themeContext.GetTheme(initialThemeName)!;
        themeContext.SetInitialTheme(initialTheme);

        // Expose context as extension
        context.SetExtension<IThemeContext>(themeContext);

        // Register UIThemed component
        context.RegisterComponent<UIThemed>();

        // Add theme applicator system (runs before UI layout)
        context.AddSystem<ThemeApplicatorSystem>(SystemPhase.LateUpdate, order: -20);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<IThemeContext>();
        themeContext?.Dispose();
        themeContext = null;
        systemThemeProvider = null;
    }

    private static ISystemThemeProvider CreatePlatformProvider()
    {
        // Use OperatingSystem.IsX() directly so CA1416 analyzer recognizes the platform guards
        if (OperatingSystem.IsWindows() && PlatformDetection.SupportsWindowsThemeDetection)
        {
            return new WindowsThemeProvider();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOSThemeProvider();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxThemeProvider();
        }

        return new FallbackThemeProvider();
    }
}
