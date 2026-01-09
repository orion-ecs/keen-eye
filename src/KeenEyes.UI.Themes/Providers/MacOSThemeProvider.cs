using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes.Providers;

/// <summary>
/// macOS implementation using NSApplication.effectiveAppearance.
/// </summary>
/// <remarks>
/// <para>
/// Uses Objective-C runtime P/Invoke to query NSApplication for the effective appearance.
/// Supports both "NSAppearanceNameDarkAqua" (dark mode) and "NSAppearanceNameAqua" (light mode).
/// </para>
/// <para>
/// Change detection uses polling since KVO blocks require complex Objective-C interop.
/// The polling interval is 500ms to balance responsiveness with resource usage.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed partial class MacOSThemeProvider : ISystemThemeProvider
{
    private const int PollingIntervalMs = 500;

    private SystemTheme currentTheme;
    private Thread? pollingThread;
    private volatile bool disposed;

    /// <inheritdoc />
    public bool SupportsRuntimeNotification => true;

    /// <inheritdoc />
    public bool IsAvailable { get; }

    /// <inheritdoc />
    public event Action<SystemThemeChangedEvent>? OnThemeChanged;

    public MacOSThemeProvider()
    {
        try
        {
            currentTheme = DetectTheme();
            IsAvailable = currentTheme != SystemTheme.Unknown;

            if (IsAvailable)
            {
                StartPolling();
            }
        }
        catch
        {
            IsAvailable = false;
            currentTheme = SystemTheme.Unknown;
        }
    }

    /// <inheritdoc />
    public SystemTheme GetCurrentTheme() => currentTheme;

    private static SystemTheme DetectTheme()
    {
        try
        {
            // Get NSApplication class
            var nsAppClass = objc_getClass("NSApplication"u8);
            if (nsAppClass == nint.Zero)
            {
                return SystemTheme.Unknown;
            }

            // Get [NSApplication sharedApplication]
            var sharedAppSel = sel_registerName("sharedApplication"u8);
            var sharedApp = objc_msgSend(nsAppClass, sharedAppSel);
            if (sharedApp == nint.Zero)
            {
                return SystemTheme.Unknown;
            }

            // Get effectiveAppearance
            var effectiveAppearanceSel = sel_registerName("effectiveAppearance"u8);
            var appearance = objc_msgSend(sharedApp, effectiveAppearanceSel);
            if (appearance == nint.Zero)
            {
                return SystemTheme.Unknown;
            }

            // Get appearance name
            var nameSel = sel_registerName("name"u8);
            var name = objc_msgSend(appearance, nameSel);
            if (name == nint.Zero)
            {
                return SystemTheme.Unknown;
            }

            // Convert NSString to managed string
            var nameStr = GetNSStringValue(name);

            // Check for dark mode variants
            if (nameStr.Contains("Dark", StringComparison.OrdinalIgnoreCase))
            {
                return SystemTheme.Dark;
            }

            // Check for high contrast
            if (nameStr.Contains("HighContrast", StringComparison.OrdinalIgnoreCase) ||
                nameStr.Contains("Accessibility", StringComparison.OrdinalIgnoreCase))
            {
                return SystemTheme.HighContrast;
            }

            // Default to light for "Aqua" and other variants
            return SystemTheme.Light;
        }
        catch
        {
            return SystemTheme.Unknown;
        }
    }

    private static string GetNSStringValue(nint nsString)
    {
        var utf8StringSel = sel_registerName("UTF8String"u8);
        var utf8Ptr = objc_msgSend(nsString, utf8StringSel);
        return utf8Ptr != nint.Zero ? Marshal.PtrToStringUTF8(utf8Ptr) ?? string.Empty : string.Empty;
    }

    private void StartPolling()
    {
        pollingThread = new Thread(() =>
        {
            while (!disposed)
            {
                Thread.Sleep(PollingIntervalMs);

                if (disposed)
                {
                    break;
                }

                var previousTheme = currentTheme;
                currentTheme = DetectTheme();

                if (previousTheme != currentTheme)
                {
                    OnThemeChanged?.Invoke(new SystemThemeChangedEvent(previousTheme, currentTheme));
                }
            }
        })
        {
            IsBackground = true,
            Name = "KeenEyes-MacOSThemePoller"
        };
        pollingThread.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true;
        pollingThread?.Join(TimeSpan.FromSeconds(2));
    }

    // Objective-C runtime P/Invoke declarations
    [LibraryImport("libobjc.dylib")]
    private static partial nint objc_getClass(ReadOnlySpan<byte> name);

    [LibraryImport("libobjc.dylib")]
    private static partial nint sel_registerName(ReadOnlySpan<byte> name);

    [LibraryImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend(nint receiver, nint selector);
}
