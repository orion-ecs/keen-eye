namespace KeenEyes.UI.Themes.Providers;

/// <summary>
/// Provides runtime platform detection for theme provider selection.
/// </summary>
internal static class PlatformDetection
{
    /// <summary>
    /// Gets whether the current platform is Windows.
    /// </summary>
    public static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>
    /// Gets whether the current platform is macOS.
    /// </summary>
    public static bool IsMacOS => OperatingSystem.IsMacOS();

    /// <summary>
    /// Gets whether the current platform is Linux.
    /// </summary>
    public static bool IsLinux => OperatingSystem.IsLinux();

    /// <summary>
    /// Gets the Windows build number for feature detection.
    /// </summary>
    /// <remarks>
    /// Returns 0 on non-Windows platforms. Theme detection requires Windows 10 (build 10240+).
    /// </remarks>
    public static int WindowsBuildNumber
    {
        get
        {
            if (!IsWindows)
            {
                return 0;
            }

            return Environment.OSVersion.Version.Build;
        }
    }

    /// <summary>
    /// Gets whether Windows theme APIs are available (Windows 10+).
    /// </summary>
    public static bool SupportsWindowsThemeDetection => IsWindows && WindowsBuildNumber >= 10240;
}
