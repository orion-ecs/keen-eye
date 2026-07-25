namespace KeenEyes.Tests;

/// <summary>
/// Tests for the order in which plugins are torn down.
/// </summary>
/// <remarks>
/// Plugins declare dependencies by requiring an extension to exist at install time, so a
/// dependent must be uninstalled while its dependency is still alive. Regression coverage for
/// #1256, where the window plugin was uninstalled before the graphics plugin that renders
/// through it and the graphics teardown then ran against a destroyed OpenGL context.
/// </remarks>
public class PluginUninstallOrderTests
{
    /// <summary>
    /// A dependency plugin: publishes a service other plugins build on.
    /// </summary>
    private sealed class HostPlugin(List<string> log) : IWorldPlugin
    {
        public string Name => "Host";

        public void Install(IPluginContext context)
        {
            context.SetExtension(new HostService());
        }

        public void Uninstall(IPluginContext context)
        {
            log.Add("Host");
            context.RemoveExtension<HostService>();
        }
    }

    /// <summary>
    /// A dependent plugin: requires the host service at install time and needs it again to
    /// release its own resources, exactly like a graphics plugin releasing GPU objects
    /// through the window that owns the context.
    /// </summary>
    private sealed class DependentPlugin(List<string> log) : IWorldPlugin
    {
        public string Name => "Dependent";

        public void Install(IPluginContext context)
        {
            if (!context.TryGetExtension<HostService>(out _))
            {
                throw new InvalidOperationException("DependentPlugin requires HostPlugin.");
            }
        }

        public void Uninstall(IPluginContext context)
        {
            log.Add("Dependent");

            if (!context.TryGetExtension<HostService>(out var host) || host is null)
            {
                throw new InvalidOperationException(
                    "DependentPlugin was uninstalled after HostPlugin, so its resources leaked.");
            }

            host.ReleaseResources();
        }
    }

    private sealed class HostService
    {
        public int ReleaseCount { get; private set; }

        public void ReleaseResources() => ReleaseCount++;
    }

    [Fact]
    public void Dispose_WithDependentPlugins_UninstallsInReverseInstallationOrder()
    {
        var log = new List<string>();
        var world = new World();
        world.InstallPlugin(new HostPlugin(log));
        world.InstallPlugin(new DependentPlugin(log));

        world.Dispose();

        Assert.Equal(["Dependent", "Host"], log);
    }

    [Fact]
    public void Dispose_WithDependentPlugins_DoesNotThrowFromTeardown()
    {
        var log = new List<string>();
        var world = new World();
        world.InstallPlugin(new HostPlugin(log));
        world.InstallPlugin(new DependentPlugin(log));

        var exception = Record.Exception(world.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WithDependentPlugins_LetsDependentReleaseThroughItsDependency()
    {
        var log = new List<string>();
        var world = new World();
        world.InstallPlugin(new HostPlugin(log));
        world.InstallPlugin(new DependentPlugin(log));
        var host = world.GetExtension<HostService>();

        world.Dispose();

        Assert.Equal(1, host.ReleaseCount);
    }

    [Fact]
    public void UninstallPlugin_ThenDispose_UninstallsRemainingPluginsOnce()
    {
        var log = new List<string>();
        using var world = new World();
        world.InstallPlugin(new HostPlugin(log));
        world.InstallPlugin(new DependentPlugin(log));

        // Explicitly uninstalling the dependent first is the caller doing the ordering by
        // hand; the automatic teardown must not try to uninstall it a second time.
        world.UninstallPlugin<DependentPlugin>();
        world.Dispose();

        Assert.Equal(["Dependent", "Host"], log);
    }
}
