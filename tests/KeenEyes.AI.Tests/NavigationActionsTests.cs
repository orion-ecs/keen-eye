using System.Numerics;
using KeenEyes.AI.Actions;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.AI.Tests;

/// <summary>
/// Tests for navigation AI actions.
/// </summary>
public class NavigationActionsTests : IDisposable
{
    private readonly World world;
    private readonly Blackboard blackboard;

    public NavigationActionsTests()
    {
        world = new World();

        // Install grid navigation provider
        var gridConfig = GridConfig.WithSize(100, 100, 1f);
        world.InstallPlugin(new GridNavigationPlugin(gridConfig));

        // Install navigation plugin
        world.InstallPlugin(new NavigationPlugin());

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

    #region MoveToAction Tests

    [Fact]
    public void MoveToAction_WithValidDestination_ReturnsRunning()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var action = new MoveToAction
        {
            Destination = new Vector3(10, 0, 10)
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void MoveToAction_WithoutNavMeshAgent_ReturnsFailure()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        var action = new MoveToAction
        {
            Destination = new Vector3(10, 0, 10)
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void MoveToAction_AtDestination_ReturnsSuccess()
    {
        var destination = new Vector3(1, 0, 1);
        var entity = CreateNavigatingEntity(destination);

        var action = new MoveToAction
        {
            Destination = destination,
            ArrivalTolerance = 1.0f
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void MoveToAction_UseBlackboardDestination_ReadsFromBlackboard()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var destination = new Vector3(10, 0, 10);
        blackboard.Set(BBKeys.Destination, destination);

        var action = new MoveToAction
        {
            UseBlackboardDestination = true
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void MoveToAction_Reset_ClearsState()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var action = new MoveToAction
        {
            Destination = new Vector3(10, 0, 10)
        };

        // Execute once to set state
        action.Execute(entity, blackboard, world);

        // Reset
        action.Reset();

        // Execute again - should request new path
        var result = action.Execute(entity, blackboard, world);
        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void MoveToAction_OnInterrupted_StopsAgent()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var action = new MoveToAction
        {
            Destination = new Vector3(10, 0, 10)
        };

        // Execute to start navigation
        action.Execute(entity, blackboard, world);

        // Interrupt
        action.OnInterrupted(entity, blackboard, world);

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        agent.IsStopped.ShouldBeTrue();
    }

    #endregion

    #region ChaseAction Tests

    [Fact]
    public void ChaseAction_WithValidTarget_ReturnsRunning()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 10), Quaternion.Identity, Vector3.One))
            .Build();

        blackboard.Set(BBKeys.Target, target);

        var action = new ChaseAction();

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void ChaseAction_TargetDead_ReturnsFailure()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 10), Quaternion.Identity, Vector3.One))
            .Build();

        blackboard.Set(BBKeys.Target, target);
        world.Despawn(target);

        var action = new ChaseAction();

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void ChaseAction_NoTarget_ReturnsFailure()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        var action = new ChaseAction();

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void ChaseAction_WithinCatchDistance_ReturnsSuccess()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(1, 0, 0), Quaternion.Identity, Vector3.One))
            .Build();

        blackboard.Set(BBKeys.Target, target);

        var action = new ChaseAction
        {
            CatchDistance = 2.0f
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void ChaseAction_UpdatesBlackboardWithTargetPosition()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var targetPos = new Vector3(10, 0, 10);
        var target = world.Spawn()
            .With(new Transform3D(targetPos, Quaternion.Identity, Vector3.One))
            .Build();

        blackboard.Set(BBKeys.Target, target);

        var action = new ChaseAction();
        action.Execute(entity, blackboard, world);

        blackboard.Get<Vector3>(BBKeys.TargetPosition).ShouldBe(targetPos);
        blackboard.Get<Vector3>(BBKeys.TargetLastSeen).ShouldBe(targetPos);
    }

    [Fact]
    public void ChaseAction_DirectTarget_UsesProvidedEntity()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 10), Quaternion.Identity, Vector3.One))
            .Build();

        var action = new ChaseAction
        {
            Target = target
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    #endregion

    #region PatrolAction Tests

    [Fact]
    public void PatrolAction_WithWaypoints_ReturnsRunning()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        var action = new PatrolAction
        {
            Waypoints =
            [
                new Vector3(10, 0, 0),
                new Vector3(10, 0, 10),
                new Vector3(0, 0, 10)
            ],
            Loop = true
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void PatrolAction_NoWaypoints_ReturnsFailure()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        var action = new PatrolAction();

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void PatrolAction_UsesBlackboardWaypoints()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var waypoints = new[]
        {
            new Vector3(10, 0, 0),
            new Vector3(10, 0, 10)
        };
        blackboard.Set(BBKeys.PatrolWaypoints, waypoints);

        var action = new PatrolAction();

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void PatrolAction_SetsDestinationInBlackboard()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var waypoints = new[]
        {
            new Vector3(10, 0, 0),
            new Vector3(10, 0, 10)
        };

        var action = new PatrolAction
        {
            Waypoints = waypoints
        };

        action.Execute(entity, blackboard, world);

        blackboard.Get<Vector3>(BBKeys.Destination).ShouldBe(waypoints[0]);
    }

    [Fact]
    public void PatrolAction_TracksPatrolIndex()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        var action = new PatrolAction
        {
            Waypoints =
            [
                new Vector3(10, 0, 0),
                new Vector3(10, 0, 10)
            ]
        };

        action.Execute(entity, blackboard, world);

        blackboard.Get(BBKeys.PatrolIndex, -1).ShouldBe(0);
    }

    #endregion

    #region FleeAction Tests

    [Fact]
    public void FleeAction_WithThreatEntity_ReturnsRunning()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var threat = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .Build();

        blackboard.Set(BBKeys.ThreatSource, threat);

        var action = new FleeAction
        {
            MinFleeDistance = 10.0f,
            SampleRadius = 15.0f
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void FleeAction_WithThreatPosition_ReturnsRunning()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        blackboard.Set(BBKeys.ThreatPosition, new Vector3(5, 0, 0));

        var action = new FleeAction
        {
            MinFleeDistance = 10.0f,
            SampleRadius = 15.0f
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void FleeAction_NoThreat_ReturnsFailure()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);

        var action = new FleeAction();

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void FleeAction_AlreadySafe_ReturnsSuccess()
    {
        // Position entity far from threat
        var entity = CreateNavigatingEntity(new Vector3(50, 0, 50));
        blackboard.Set(BBKeys.ThreatPosition, Vector3.Zero);

        // Stop the agent so it considers itself safe
        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.IsStopped = true;

        var action = new FleeAction
        {
            MinFleeDistance = 10.0f
        };

        var result = action.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void FleeAction_SetsThreatPositionInBlackboard()
    {
        var entity = CreateNavigatingEntity(Vector3.Zero);
        var threatPos = new Vector3(5, 0, 0);
        var threat = world.Spawn()
            .With(new Transform3D(threatPos, Quaternion.Identity, Vector3.One))
            .Build();

        blackboard.Set(BBKeys.ThreatSource, threat);

        var action = new FleeAction
        {
            MinFleeDistance = 10.0f
        };

        action.Execute(entity, blackboard, world);

        blackboard.Get<Vector3>(BBKeys.ThreatPosition).ShouldBe(threatPos);
    }

    #endregion
}
