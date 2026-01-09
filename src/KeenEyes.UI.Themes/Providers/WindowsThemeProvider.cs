using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using KeenEyes.UI.Themes.Abstractions;
using Microsoft.Win32;

namespace KeenEyes.UI.Themes.Providers;

/// <summary>
/// Windows implementation using registry watching for theme changes.
/// </summary>
/// <remarks>
/// Uses the Personalize registry key for maximum compatibility across Windows 10+.
/// Also detects Windows High Contrast mode for accessibility.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed partial class WindowsThemeProvider : ISystemThemeProvider
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    private SystemTheme currentTheme;
    private Thread? watcherThread;
    private volatile bool disposed;

    /// <inheritdoc />
    public bool SupportsRuntimeNotification => true;

    /// <inheritdoc />
    public bool IsAvailable { get; }

    /// <inheritdoc />
    public event Action<SystemThemeChangedEvent>? OnThemeChanged;

    public WindowsThemeProvider()
    {
        currentTheme = DetectTheme();
        IsAvailable = currentTheme != SystemTheme.Unknown;

        if (IsAvailable)
        {
            StartRegistryWatcher();
        }
    }

    /// <inheritdoc />
    public SystemTheme GetCurrentTheme() => currentTheme;

    private static SystemTheme DetectTheme()
    {
        // Check high contrast first (accessibility takes priority)
        if (IsHighContrastEnabled())
        {
            return SystemTheme.HighContrast;
        }

        // Read from registry
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            if (key is null)
            {
                return SystemTheme.Unknown;
            }

            var value = key.GetValue(AppsUseLightThemeValue);
            if (value is int intValue)
            {
                return intValue == 0 ? SystemTheme.Dark : SystemTheme.Light;
            }
        }
        catch
        {
            // Registry access failed - return unknown
        }

        return SystemTheme.Unknown;
    }

    private static bool IsHighContrastEnabled()
    {
        var info = new HighContrastInfo { cbSize = (uint)Marshal.SizeOf<HighContrastInfo>() };
        if (SystemParametersInfoW(SPI_GETHIGHCONTRAST, info.cbSize, ref info, 0))
        {
            return (info.dwFlags & HCF_HIGHCONTRASTON) != 0;
        }

        return false;
    }

    private void StartRegistryWatcher()
    {
        watcherThread = new Thread(RegistryWatchLoop)
        {
            IsBackground = true,
            Name = "KeenEyes-ThemeWatcher"
        };
        watcherThread.Start();
    }

    private void RegistryWatchLoop()
    {
        if (RegOpenKeyExW(HKEY_CURRENT_USER, PersonalizeKeyPath, 0, KEY_NOTIFY, out var hKey) != 0)
        {
            return;
        }

        var hEvent = CreateEventW(nint.Zero, false, false, null);
        if (hEvent == nint.Zero)
        {
            RegCloseKey(hKey);
            return;
        }

        try
        {
            while (!disposed)
            {
                // Register for notification on value changes
                if (RegNotifyChangeKeyValue(hKey, false, REG_NOTIFY_CHANGE_LAST_SET, hEvent, true) != 0)
                {
                    break;
                }

                // Wait for change (1 second timeout to check disposed flag)
                var result = WaitForSingleObject(hEvent, 1000);

                if (result == WAIT_OBJECT_0 && !disposed)
                {
                    var previousTheme = currentTheme;
                    currentTheme = DetectTheme();

                    if (previousTheme != currentTheme)
                    {
                        OnThemeChanged?.Invoke(new SystemThemeChangedEvent(previousTheme, currentTheme));
                    }
                }
            }
        }
        finally
        {
            CloseHandle(hEvent);
            RegCloseKey(hKey);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true;
        watcherThread?.Join(TimeSpan.FromSeconds(2));
    }

    // P/Invoke declarations for High Contrast detection
    private const uint SPI_GETHIGHCONTRAST = 0x0042;
    private const uint HCF_HIGHCONTRASTON = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct HighContrastInfo
    {
        public uint cbSize;
        public uint dwFlags;
        public nint lpszDefaultScheme;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfoW(
        uint uiAction, uint uiParam, ref HighContrastInfo pvParam, uint fWinIni);

    // P/Invoke declarations for registry watching
    private static readonly nint HKEY_CURRENT_USER = unchecked((nint)0x80000001);
    private const uint KEY_NOTIFY = 0x0010;
    private const uint REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;
    private const uint WAIT_OBJECT_0 = 0x00000000;

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial int RegOpenKeyExW(
        nint hKey, string lpSubKey, uint ulOptions, uint samDesired, out nint phkResult);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int RegNotifyChangeKeyValue(
        nint hKey,
        [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
        uint dwNotifyFilter,
        nint hEvent,
        [MarshalAs(UnmanagedType.Bool)] bool fAsynchronous);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int RegCloseKey(nint hKey);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateEventW(
        nint lpEventAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
        [MarshalAs(UnmanagedType.Bool)] bool bInitialState,
        string? lpName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint hObject);
}
