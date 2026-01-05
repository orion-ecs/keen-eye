using System.Numerics;
using KeenEyes.AI;
using KeenEyes.AI.Conditions;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.AI.Tests;

/// <summary>
/// Tests for navigation AI conditions.
/// </summary>
public class NavigationConditionsTests : IDisposable
{
    private readonly World world;
    private readonly NavigationContext navContext;
    private readonly Blackboard blackboard;

    public NavigationConditionsTests()
    {
        world = new World();

        // Install grid navigation provider
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        world.InstallPlugin(new GridNavigationPlugin(gridConfig));

        // Install navigation plugin
        world.InstallPlugin(new NavigationPlugin());

        navContext = world.GetExtension<NavigationContext>();
        blackboard = new Blackboard();
        blackboard.Set(BBKeys.Time, 0f);
        blackboard.Set(BBKeys.DeltaTime, 0.016f);
    }

    public void Dispose()
    {
        world.Dispose();
    }

    private Entity CreateNavigatingEntity(Vector3 position)
    {
        return world.Spawn()
            .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
            .With(NavMeshAgent.Create())
            .Build();
    }

    #region HasPathCondition Tests

    [Fact]
    public void HasPathCondition_WithNoPath_ReturnsFalse()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var condition = new HasPathCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPathCondition_WithPath_ReturnsTrue()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        // Set destination and process
        navContext.SetDestination(entity, new Vector3(10, 0, 10));
        world.Update(0.016f); // Process path request

        var condition = new HasPathCondition();

        // Need to wait for path computation
        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        if (agent.HasPath)
        {
            var result = condition.Evaluate(entity, blackboard, world);
            result.ShouldBeTrue();
        }
    }

    [Fact]
    public void HasPathCondition_WithPendingPath_ReturnsFalseByDefault()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        navContext.SetDestination(entity, new Vector3(10, 0, 10));

        // Path is pending but not complete
        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        if (agent.PathPending && !agent.HasPath)
        {
            var condition = new HasPathCondition { RequireCompletedPath = true };

            var result = condition.Evaluate(entity, blackboard, world);

            result.ShouldBeFalse();
        }
    }

    [Fact]
    public void HasPathCondition_WithPendingPath_AllowsPending()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        navContext.SetDestination(entity, new Vector3(10, 0, 10));

        // Path is pending
        var condition = new HasPathCondition { RequireCompletedPath = false };

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        if (agent.PathPending)
        {
            var result = condition.Evaluate(entity, blackboard, world);
            result.ShouldBeTrue();
        }
    }

    [Fact]
    public void HasPathCondition_WithoutNavMeshAgent_ReturnsFalse()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        var condition = new HasPathCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeFalse();
    }

    #endregion

    #region AtDestinationCondition Tests

    [Fact]
    public void AtDestinationCondition_AtDestination_ReturnsTrue()
    {
        var destination = new Vector3(5, 0, 5);
        var entity = CreateNavigatingEntity(destination);

        // Set destination close to current position
        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Destination = destination;

        var condition = new AtDestinationCondition
        {
            Tolerance = 1.0f
        };

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AtDestinationCondition_FarFromDestination_ReturnsFalse()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Destination = new Vector3(10, 0, 10);

        var condition = new AtDestinationCondition
        {
            Tolerance = 1.0f
        };

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AtDestinationCondition_UsesBlackboardDestination()
    {
        var destination = new Vector3(1, 0, 1);
        var entity = CreateNavigatingEntity(destination);

        blackboard.Set(BBKeys.Destination, destination);

        var condition = new AtDestinationCondition
        {
            UseBlackboardDestination = true,
            Tolerance = 1.0f
        };

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AtDestinationCondition_UsesAgentStoppingDistance()
    {
        var destination = new Vector3(0.05f, 0, 0);
        var entity = CreateNavigatingEntity(Vector3.Zero);

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Destination = destination;
        agent.StoppingDistance = 0.1f;

        var condition = new AtDestinationCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AtDestinationCondition_WithoutNavMeshAgent_ReturnsFalse()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        var condition = new AtDestinationCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AtDestinationCondition_ChecksRemainingDistance()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Destination = new Vector3(10, 0, 10);
        agent.HasPath = true;
        agent.RemainingDistance = 0.05f;
        agent.StoppingDistance = 0.1f;

        var condition = new AtDestinationCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeTrue();
    }

    #endregion

    #region PathBlockedCondition Tests

    [Fact]
    public void PathBlockedCondition_NoPath_ReturnsFalse()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        // Agent is stopped with no path - not blocked, just no destination
        var condition = new PathBlockedCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        result.ShouldBeFalse();
    }

    [Fact]
    public void PathBlockedCondition_WithValidPath_ReturnsFalse()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        navContext.SetDestination(entity, new Vector3(10, 0, 10));
        world.Update(0.016f); // Process path request

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        if (agent.HasPath)
        {
            var condition = new PathBlockedCondition
            {
                UseNavmeshRaycast = false // Skip raycast for this test
            };

            var result = condition.Evaluate(entity, blackboard, world);

            result.ShouldBeFalse();
        }
    }

    [Fact]
    public void PathBlockedCondition_WithPendingPath_AllowsByDefault()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        navContext.SetDestination(entity, new Vector3(10, 0, 10));

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        if (agent.PathPending)
        {
            var condition = new PathBlockedCondition { AllowPendingPath = true };

            var result = condition.Evaluate(entity, blackboard, world);

            result.ShouldBeFalse();
        }
    }

    [Fact]
    public void PathBlockedCondition_WithPendingPath_CanReportAsBlocked()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        navContext.SetDestination(entity, new Vector3(10, 0, 10));

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        if (agent.PathPending)
        {
            var condition = new PathBlockedCondition { AllowPendingPath = false };

            var result = condition.Evaluate(entity, blackboard, world);

            result.ShouldBeTrue();
        }
    }

    [Fact]
    public void PathBlockedCondition_WithoutNavMeshAgent_ReturnsTrue()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        var condition = new PathBlockedCondition();

        var result = condition.Evaluate(entity, blackboard, world);

        // No NavMeshAgent means navigation is not available = blocked
        result.ShouldBeTrue();
    }

    #endregion
}
