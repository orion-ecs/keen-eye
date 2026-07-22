using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// World-level integration tests for crowd steering: entities with
/// <see cref="CrowdAgent"/> are simulated by the crowd when the provider
/// supports it, plain agents keep waypoint steering, and worlds with a
/// non-crowd provider degrade gracefully.
/// </summary>
public class CrowdSteeringSystemTests
{
    private const float SlabSize = 40f;
    private const float TimeStep = 0.05f;

    #region Helpers

    private static DotRecastProvider CreateSlabProvider()
    {
        var builder = TestHelper.CreateTestBuilder();
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var mesh = builder.Build(vertices, indices);
        return new DotRecastProvider(mesh, TestHelper.CreateTestConfig());
    }

    private static World CreateNavigationWorld(INavigationProvider provider)
    {
        var world = new World();
        world.InstallPlugin(new NavigationPlugin(new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = provider
        }));
        return world;
    }

    private static Entity SpawnAgent(World world, Vector3 position, bool crowd)
    {
        var agent = NavMeshAgent.Create();
        agent.StoppingDistance = 0.5f;

        var spawn = world.Spawn()
            .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
            .With(agent);

        if (crowd)
        {
            var crowdAgent = CrowdAgent.Create();
            crowdAgent.Radius = 0.6f;
            spawn = spawn.With(crowdAgent);
        }

        return spawn.Build();
    }

    #endregion

    #region Crowd Steering Tests

    [Fact]
    public void Update_CrowdAgent_ReachesDestinationViaCrowdSimulation()
    {
        using var provider = CreateSlabProvider();
        using var world = CreateNavigationWorld(provider);

        var entity = SpawnAgent(world, new Vector3(10f, 0f, 10f), crowd: true);
        var destination = new Vector3(30f, 0f, 30f);
        world.GetExtension<NavigationContext>().SetDestination(entity, destination);

        for (int i = 0; i < 800; i++)
        {
            world.Update(TimeStep);
            if (world.Get<NavMeshAgent>(entity).IsStopped)
            {
                break;
            }
        }

        // The crowd simulation registered and moved the entity
        provider.CrowdAgentCount.ShouldBe(1);

        ref readonly var transform = ref world.Get<Transform3D>(entity);
        Vector3.Distance(transform.Position, destination).ShouldBeLessThanOrEqualTo(0.75f);

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        agent.IsStopped.ShouldBeTrue();
        agent.DesiredVelocity.Length().ShouldBe(0f, 1e-3f);
    }

    [Fact]
    public void Update_TwoOpposingCrowdAgents_DoNotInterpenetrate()
    {
        using var provider = CreateSlabProvider();
        using var world = CreateNavigationWorld(provider);

        var left = SpawnAgent(world, new Vector3(10f, 0f, 20f), crowd: true);
        var right = SpawnAgent(world, new Vector3(30f, 0f, 20.05f), crowd: true);

        var navigation = world.GetExtension<NavigationContext>();
        navigation.SetDestination(left, new Vector3(30f, 0f, 20f));
        navigation.SetDestination(right, new Vector3(10f, 0f, 20f));

        float minSeparation = float.MaxValue;
        for (int i = 0; i < 400; i++)
        {
            world.Update(TimeStep);

            var leftPos = world.Get<Transform3D>(left).Position;
            var rightPos = world.Get<Transform3D>(right).Position;
            minSeparation = MathF.Min(minSeparation, Vector3.Distance(leftPos, rightPos));
        }

        minSeparation.ShouldBeGreaterThanOrEqualTo((0.6f + 0.6f) * 0.8f);
    }

    [Fact]
    public void Update_CrowdAgentComponentRemoved_UnregistersFromCrowd()
    {
        using var provider = CreateSlabProvider();
        using var world = CreateNavigationWorld(provider);

        var entity = SpawnAgent(world, new Vector3(10f, 0f, 10f), crowd: true);
        world.GetExtension<NavigationContext>().SetDestination(entity, new Vector3(30f, 0f, 30f));

        world.Update(TimeStep);
        provider.CrowdAgentCount.ShouldBe(1);

        world.Remove<CrowdAgent>(entity);

        provider.CrowdAgentCount.ShouldBe(0);
    }

    [Fact]
    public void Update_CrowdAgentEntityDespawned_UnregistersFromCrowd()
    {
        using var provider = CreateSlabProvider();
        using var world = CreateNavigationWorld(provider);

        var entity = SpawnAgent(world, new Vector3(10f, 0f, 10f), crowd: true);
        world.GetExtension<NavigationContext>().SetDestination(entity, new Vector3(30f, 0f, 30f));

        world.Update(TimeStep);
        provider.CrowdAgentCount.ShouldBe(1);

        world.Despawn(entity);

        provider.CrowdAgentCount.ShouldBe(0);
    }

    #endregion

    #region Non-Crowd Steering Tests

    [Fact]
    public void Update_PlainAgentWithCrowdCapableProvider_UsesWaypointSteering()
    {
        using var provider = CreateSlabProvider();
        using var world = CreateNavigationWorld(provider);

        var entity = SpawnAgent(world, new Vector3(10f, 0f, 10f), crowd: false);
        var destination = new Vector3(30f, 0f, 30f);
        world.GetExtension<NavigationContext>().SetDestination(entity, destination);

        float startDistance = Vector3.Distance(new Vector3(10f, 0f, 10f), destination);
        for (int i = 0; i < 400; i++)
        {
            world.Update(TimeStep);
        }

        // The plain agent never touched the crowd simulation
        provider.CrowdAgentCount.ShouldBe(0);

        // But it still moved toward its destination via waypoint steering
        ref readonly var transform = ref world.Get<Transform3D>(entity);
        Vector3.Distance(transform.Position, destination).ShouldBeLessThan(startDistance * 0.5f);
    }

    [Fact]
    public void Update_ProviderWithoutCrowdSupport_CrowdAgentFallsBackToPlainSteering()
    {
        using var provider = new StubPathProvider();
        using var world = CreateNavigationWorld(provider);

        var entity = SpawnAgent(world, new Vector3(0f, 0f, 0f), crowd: true);
        var destination = new Vector3(10f, 0f, 10f);
        world.GetExtension<NavigationContext>().SetDestination(entity, destination);

        float startDistance = Vector3.Distance(Vector3.Zero, destination);

        // Must not throw even though the provider has no crowd concept
        for (int i = 0; i < 200; i++)
        {
            world.Update(TimeStep);
        }

        // The agent moved toward its destination via plain waypoint steering
        ref readonly var transform = ref world.Get<Transform3D>(entity);
        Vector3.Distance(transform.Position, destination).ShouldBeLessThan(startDistance * 0.5f);
    }

    #endregion

    #region Stub Provider

    /// <summary>
    /// Minimal navigation provider without crowd support that returns direct
    /// straight-line paths.
    /// </summary>
    private sealed class StubPathProvider : INavigationProvider
    {
        public NavigationStrategy Strategy => NavigationStrategy.Custom;

        public bool IsReady => true;

        public INavigationMesh? ActiveMesh => null;

        public NavPath FindPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
            => new([new NavPoint(start), new NavPoint(end)], true, Vector3.Distance(start, end));

        public IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
            => new StubPathRequest(start, end, agent);

        public void CancelAllRequests()
        {
        }

        public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition)
        {
            hitPosition = end;
            return false;
        }

        public bool Raycast(Vector3 start, Vector3 end, NavAreaMask areaMask, out Vector3 hitPosition, out NavAreaType hitAreaType)
        {
            hitPosition = end;
            hitAreaType = NavAreaType.Walkable;
            return false;
        }

        public NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f) => new NavPoint(position);

        public bool IsNavigable(Vector3 position, AgentSettings agent) => true;

        public Vector3? ProjectToNavMesh(Vector3 position, float maxDistance = 5f) => position;

        public float GetAreaCost(NavAreaType areaType) => 1f;

        public void SetAreaCost(NavAreaType areaType, float cost)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Path request that completes immediately with a straight-line path.
    /// </summary>
    private sealed class StubPathRequest(Vector3 start, Vector3 end, AgentSettings agent) : IPathRequest
    {
        public int Id => 1;

        public PathRequestStatus Status { get; private set; } = PathRequestStatus.Completed;

        public Vector3 Start => start;

        public Vector3 End => end;

        public AgentSettings Agent => agent;

        public NavPath Result { get; } = new([new NavPoint(start), new NavPoint(end)], true, Vector3.Distance(start, end));

        public void Cancel() => Status = PathRequestStatus.Cancelled;

        public bool Wait(TimeSpan timeout) => true;

        public Task<NavPath> AsTask() => Task.FromResult(Result);

        public void Dispose()
        {
        }
    }

    #endregion
}
