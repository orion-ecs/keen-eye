using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Simple benchmark plugin that registers one system.
/// </summary>
public class SimplePlugin : IWorldPlugin
{
    public string Name => "Simple";

    public void Install(PluginContext context)
    {
        context.AddSystem<MovementSystem>(SystemPhase.Update);
    }

    public void Uninstall(PluginContext context)
    {
        // Systems auto-removed
    }
}

/// <summary>
/// Complex plugin that registers multiple systems and an extension.
/// </summary>
public class ComplexPlugin : IWorldPlugin
{
    public string Name => "Complex";

    public void Install(PluginContext context)
    {
        context.AddSystem<MovementSystem>(SystemPhase.Update, order: 0);
        context.AddSystem<HealthDecaySystem>(SystemPhase.Update, order: 10);
        context.AddSystem<RotationSystem>(SystemPhase.Update, order: 20);
        context.SetExtension(new PluginStats());
    }

    public void Uninstall(PluginContext context)
    {
        context.RemoveExtension<PluginStats>();
    }
}

/// <summary>
/// Extension class for benchmark plugin.
/// </summary>
public class PluginStats
{
    public int InstallCount { get; set; }
    public int UpdateCount { get; set; }
}

/// <summary>
/// Benchmarks for plugin installation, uninstallation, and queries.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class PluginInstallBenchmarks
{
    private World world = null!;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of installing a simple plugin (1 system).
    /// </summary>
    [Benchmark]
    public World InstallSimplePlugin()
    {
        world.InstallPlugin<SimplePlugin>();
        world.UninstallPlugin<SimplePlugin>();
        return world;
    }

    /// <summary>
    /// Measures the cost of installing a complex plugin (3 systems + extension).
    /// </summary>
    [Benchmark]
    public World InstallComplexPlugin()
    {
        world.InstallPlugin<ComplexPlugin>();
        world.UninstallPlugin<ComplexPlugin>();
        return world;
    }

    /// <summary>
    /// Measures the cost of checking plugin existence.
    /// </summary>
    [Benchmark]
    public bool HasPlugin()
    {
        world.InstallPlugin<SimplePlugin>();
        var result = world.HasPlugin<SimplePlugin>();
        world.UninstallPlugin<SimplePlugin>();
        return result;
    }

    /// <summary>
    /// Measures the cost of retrieving a plugin by type.
    /// </summary>
    [Benchmark]
    public IWorldPlugin? GetPlugin()
    {
        world.InstallPlugin<SimplePlugin>();
        var plugin = world.GetPlugin<SimplePlugin>();
        world.UninstallPlugin<SimplePlugin>();
        return plugin;
    }
}

/// <summary>
/// Benchmarks for WorldBuilder plugin configuration.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class WorldBuilderPluginBenchmarks
{
    /// <summary>
    /// Measures the cost of building a world with no plugins.
    /// </summary>
    [Benchmark(Baseline = true)]
    public World BuildWithNoPlugins()
    {
        var world = new WorldBuilder().Build();
        world.Dispose();
        return world;
    }

    /// <summary>
    /// Measures the cost of building a world with one plugin.
    /// </summary>
    [Benchmark]
    public World BuildWithOnePlugin()
    {
        var world = new WorldBuilder()
            .WithPlugin<SimplePlugin>()
            .Build();
        world.Dispose();
        return world;
    }

    /// <summary>
    /// Measures the cost of building a world with multiple plugins.
    /// </summary>
    [Benchmark]
    public World BuildWithMultiplePlugins()
    {
        var world = new WorldBuilder()
            .WithPlugin<SimplePlugin>()
            .WithPlugin<ComplexPlugin>()
            .Build();
        world.Dispose();
        return world;
    }
}

/// <summary>
/// Benchmarks for extension API performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ExtensionBenchmarks
{
    private World world = null!;
    private PluginStats stats = null!;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        stats = new PluginStats();
        world.SetExtension(stats);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of setting an extension.
    /// </summary>
    [Benchmark]
    public void SetExtension()
    {
        world.SetExtension(new PluginStats());
    }

    /// <summary>
    /// Measures the cost of getting an extension.
    /// </summary>
    [Benchmark]
    public PluginStats GetExtension()
    {
        return world.GetExtension<PluginStats>();
    }

    /// <summary>
    /// Measures the cost of TryGetExtension.
    /// </summary>
    [Benchmark]
    public bool TryGetExtension()
    {
        return world.TryGetExtension<PluginStats>(out _);
    }

    /// <summary>
    /// Measures the cost of checking extension existence.
    /// </summary>
    [Benchmark]
    public bool HasExtension()
    {
        return world.HasExtension<PluginStats>();
    }
}

/// <summary>
/// Benchmarks for plugin enumeration.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class PluginEnumerationBenchmarks
{
    private World world = null!;

    [Params(1, 5, 10)]
    public int PluginCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Install numbered plugins
        for (var i = 0; i < PluginCount; i++)
        {
            world.InstallPlugin(new NumberedPlugin(i));
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of enumerating all installed plugins.
    /// </summary>
    [Benchmark]
    public int EnumeratePlugins()
    {
        var count = 0;
        foreach (var _ in world.GetPlugins())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures the cost of checking plugin existence by name.
    /// </summary>
    [Benchmark]
    public bool HasPluginByName()
    {
        return world.HasPlugin("Plugin_0");
    }

    /// <summary>
    /// Measures the cost of getting plugin by name.
    /// </summary>
    [Benchmark]
    public IWorldPlugin? GetPluginByName()
    {
        return world.GetPlugin("Plugin_0");
    }
}

/// <summary>
/// Numbered plugin for enumeration benchmarks.
/// </summary>
public class NumberedPlugin : IWorldPlugin
{
    private readonly int number;

    public NumberedPlugin(int number)
    {
        this.number = number;
    }

    public string Name => $"Plugin_{number}";

    public void Install(PluginContext context)
    {
        // Minimal install
    }

    public void Uninstall(PluginContext context)
    {
        // Minimal uninstall
    }
}
