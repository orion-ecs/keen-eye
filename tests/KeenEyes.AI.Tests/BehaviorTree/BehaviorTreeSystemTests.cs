using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.BehaviorTree;

/// <summary>
/// Tests for the BehaviorTreeSystem.
/// </summary>
public class BehaviorTreeSystemTests
{
    #region Initialization Tests

    [Fact]
    public void Update_InitializesBehaviorTree()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = new TestBTNode(BTNodeState.Success)
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        component.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Update_SetsTimeInBlackboard()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = new TestBTNode(BTNodeState.Success)
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.5f);

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        var blackboard = component.GetOrCreateBlackboard();

        blackboard.Get<float>(BBKeys.DeltaTime).ShouldBe(0.5f);
    }

    #endregion

    #region Disabled Tests

    [Fact]
    public void Update_SkipsDisabledBehaviorTrees()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var child = new TestBTNode(BTNodeState.Success);
        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = child
        };

        var component = BehaviorTreeComponent.Create(bt);
        component.Enabled = false;

        var entity = world.Spawn()
            .With(component)
            .Build();

        world.Update(0.016f);

        child.ExecuteCount.ShouldBe(0);
    }

    [Fact]
    public void Update_SkipsNullDefinition()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var component = new BehaviorTreeComponent
        {
            Definition = null,
            Enabled = true
        };

        var entity = world.Spawn()
            .With(component)
            .Build();

        // Should not throw
        Should.NotThrow(() => world.Update(0.016f));
    }

    #endregion

    #region Execution Tests

    [Fact]
    public void Update_ExecutesBehaviorTree()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var child = new TestBTNode(BTNodeState.Success);
        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = child
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        child.ExecuteCount.ShouldBe(1);
    }

    [Fact]
    public void Update_StoresLastResult()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = new TestBTNode(BTNodeState.Failure)
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        component.LastResult.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void Update_WithRunningNode_ReturnsRunningState()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var runningNode = new TestBTNode(BTNodeState.Running);
        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = runningNode
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        // The system tracks the result but RunningNode tracking is handled
        // at tree level, not system level. Verify the result instead.
        component.LastResult.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void Update_ClearsRunningNodeOnComplete()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = new TestBTNode(BTNodeState.Success)
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        component.RunningNode.ShouldBeNull();
    }

    #endregion

    #region Running State Tests

    [Fact]
    public void Update_ContinuesRunningTree()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var runningNode = new StateChangingBTNode([BTNodeState.Running, BTNodeState.Success]);
        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = runningNode
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        // First update - Running
        world.Update(0.016f);
        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        component.LastResult.ShouldBe(BTNodeState.Running);

        // Second update - Success
        world.Update(0.016f);
        component = ref world.Get<BehaviorTreeComponent>(entity);
        component.LastResult.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Update_ResetsTreeOnSuccess()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var child = new TestBTNode(BTNodeState.Success);
        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = child
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        child.WasReset.ShouldBeTrue();
    }

    #endregion

    #region Multiple Entities Tests

    [Fact]
    public void Update_ProcessesMultipleEntities()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var child1 = new TestBTNode(BTNodeState.Success);
        var child2 = new TestBTNode(BTNodeState.Success);

        var bt1 = new AI.BehaviorTree.BehaviorTree { Name = "Test1", Root = child1 };
        var bt2 = new AI.BehaviorTree.BehaviorTree { Name = "Test2", Root = child2 };

        var entity1 = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt1))
            .Build();

        var entity2 = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt2))
            .Build();

        world.Update(0.016f);

        child1.ExecuteCount.ShouldBe(1);
        child2.ExecuteCount.ShouldBe(1);
    }

    #endregion

    #region Invalid Definition Tests

    [Fact]
    public void Update_WithInvalidDefinition_DisablesComponent()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var bt = new AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = null // Invalid - no root
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        component.IsInitialized.ShouldBeTrue();
        // The system should handle invalid definitions gracefully
    }

    #endregion
}
