using System.Diagnostics;
using System.Runtime.Versioning;
using KeenEyes.UI.Themes.Abstractions;

namespace KeenEyes.UI.Themes.Providers;

/// <summary>
/// Linux implementation using XDG portal D-Bus interface or gsettings fallback.
/// </summary>
/// <remarks>
/// <para>
/// Attempts detection in this order:
/// 1. XDG Desktop Portal (org.freedesktop.portal.Settings) - works across DEs
/// 2. gsettings (GNOME/GTK applications)
/// 3. KDE global configuration file
/// </para>
/// <para>
/// Change detection uses dbus-monitor when available, otherwise falls back to polling.
/// </para>
/// </remarks>
[SupportedOSPlatform("linux")]
internal sealed class LinuxThemeProvider : ISystemThemeProvider
{
    private const int PollingIntervalMs = 1000;

    private SystemTheme currentTheme;
    private Thread? watcherThread;
    private Process? dbusMonitorProcess;
    private volatile bool disposed;

    /// <inheritdoc />
    public bool SupportsRuntimeNotification { get; private set; }

    /// <inheritdoc />
    public bool IsAvailable { get; }

    /// <inheritdoc />
    public event Action<SystemThemeChangedEvent>? OnThemeChanged;

    public LinuxThemeProvider()
    {
        currentTheme = DetectTheme();
        IsAvailable = currentTheme != SystemTheme.Unknown;

        if (IsAvailable)
        {
            // Try to start dbus-monitor for efficient change detection
            if (TryStartDbusMonitor())
            {
                SupportsRuntimeNotification = true;
            }
            else
            {
                // Fall back to polling
                SupportsRuntimeNotification = true;
                StartPolling();
            }
        }
    }

    /// <inheritdoc />
    public SystemTheme GetCurrentTheme() => currentTheme;

    private static SystemTheme DetectTheme()
    {
        // Try XDG portal first (most universal)
        var xdgTheme = TryXdgPortal();
        if (xdgTheme != SystemTheme.Unknown)
        {
            return xdgTheme;
        }

        // Try gsettings (GNOME)
        var gsettingsTheme = TryGsettings();
        if (gsettingsTheme != SystemTheme.Unknown)
        {
            return gsettingsTheme;
        }

        // Try KDE settings
        var kdeTheme = TryKde();
        if (kdeTheme != SystemTheme.Unknown)
        {
            return kdeTheme;
        }

        return SystemTheme.Unknown;
    }

    private static SystemTheme TryXdgPortal()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dbus-send",
                Arguments = "--session --print-reply --dest=org.freedesktop.portal.Desktop " +
                    "/org/freedesktop/portal/desktop org.freedesktop.portal.Settings.Read " +
                    "string:org.freedesktop.appearance string:color-scheme",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return SystemTheme.Unknown;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000);

            if (process.ExitCode != 0)
            {
                return SystemTheme.Unknown;
            }

            // XDG portal color-scheme values:
            // 0 = no preference (treat as light)
            // 1 = prefer dark
            // 2 = prefer light
            if (output.Contains("uint32 1"))
            {
                return SystemTheme.Dark;
            }

            if (output.Contains("uint32 2") || output.Contains("uint32 0"))
            {
                return SystemTheme.Light;
            }
        }
        catch
        {
            // dbus-send not available or failed
        }

        return SystemTheme.Unknown;
    }

    private static SystemTheme TryGsettings()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "gsettings",
                Arguments = "get org.gnome.desktop.interface color-scheme",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return SystemTheme.Unknown;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(1000);

            if (process.ExitCode != 0)
            {
                return SystemTheme.Unknown;
            }

            // Values: 'default', 'prefer-dark', 'prefer-light'
            if (output.Contains("prefer-dark", StringComparison.OrdinalIgnoreCase))
            {
                return SystemTheme.Dark;
            }

            if (output.Contains("prefer-light", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("default", StringComparison.OrdinalIgnoreCase))
            {
                return SystemTheme.Light;
            }
        }
        catch
        {
            // gsettings not available or failed
        }

        return SystemTheme.Unknown;
    }

    private static SystemTheme TryKde()
    {
        try
        {
            // Read KDE global config
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config/kdeglobals");

            if (!File.Exists(configPath))
            {
                return SystemTheme.Unknown;
            }

            var content = File.ReadAllText(configPath);

            // Look for LookAndFeelPackage or ColorScheme containing "dark"
            if (content.Contains("dark", StringComparison.OrdinalIgnoreCase))
            {
                return SystemTheme.Dark;
            }

            // If we can read the file but no dark indicator, assume light
            return SystemTheme.Light;
        }
        catch
        {
            // File read failed
        }

        return SystemTheme.Unknown;
    }

    private bool TryStartDbusMonitor()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dbus-monitor",
                Arguments = "--session type=signal,interface=org.freedesktop.portal.Settings",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            dbusMonitorProcess = Process.Start(startInfo);
            if (dbusMonitorProcess is null)
            {
                return false;
            }

            watcherThread = new Thread(() =>
            {
                try
                {
                    while (!disposed && dbusMonitorProcess is { HasExited: false })
                    {
                        var line = dbusMonitorProcess.StandardOutput.ReadLine();
                        if (line is null)
                        {
                            continue;
                        }

                        // Look for color-scheme signals
                        if (line.Contains("color-scheme", StringComparison.OrdinalIgnoreCase))
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
                catch
                {
                    // Monitor process exited or read failed
                }
            })
            {
                IsBackground = true,
                Name = "KeenEyes-LinuxDbusMonitor"
            };
            watcherThread.Start();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void StartPolling()
    {
        watcherThread = new Thread(() =>
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
            Name = "KeenEyes-LinuxThemePoller"
        };
        watcherThread.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true;

        try
        {
            dbusMonitorProcess?.Kill();
            dbusMonitorProcess?.Dispose();
        }
        catch
        {
            // Process already exited
        }

        watcherThread?.Join(TimeSpan.FromSeconds(2));
    }
}
