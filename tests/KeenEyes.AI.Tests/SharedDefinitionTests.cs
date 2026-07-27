using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.BehaviorTree.Composites;
using KeenEyes.AI.BehaviorTree.Decorators;
using KeenEyes.AI.BehaviorTree.Leaves;
using KeenEyes.AI.Tests.BehaviorTree;
using KeenEyes.AI.Utility;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests;

/// <summary>
/// Regression tests for #1281: behavior definitions (trees, actions, utility sets) are
/// shared between entities, so all execution state must be per-entity. Each test runs
/// one shared definition against two blackboards and asserts the runs do not bleed
/// into each other.
/// </summary>
public class SharedDefinitionTests
{
    #region Composite cursor isolation

    [Fact]
    public void Sequence_SharedBetweenTwoEntities_AdvancesIndependently()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var first = new TestBTNode(BTNodeState.Success);
        var second = new TestBTNode(BTNodeState.Running);
        var sequence = new Sequence
        {
            Name = "Shared",
            Children = [first, second]
        };

        // Entity A advances past the first child and parks on the running second child.
        sequence.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Running);
        first.ExecuteCount.ShouldBe(1);

        // Entity B starts its own run: it must execute the first child from the top,
        // not resume from entity A's cursor.
        sequence.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Running);
        first.ExecuteCount.ShouldBe(2);
    }

    [Fact]
    public void Selector_SharedBetweenTwoEntities_KeepsSeparateCursors()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var failing = new TestBTNode(BTNodeState.Failure);
        var running = new TestBTNode(BTNodeState.Running);
        var selector = new Selector
        {
            Name = "Shared",
            Children = [failing, running]
        };

        selector.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Running);
        failing.ExecuteCount.ShouldBe(1);

        // B must retry the failing child itself rather than resuming at A's cursor.
        selector.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Running);
        failing.ExecuteCount.ShouldBe(2);
    }

    #endregion

    #region Leaf and decorator state isolation

    [Fact]
    public void WaitNode_SharedBetweenTwoEntities_TracksElapsedPerEntity()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        blackboardA.Set(BBKeys.DeltaTime, 0.6f);
        blackboardB.Set(BBKeys.DeltaTime, 0.6f);
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var wait = new WaitNode { Duration = 1f };

        wait.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Running);

        // With shared elapsed state, B's first tick would already exceed the duration.
        wait.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Running);

        // A's second tick completes A; B is still waiting.
        wait.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Success);
        wait.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Cooldown_SharedBetweenTwoEntities_CoolsDownPerEntity()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        blackboardA.Set(BBKeys.Time, 0f);
        blackboardB.Set(BBKeys.Time, 1f);
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var cooldown = new Cooldown
        {
            Duration = 10f,
            Child = new TestBTNode(BTNodeState.Success)
        };

        // A succeeds and starts A's cooldown.
        cooldown.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Success);

        // A is now on cooldown, but B never executed and must not be.
        cooldown.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Failure);
        cooldown.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Repeater_SharedBetweenTwoEntities_CountsPerEntity()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var repeater = new Repeater
        {
            Count = 2,
            Child = new TestBTNode(BTNodeState.Success)
        };

        // A completes one of two iterations.
        repeater.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Running);

        // B starts from zero: its first tick is also iteration one, not A's second.
        repeater.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Running);

        // Each entity independently completes on its own second tick.
        repeater.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Success);
        repeater.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Reset_ForOneEntity_LeavesOtherEntityProgressIntact()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var first = new TestBTNode(BTNodeState.Success);
        var second = new TestBTNode(BTNodeState.Running);
        var tree = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "Shared",
            Root = new Sequence { Children = [first, second] }
        };

        // Both entities park on the running second child.
        tree.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Running);
        tree.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Running);
        first.ExecuteCount.ShouldBe(2);

        // Resetting A must not rewind B's cursor.
        tree.Reset(blackboardA);
        tree.Execute(entityB, blackboardB, world).ShouldBe(BTNodeState.Running);
        first.ExecuteCount.ShouldBe(2); // B resumed at the second child

        tree.Execute(entityA, blackboardA, world).ShouldBe(BTNodeState.Running);
        first.ExecuteCount.ShouldBe(3); // A restarted from the first child
    }

    #endregion

    #region Per-entity last state

    [Fact]
    public void GetLastState_SharedNode_ReportsPerEntityState()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var node = new StateChangingBTNode([BTNodeState.Failure, BTNodeState.Success]);

        node.Execute(entityA, blackboardA, world);
        node.Execute(entityB, blackboardB, world);

        node.GetLastState(blackboardA).ShouldBe(BTNodeState.Failure);
        node.GetLastState(blackboardB).ShouldBe(BTNodeState.Success);
    }

    #endregion

    #region Utility scoring isolation

    [Fact]
    public void UtilityAction_SharedBetweenTwoEntities_KeepsPerEntityLastScore()
    {
        using var world = new World();
        var blackboardA = new Blackboard();
        var blackboardB = new Blackboard();
        blackboardA.Set("threat", 0.2f);
        blackboardB.Set("threat", 0.8f);
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Flee",
            Weight = 1f,
            Considerations =
            [
                new Consideration
                {
                    Name = "Threat",
                    Input = new BlackboardInput { Key = "threat" },
                    Curve = new LinearCurve()
                }
            ]
        };

        var scoreA = action.CalculateScore(entityA, blackboardA, world);
        var scoreB = action.CalculateScore(entityB, blackboardB, world);

        scoreA.ShouldNotBe(scoreB);

        // Scoring B must not overwrite A's recorded score.
        action.GetLastScore(blackboardA).ShouldBe(scoreA);
        action.GetLastScore(blackboardB).ShouldBe(scoreB);
    }

    #endregion

    #region End-to-end through the system

    [Fact]
    public void BehaviorTreeSystem_TwoEntitiesOneDefinition_ProgressIndependently()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var definition = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "SharedWait",
            Root = new WaitNode { Duration = 1f }
        };

        var entityA = world.Spawn()
            .With(BehaviorTreeComponent.Create(definition))
            .Build();

        // A accumulates 0.6s of wait time.
        world.Update(0.6f);

        var entityB = world.Spawn()
            .With(BehaviorTreeComponent.Create(definition))
            .Build();

        // Both tick 0.6s: A crosses 1.0s and completes; B has only 0.6s accumulated.
        world.Update(0.6f);

        world.Get<BehaviorTreeComponent>(entityA).LastResult.ShouldBe(BTNodeState.Success);
        world.Get<BehaviorTreeComponent>(entityB).LastResult.ShouldBe(BTNodeState.Running);
    }

    #endregion
}
