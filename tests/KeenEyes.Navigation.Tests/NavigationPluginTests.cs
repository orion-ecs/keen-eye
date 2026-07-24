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

    [Fact]
    public void Uninstall_WithCustomProvider_DoesNotDisposeCallerOwnedProvider()
    {
        // Regression test for #1171: a caller-supplied custom provider is owned by the
        // caller. Uninstalling the plugin must remove the registration WITHOUT disposing
        // the provider, which the caller still holds and may reuse.
        world = new World();

        var inner = new GridNavigationProvider(GridConfig.WithSize(100, 100, 1f));
        var customProvider = new DisposalTrackingNavigationProvider(inner);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };
        world.InstallPlugin(new NavigationPlugin(config));

        world.UninstallPlugin<NavigationPlugin>();

        // Caller-owned: never disposed by the plugin/world.
        customProvider.DisposeCount.ShouldBe(0);
        // The registration this plugin added is removed.
        world.TryGetExtension<INavigationProvider>(out _).ShouldBeFalse();

        // The caller can still dispose it exactly once, itself.
        customProvider.Dispose();
        customProvider.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public void Uninstall_WithSharedProviderPlugin_LeavesProviderOwnedByOtherPluginRegistered()
    {
        // Regression test for #1171: when GridNavigationPlugin owns the provider and is
        // still installed, uninstalling the consumer NavigationPlugin must not remove or
        // dispose the shared provider.
        world = new World();

        world.InstallPlugin(new GridNavigationPlugin(GridConfig.WithSize(100, 100, 1f)));
        world.InstallPlugin(new NavigationPlugin());

        var provider = world.GetExtension<INavigationProvider>();

        world.UninstallPlugin<NavigationPlugin>();

        // The provider belongs to GridNavigationPlugin (still installed): its registration
        // must survive and remain the same instance.
        world.TryGetExtension<INavigationProvider>(out var stillRegistered).ShouldBeTrue();
        stillRegistered.ShouldBeSameAs(provider);
    }

    #endregion

    #region Component Registration Tests

    [Fact]
    public void Install_RegistersNavMeshAgentComponent()
    {
        using var testWorld = new World();

        // Use custom provider to avoid dependency on GridNavigationPlugin
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasComponentRegistered<NavMeshAgent>().ShouldBeTrue();
    }

    [Fact]
    public void Install_RegistersNavMeshObstacleComponent()
    {
        using var testWorld = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasComponentRegistered<NavMeshObstacle>().ShouldBeTrue();
    }

    #endregion

    #region System Registration Tests

    [Fact]
    public void Install_RegistersPathRequestSystem()
    {
        using var testWorld = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.PathRequestSystem>().ShouldBeTrue();
        context.WasSystemRegisteredAtPhase<Systems.PathRequestSystem>(SystemPhase.Update).ShouldBeTrue();
    }

    [Fact]
    public void Install_WithSteeringEnabled_RegistersNavMeshAgentSystem()
    {
        using var testWorld = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            AgentSteeringEnabled = true
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.NavMeshAgentSystem>().ShouldBeTrue();
    }

    [Fact]
    public void Install_WithSteeringDisabled_DoesNotRegisterNavMeshAgentSystem()
    {
        using var testWorld = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            AgentSteeringEnabled = false
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.NavMeshAgentSystem>().ShouldBeFalse();
    }

    [Fact]
    public void Install_WithDynamicObstaclesEnabled_RegistersObstacleUpdateSystem()
    {
        using var testWorld = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            DynamicObstaclesEnabled = true
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.ObstacleUpdateSystem>().ShouldBeTrue();
    }

    [Fact]
    public void Install_WithDynamicObstaclesDisabled_DoesNotRegisterObstacleUpdateSystem()
    {
        using var testWorld = new World();

        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        var customProvider = new GridNavigationProvider(gridConfig);

        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = customProvider,
            DynamicObstaclesEnabled = false
        };

        var plugin = new NavigationPlugin(config);
        var context = new MockPluginContext(plugin, testWorld);

        plugin.Install(context);

        context.WasSystemRegistered<Systems.ObstacleUpdateSystem>().ShouldBeFalse();
    }

    #endregion
}

/// <summary>
/// A caller-owned <see cref="INavigationProvider"/> that delegates to an inner provider
/// and records how many times it is disposed. Used to verify that plugin uninstall does
/// not dispose providers the caller owns (issue #1171).
/// </summary>
internal sealed class DisposalTrackingNavigationProvider(INavigationProvider inner) : INavigationProvider
{
    public int DisposeCount { get; private set; }

    public NavigationStrategy Strategy => inner.Strategy;

    public bool IsReady => inner.IsReady;

    public INavigationMesh? ActiveMesh => inner.ActiveMesh;

    public NavPath FindPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
        => inner.FindPath(start, end, agent, areaMask);

    public IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
        => inner.RequestPath(start, end, agent, areaMask);

    public void CancelAllRequests() => inner.CancelAllRequests();

    public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition)
        => inner.Raycast(start, end, out hitPosition);

    public bool Raycast(Vector3 start, Vector3 end, NavAreaMask areaMask, out Vector3 hitPosition, out NavAreaType hitAreaType)
        => inner.Raycast(start, end, areaMask, out hitPosition, out hitAreaType);

    public NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f)
        => inner.FindNearestPoint(position, searchRadius);

    public bool IsNavigable(Vector3 position, AgentSettings agent) => inner.IsNavigable(position, agent);

    public Vector3? ProjectToNavMesh(Vector3 position, float maxDistance = 5f)
        => inner.ProjectToNavMesh(position, maxDistance);

    public float GetAreaCost(NavAreaType areaType) => inner.GetAreaCost(areaType);

    public void SetAreaCost(NavAreaType areaType, float cost) => inner.SetAreaCost(areaType, cost);

    public void Update(float deltaTime) => inner.Update(deltaTime);

    public void Dispose()
    {
        DisposeCount++;
        inner.Dispose();
    }
}
