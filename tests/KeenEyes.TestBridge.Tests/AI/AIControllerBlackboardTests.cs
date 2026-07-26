using System.Numerics;
using KeenEyes.AI;
using KeenEyes.AI.FSM;

namespace KeenEyes.TestBridge.Tests.AI;

/// <summary>
/// Tests for blackboard inspection through the TestBridge AI controller (issue #1306).
/// Blackboard stores value types inside an internal reusable cell to avoid re-boxing on hot
/// writes; inspection must report the real value and its real type, never the storage wrapper.
/// </summary>
public class AIControllerBlackboardTests
{
    #region Helpers

    private static Entity SpawnAIEntity(World world, Action<Blackboard> populate)
    {
        world.InstallPlugin(new AIPlugin());

        var entity = world.Spawn()
            .With(new StateMachineComponent { Enabled = true })
            .Build();

        ref var fsm = ref world.Get<StateMachineComponent>(entity);
        populate(fsm.GetOrCreateBlackboard());

        return entity;
    }

    #endregion

    #region GetBlackboardAsync value-type reporting

    [Fact]
    public async Task GetBlackboardAsync_WithFloatValue_ReportsRealTypeAndValue()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard => blackboard.Set("Speed", 1.5f));
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        var entry = entries.ShouldHaveSingleItem();
        entry.Key.ShouldBe("Speed");
        entry.ValueType.ShouldBe("Single");
        entry.Value.ShouldNotBeNull();
        entry.Value!.Value.GetSingle().ShouldBe(1.5f);
        entry.ValueString.ShouldBe("1.5");
    }

    [Fact]
    public async Task GetBlackboardAsync_WithVector3Value_ReportsRealType()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard => blackboard.Set("Target", new Vector3(1, 2, 3)));
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        // Vector3 exposes its components as fields, which the default serializer options omit,
        // so only the reported type is meaningful here - it must be the stored type.
        var entry = entries.ShouldHaveSingleItem();
        entry.Key.ShouldBe("Target");
        entry.ValueType.ShouldBe("Vector3");
    }

    [Fact]
    public async Task GetBlackboardAsync_WithValueTypeEntry_DoesNotLeakStorageWrapper()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard => blackboard.Set("Ticks", 7));
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        var entry = entries.ShouldHaveSingleItem();
        entry.ValueType.ShouldNotContain("Cell");
        entry.ValueType.ShouldBe("Int32");
        entry.Value.ShouldNotBeNull();
        entry.Value!.Value.GetInt32().ShouldBe(7);
    }

    [Fact]
    public async Task GetBlackboardAsync_WithReferenceTypeValue_ReportsValueUnchanged()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard => blackboard.Set("Name", "goblin"));
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        var entry = entries.ShouldHaveSingleItem();
        entry.ValueType.ShouldBe("String");
        entry.ValueString.ShouldBe("goblin");
        entry.Value.ShouldNotBeNull();
        entry.Value!.Value.GetString().ShouldBe("goblin");
    }

    [Fact]
    public async Task GetBlackboardAsync_WithMixedEntries_ReportsEveryEntryWithItsRealType()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard =>
        {
            blackboard.Set("Speed", 1.5f);
            blackboard.Set("Target", new Vector3(4, 5, 6));
            blackboard.Set("Name", "goblin");
        });
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        entries.Count.ShouldBe(3);
        entries.Single(e => e.Key == "Speed").ValueType.ShouldBe("Single");
        entries.Single(e => e.Key == "Target").ValueType.ShouldBe("Vector3");
        entries.Single(e => e.Key == "Name").ValueType.ShouldBe("String");
    }

    [Fact]
    public async Task GetBlackboardAsync_AfterRepeatedSetOfSameKey_ReportsLatestValue()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard =>
        {
            blackboard.Set("Speed", 1.5f);
            blackboard.Set("Speed", 9.25f);
        });
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        var entry = entries.ShouldHaveSingleItem();
        entry.ValueType.ShouldBe("Single");
        entry.Value.ShouldNotBeNull();
        entry.Value!.Value.GetSingle().ShouldBe(9.25f);
    }

    [Fact]
    public async Task GetBlackboardAsync_WithEmptyBlackboard_ReturnsNoEntries()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, _ => { });
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken);

        entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetBlackboardAsync_AgreesWithGetBlackboardValueAsync_ForValueTypes()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard => blackboard.Set("Speed", 1.5f));
        using var bridge = new InProcessBridge(world);

        var listed = (await bridge.AI.GetBlackboardAsync(entity.Id, TestContext.Current.CancellationToken)).ShouldHaveSingleItem();
        var single = await bridge.AI.GetBlackboardValueAsync(entity.Id, "Speed", TestContext.Current.CancellationToken);

        single.ShouldNotBeNull();
        listed.ValueType.ShouldBe(single!.ValueType);
        listed.ValueString.ShouldBe(single.ValueString);
    }

    [Fact]
    public async Task GetBlackboardAsync_WithUnknownEntityId_ReturnsNoEntries()
    {
        using var world = new World();
        SpawnAIEntity(world, blackboard => blackboard.Set("Speed", 1.5f));
        using var bridge = new InProcessBridge(world);

        var entries = await bridge.AI.GetBlackboardAsync(9999, TestContext.Current.CancellationToken);

        entries.ShouldBeEmpty();
    }

    #endregion

    #region Entity resolution

    [Fact]
    public async Task GetStateMachineStateAsync_WithLiveEntity_ResolvesEntityAndReturnsSnapshot()
    {
        using var world = new World();
        var entity = SpawnAIEntity(world, blackboard => blackboard.Set("Speed", 1.5f));
        using var bridge = new InProcessBridge(world);

        // Entity versions start at 1, so an id-addressed lookup has to recover the live version.
        var snapshot = await bridge.AI.GetStateMachineStateAsync(entity.Id, TestContext.Current.CancellationToken);

        snapshot.ShouldNotBeNull();
        snapshot!.EntityId.ShouldBe(entity.Id);
        snapshot.Enabled.ShouldBeTrue();
        snapshot.BlackboardEntryCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetStateMachineStateAsync_WithUnknownEntityId_ReturnsNull()
    {
        using var world = new World();
        SpawnAIEntity(world, _ => { });
        using var bridge = new InProcessBridge(world);

        var snapshot = await bridge.AI.GetStateMachineStateAsync(9999, TestContext.Current.CancellationToken);

        snapshot.ShouldBeNull();
    }

    #endregion

    #region Reflection regression guard

    [Fact]
    public void AIControllerImplSource_DoesNotUseReflection()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "KeenEyes.TestBridge",
            "AIImpl",
            "AIControllerImpl.cs"));

        source.ShouldNotContain("BindingFlags");
        source.ShouldNotContain("GetField");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KeenEyes.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("Could not locate the repository root (KeenEyes.slnx) from the test output directory.");
        return directory!.FullName;
    }

    #endregion
}
