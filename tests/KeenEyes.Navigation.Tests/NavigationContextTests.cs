using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.Navigation.Tests;

/// <summary>
/// Tests for NavigationContext API.
/// </summary>
public class NavigationContextTests : IDisposable
{
    private readonly World world;
    private readonly NavigationContext context;

    public NavigationContextTests()
    {
        world = new World();

        // Install grid navigation provider
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        world.InstallPlugin(new GridNavigationPlugin(gridConfig));

        // Install navigation plugin
        world.InstallPlugin(new NavigationPlugin());

        context = world.GetExtension<NavigationContext>();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Properties

    [Fact]
    public void IsReady_WhenProviderReady_ReturnsTrue()
    {
        context.IsReady.ShouldBeTrue();
    }

    [Fact]
    public void Strategy_ReturnsProviderStrategy()
    {
        context.Strategy.ShouldBe(NavigationStrategy.Grid);
    }

    [Fact]
    public void Config_ReturnsPluginConfig()
    {
        context.Config.ShouldNotBeNull();
    }

    #endregion

    #region SetDestination Tests

    [Fact]
    public void SetDestination_WithValidEntity_RequestsPath()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        context.SetDestination(entity, new Vector3(10, 0, 10));

        context.PendingRequestCount.ShouldBe(1);
    }

    [Fact]
    public void SetDestination_WithoutNavMeshAgent_ThrowsInvalidOperationException()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        Should.Throw<InvalidOperationException>(() =>
            context.SetDestination(entity, new Vector3(10, 0, 10)));
    }

    [Fact]
    public void SetDestination_UpdatesAgentComponent()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        var destination = new Vector3(10, 0, 10);
        context.SetDestination(entity, destination);

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Destination.ShouldBe(destination);
        agent.PathPending.ShouldBeTrue();
    }

    [Fact]
    public void SetDestination_CalledTwice_CancelsPreviousRequest()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        context.SetDestination(entity, new Vector3(10, 0, 10));
        context.SetDestination(entity, new Vector3(20, 0, 20));

        // Should only have one pending request
        context.PendingRequestCount.ShouldBe(1);
    }

    #endregion

    #region Stop Tests

    [Fact]
    public void Stop_WithValidEntity_StopsAgent()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        context.SetDestination(entity, new Vector3(10, 0, 10));
        context.Stop(entity);

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        agent.IsStopped.ShouldBeTrue();
    }

    [Fact]
    public void Stop_WithoutNavMeshAgent_ThrowsInvalidOperationException()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        Should.Throw<InvalidOperationException>(() => context.Stop(entity));
    }

    [Fact]
    public void Stop_CancelsPendingPathRequest()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        context.SetDestination(entity, new Vector3(10, 0, 10));
        context.PendingRequestCount.ShouldBe(1);

        context.Stop(entity);
        context.PendingRequestCount.ShouldBe(0);
    }

    #endregion

    #region Resume Tests

    [Fact]
    public void Resume_WithStoppedAgentWithPath_ResumesMovement()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        // Request a path and process it
        context.SetDestination(entity, new Vector3(10, 0, 10));
        world.Update(0.016f); // Process path request

        // Now stop and resume
        context.Stop(entity);

        // Give the agent a path again for resume to work
        context.SetDestination(entity, new Vector3(10, 0, 10));
        world.Update(0.016f); // Process path request

        context.Stop(entity);
        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.IsStopped.ShouldBeTrue();

        // Resume only works if agent has a path
        // First we need to ensure agent has a path set
        agent.HasPath = true;
        context.Resume(entity);

        ref readonly var agentAfter = ref world.Get<NavMeshAgent>(entity);
        agentAfter.IsStopped.ShouldBeFalse();
    }

    [Fact]
    public void Resume_WithoutPath_DoesNotResume()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        // Stop without setting a destination (no path)
        context.Stop(entity);
        context.Resume(entity);

        // Agent should still be stopped because Resume only resumes if HasPath is true
        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        agent.IsStopped.ShouldBeTrue();
    }

    [Fact]
    public void Resume_WithoutNavMeshAgent_DoesNotThrow()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        // Should not throw - just returns early
        context.Resume(entity);
    }

    #endregion

    #region Query Tests

    [Fact]
    public void FindPath_ReturnsPath()
    {
        var path = context.FindPath(
            Vector3.Zero,
            new Vector3(10, 0, 10),
            AgentSettings.Default);

        path.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RequestPath_ReturnsPathRequest()
    {
        var request = context.RequestPath(
            Vector3.Zero,
            new Vector3(10, 0, 10),
            AgentSettings.Default);

        request.ShouldNotBeNull();
        request.Status.ShouldBe(PathRequestStatus.Pending);
    }

    [Fact]
    public void IsNavigable_WithValidPosition_ReturnsTrue()
    {
        var isNavigable = context.IsNavigable(Vector3.Zero, AgentSettings.Default);

        isNavigable.ShouldBeTrue();
    }

    [Fact]
    public void Raycast_WithClearPath_ReturnsFalse()
    {
        var hit = context.Raycast(Vector3.Zero, new Vector3(10, 0, 10), out var hitPosition);

        hit.ShouldBeFalse();
        hitPosition.ShouldBe(new Vector3(10, 0, 10));
    }

    #endregion

    #region Agent State Tests

    [Fact]
    public void ActiveAgentCount_ReturnsCorrectCount()
    {
        context.ActiveAgentCount.ShouldBe(0);

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        context.SetDestination(entity, new Vector3(10, 0, 10));

        // Process the request
        world.Update(0.016f);

        // Agent should have state now (path completed)
        context.ActiveAgentCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryGetAgentState_WithNoPath_ReturnsFalse()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();

        var result = context.TryGetAgentState(entity, out _);

        result.ShouldBeFalse();
    }

    #endregion
}
