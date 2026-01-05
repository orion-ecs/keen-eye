using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Grid;
using KeenEyes.Testing.Plugins;

namespace KeenEyes.Navigation.Tests;

/// <summary>
/// Tests for NavigationPlugin integration with the World.
/// </summary>
public class NavigationPluginTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Installation Tests

    [Fact]
    public void Install_WithGridProvider_Succeeds()
    {
        world = new World();

        // Install grid navigation provider first
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        world.InstallPlugin(new GridNavigationPlugin(gridConfig));

        // Then install the navigation plugin
        world.InstallPlugin(new NavigationPlugin());

        // Should have both extensions
        world.TryGetExtension<NavigationContext>(out _).ShouldBeTrue();
        world.TryGetExtension<INavigationProvider>(out _).ShouldBeTrue();
    }

    [Fact]
    public void Install_WithCustomProvider_Succeeds()
    {
        world = new World();

        // Create a custom provider
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        // Install with custom provider - should not throw
        world.InstallPlugin(new NavigationPlugin(config));

        world.TryGetExtension<NavigationContext>(out _).ShouldBeTrue();
    }

    [Fact]
    public void Install_WithInvalidConfig_ThrowsArgumentException()
    {
        var config = new NavigationConfig
        {
            MaxPathRequestsPerFrame = 0 // Invalid
        };

        var ex = Should.Throw<ArgumentException>(() => new NavigationPlugin(config));
        ex.Message.ShouldContain("Invalid NavigationConfig");
    }

    [Fact]
    public void Install_WithoutProvider_ThrowsInvalidOperationException()
    {
        world = new World();

        // No provider installed - should throw
        Should.Throw<InvalidOperationException>(() =>
            world.InstallPlugin(new NavigationPlugin()));
    }

    #endregion

    #region Uninstallation Tests

    [Fact]
    public void Uninstall_RemovesExtensions()
    {
        world = new World();

        // Setup
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        world.InstallPlugin(new GridNavigationPlugin(gridConfig));
        world.InstallPlugin(new NavigationPlugin());

        world.TryGetExtension<NavigationContext>(out _).ShouldBeTrue();

        // Uninstall
        world.UninstallPlugin<NavigationPlugin>();

        world.TryGetExtension<NavigationContext>(out _).ShouldBeFalse();
    }

    [Fact]
    public void Uninstall_DisposesContext()
    {
        world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        world.InstallPlugin(new GridNavigationPlugin(gridConfig));
        world.InstallPlugin(new NavigationPlugin());

        var context = world.GetExtension<NavigationContext>();

        // Add an agent to trigger state creation
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        context.SetDestination(entity, new Vector3(10, 0, 10));
        context.PendingRequestCount.ShouldBeGreaterThan(0);

        // Uninstall should dispose context and cancel pending requests
        world.UninstallPlugin<NavigationPlugin>();

        // Extension should be gone
        world.TryGetExtension<NavigationContext>(out _).ShouldBeFalse();
    }

    #endregion

    #region Component Registration Tests

    [Fact]
    public void Install_RegistersNavMeshAgentComponent()
    {
        using var world = new World();

        // Use custom provider to avoid dependency on GridNavigationPlugin
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasComponentRegistered<NavMeshAgent>().ShouldBeTrue();
    }

    [Fact]
    public void Install_RegistersNavMeshObstacleComponent()
    {
        using var world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasComponentRegistered<NavMeshObstacle>().ShouldBeTrue();
    }

    #endregion

    #region System Registration Tests

    [Fact]
    public void Install_RegistersPathRequestSystem()
    {
        using var world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.PathRequestSystem>().ShouldBeTrue();
        context.WasSystemRegisteredAtPhase<Systems.PathRequestSystem>(SystemPhase.Update).ShouldBeTrue();
    }

    [Fact]
    public void Install_WithSteeringEnabled_RegistersNavMeshAgentSystem()
    {
        using var world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            AgentSteeringEnabled = true
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.NavMeshAgentSystem>().ShouldBeTrue();
    }

    [Fact]
    public void Install_WithSteeringDisabled_DoesNotRegisterNavMeshAgentSystem()
    {
        using var world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            AgentSteeringEnabled = false
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.NavMeshAgentSystem>().ShouldBeFalse();
    }

    [Fact]
    public void Install_WithDynamicObstaclesEnabled_RegistersObstacleUpdateSystem()
    {
        using var world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            DynamicObstaclesEnabled = true
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.ObstacleUpdateSystem>().ShouldBeTrue();
    }

    [Fact]
    public void Install_WithDynamicObstaclesDisabled_DoesNotRegisterObstacleUpdateSystem()
    {
        using var world = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            DynamicObstaclesEnabled = false
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.ObstacleUpdateSystem>().ShouldBeFalse();
    }

    #endregion
}
