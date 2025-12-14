namespace KeenEyes.Tests;

/// <summary>
/// Tests for parallel system batching based on component dependencies.
/// </summary>
public class ParallelSystemBatcherTests
{
    #region Test Components

    public struct Position : IComponent
    {
        public float X, Y;
    }

    public struct Velocity : IComponent
    {
        public float X, Y;
    }

    public struct Health : IComponent
    {
        public int Current, Max;
    }

    public struct Damage : IComponent
    {
        public int Amount;
    }

    public struct Armor : IComponent
    {
        public int Value;
    }

    #endregion

    #region Test Systems

    private sealed class MovementSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Velocity>().Writes<Position>();
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class PhysicsSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Writes<Velocity>(); // Writes Velocity
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class DamageSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Damage>().Writes<Health>();
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class HealingSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Writes<Health>(); // Conflicts with DamageSystem
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class ArmorSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Armor>(); // Read-only, no conflicts
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class ReadOnlyPositionSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Position>();
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class EmptyDependencySystem : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    #endregion

    #region CreateBatches Tests

    [Fact]
    public void CreateBatches_EmptyList_ReturnsEmptyBatches()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var batches = batcher.CreateBatches([]);

        Assert.Empty(batches);
    }

    [Fact]
    public void CreateBatches_SingleSystem_ReturnsSingleBatch()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);
        var system = new MovementSystem();
        tracker.RegisterSystem(system);

        var batches = batcher.CreateBatches([system]);

        Assert.Single(batches);
        Assert.Single(batches[0].Systems);
        Assert.Same(system, batches[0].Systems[0]);
    }

    [Fact]
    public void CreateBatches_NoConflicts_SingleBatch()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // Movement (writes Position) and Damage (writes Health) don't conflict
        var movementSystem = new MovementSystem();
        var damageSystem = new DamageSystem();
        tracker.RegisterSystem(movementSystem);
        tracker.RegisterSystem(damageSystem);

        var batches = batcher.CreateBatches([movementSystem, damageSystem]);

        Assert.Single(batches);
        Assert.Equal(2, batches[0].Count);
    }

    [Fact]
    public void CreateBatches_WriteWriteConflict_SeparateBatches()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // Both write Health - must be separate batches
        var damageSystem = new DamageSystem();
        var healingSystem = new HealingSystem();
        tracker.RegisterSystem(damageSystem);
        tracker.RegisterSystem(healingSystem);

        var batches = batcher.CreateBatches([damageSystem, healingSystem]);

        Assert.Equal(2, batches.Count);
        Assert.Single(batches[0].Systems);
        Assert.Single(batches[1].Systems);
    }

    [Fact]
    public void CreateBatches_ReadWriteConflict_SeparateBatches()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // Movement reads Velocity, Physics writes Velocity - conflict
        var movementSystem = new MovementSystem();
        var physicsSystem = new PhysicsSystem();
        tracker.RegisterSystem(movementSystem);
        tracker.RegisterSystem(physicsSystem);

        var batches = batcher.CreateBatches([movementSystem, physicsSystem]);

        Assert.Equal(2, batches.Count);
    }

    [Fact]
    public void CreateBatches_ReadReadNoConflict_SingleBatch()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // Both read Position - no conflict
        var readOnlySystem1 = new ReadOnlyPositionSystem();
        var armorSystem = new ArmorSystem();
        tracker.RegisterSystem(readOnlySystem1);
        tracker.RegisterSystem(armorSystem);

        // Also add a system that reads Position
        var readOnlySystem2 = new ReadOnlyPositionSystem();
        tracker.RegisterSystem(readOnlySystem2);

        var batches = batcher.CreateBatches([readOnlySystem1, readOnlySystem2]);

        Assert.Single(batches);
        Assert.Equal(2, batches[0].Count);
    }

    [Fact]
    public void CreateBatches_ComplexScenario_CorrectGrouping()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // Systems:
        // 1. MovementSystem: reads Velocity, writes Position
        // 2. PhysicsSystem: writes Velocity
        // 3. DamageSystem: reads Damage, writes Health
        // 4. HealingSystem: writes Health
        // 5. ArmorSystem: reads Armor

        var movementSystem = new MovementSystem();
        var physicsSystem = new PhysicsSystem();
        var damageSystem = new DamageSystem();
        var healingSystem = new HealingSystem();
        var armorSystem = new ArmorSystem();

        tracker.RegisterSystem(movementSystem);
        tracker.RegisterSystem(physicsSystem);
        tracker.RegisterSystem(damageSystem);
        tracker.RegisterSystem(healingSystem);
        tracker.RegisterSystem(armorSystem);

        // Order matters for batching - systems are processed in input order
        // Movement + Damage + Armor can run together (no conflicts)
        // Physics conflicts with Movement (Velocity)
        // Healing conflicts with Damage (Health)
        var batches = batcher.CreateBatches([
            movementSystem,
            damageSystem,
            armorSystem,
            physicsSystem, // Conflicts with Movement
            healingSystem  // Conflicts with Damage
        ]);

        // Expected:
        // Batch 1: Movement, Damage, Armor
        // Batch 2: Physics (conflicts with batch 1's Movement)
        // Batch 3: Healing (conflicts with batch 2's nothing, but came after Physics)
        // Actually, let me trace through:
        // - Movement: batch 1
        // - Damage: no conflict with Movement, batch 1
        // - Armor: no conflict with Movement or Damage, batch 1
        // - Physics: conflicts with Movement (Velocity), new batch 2
        // - Healing: no conflict with Physics, batch 2

        Assert.Equal(2, batches.Count);
        Assert.Equal(3, batches[0].Count); // Movement, Damage, Armor
        Assert.Equal(2, batches[1].Count); // Physics, Healing
    }

    [Fact]
    public void CreateBatches_UnregisteredSystem_TreatedAsEmpty()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var emptySystem = new EmptyDependencySystem();
        var damageSystem = new DamageSystem();
        tracker.RegisterSystem(damageSystem);
        // emptySystem is NOT registered

        var batches = batcher.CreateBatches([emptySystem, damageSystem]);

        // Empty dependencies don't conflict with anything
        Assert.Single(batches);
        Assert.Equal(2, batches[0].Count);
    }

    #endregion

    #region CreateTypeBatches Tests

    [Fact]
    public void CreateTypeBatches_EmptyList_ReturnsEmptyBatches()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var batches = batcher.CreateTypeBatches([]);

        Assert.Empty(batches);
    }

    [Fact]
    public void CreateTypeBatches_NoConflicts_SingleBatch()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        tracker.RegisterDependencies(typeof(MovementSystem), new ComponentDependencies(
            reads: [typeof(Velocity)],
            writes: [typeof(Position)]
        ));
        tracker.RegisterDependencies(typeof(DamageSystem), new ComponentDependencies(
            reads: [typeof(Damage)],
            writes: [typeof(Health)]
        ));

        var batches = batcher.CreateTypeBatches([typeof(MovementSystem), typeof(DamageSystem)]);

        Assert.Single(batches);
        Assert.Equal(2, batches[0].Count);
    }

    [Fact]
    public void CreateTypeBatches_WithConflicts_MultipleBatches()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        tracker.RegisterDependencies(typeof(DamageSystem), new ComponentDependencies(
            reads: [],
            writes: [typeof(Health)]
        ));
        tracker.RegisterDependencies(typeof(HealingSystem), new ComponentDependencies(
            reads: [],
            writes: [typeof(Health)]
        ));

        var batches = batcher.CreateTypeBatches([typeof(DamageSystem), typeof(HealingSystem)]);

        Assert.Equal(2, batches.Count);
    }

    #endregion

    #region Analyze Tests

    [Fact]
    public void Analyze_NoConflicts_ReturnsEmptyConflicts()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var movementSystem = new MovementSystem();
        var damageSystem = new DamageSystem();
        tracker.RegisterSystem(movementSystem);
        tracker.RegisterSystem(damageSystem);

        var analysis = batcher.Analyze([movementSystem, damageSystem]);

        Assert.Empty(analysis.Conflicts);
        Assert.Equal(1, analysis.BatchCount);
    }

    [Fact]
    public void Analyze_WithConflicts_ReturnsConflictDetails()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var damageSystem = new DamageSystem();
        var healingSystem = new HealingSystem();
        tracker.RegisterSystem(damageSystem);
        tracker.RegisterSystem(healingSystem);

        var analysis = batcher.Analyze([damageSystem, healingSystem]);

        Assert.Single(analysis.Conflicts);
        var conflict = analysis.Conflicts[0];
        Assert.Equal(typeof(DamageSystem), conflict.SystemA);
        Assert.Equal(typeof(HealingSystem), conflict.SystemB);
        Assert.Contains(typeof(Health), conflict.ConflictingComponents);
    }

    [Fact]
    public void Analyze_MaxParallelism_ReturnsLargestBatchSize()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var movementSystem = new MovementSystem();
        var damageSystem = new DamageSystem();
        var armorSystem = new ArmorSystem();
        var healingSystem = new HealingSystem();
        tracker.RegisterSystem(movementSystem);
        tracker.RegisterSystem(damageSystem);
        tracker.RegisterSystem(armorSystem);
        tracker.RegisterSystem(healingSystem);

        // Movement, Damage, Armor can be parallel (batch 1 = 3)
        // Healing conflicts with Damage (batch 2 = 1)
        var analysis = batcher.Analyze([movementSystem, damageSystem, armorSystem, healingSystem]);

        Assert.Equal(3, analysis.MaxParallelism);
    }

    [Fact]
    public void Analyze_EmptySystems_ReturnsZeroMaxParallelism()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var analysis = batcher.Analyze([]);

        Assert.Equal(0, analysis.MaxParallelism);
        Assert.Equal(0, analysis.BatchCount);
        Assert.Equal(0, analysis.ConflictCount);
    }

    #endregion

    #region SystemBatch Tests

    [Fact]
    public void SystemBatch_IsParallelizable_TrueForMultipleSystems()
    {
        var systems = new ISystem[] { new MovementSystem(), new DamageSystem() };
        var batch = new SystemBatch(systems);

        Assert.True(batch.IsParallelizable);
    }

    [Fact]
    public void SystemBatch_IsParallelizable_FalseForSingleSystem()
    {
        var systems = new ISystem[] { new MovementSystem() };
        var batch = new SystemBatch(systems);

        Assert.False(batch.IsParallelizable);
    }

    [Fact]
    public void SystemBatch_Count_ReturnsCorrectValue()
    {
        var systems = new ISystem[] { new MovementSystem(), new DamageSystem(), new ArmorSystem() };
        var batch = new SystemBatch(systems);

        Assert.Equal(3, batch.Count);
    }

    #endregion

    #region TypeBatch Tests

    [Fact]
    public void TypeBatch_IsParallelizable_TrueForMultipleTypes()
    {
        var types = new Type[] { typeof(MovementSystem), typeof(DamageSystem) };
        var batch = new TypeBatch(types);

        Assert.True(batch.IsParallelizable);
    }

    [Fact]
    public void TypeBatch_IsParallelizable_FalseForSingleType()
    {
        var types = new Type[] { typeof(MovementSystem) };
        var batch = new TypeBatch(types);

        Assert.False(batch.IsParallelizable);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CreateBatches_MaintainsInputOrder_WithinBatch()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        var movement = new MovementSystem();
        var damage = new DamageSystem();
        var armor = new ArmorSystem();
        tracker.RegisterSystem(movement);
        tracker.RegisterSystem(damage);
        tracker.RegisterSystem(armor);

        var batches = batcher.CreateBatches([movement, damage, armor]);

        Assert.Single(batches);
        Assert.Same(movement, batches[0].Systems[0]);
        Assert.Same(damage, batches[0].Systems[1]);
        Assert.Same(armor, batches[0].Systems[2]);
    }

    [Fact]
    public void CreateBatches_ChainOfConflicts_CreatesSeparateBatches()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // Create a chain: A writes X, B reads X and writes Y, C reads Y
        // A and B conflict (X), B and C conflict (Y)
        tracker.RegisterDependencies(typeof(MovementSystem), new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        ));
        tracker.RegisterDependencies(typeof(PhysicsSystem), new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        ));
        tracker.RegisterDependencies(typeof(DamageSystem), new ComponentDependencies(
            reads: [typeof(Velocity)],
            writes: []
        ));

        var movementSystem = new MovementSystem();
        var physicsSystem = new PhysicsSystem();
        var damageSystem = new DamageSystem();

        var batches = batcher.CreateBatches([movementSystem, physicsSystem, damageSystem]);

        // Movement: batch 1
        // Physics: conflicts with Movement, batch 2
        // Damage: conflicts with Physics, batch 3
        Assert.Equal(3, batches.Count);
    }

    [Fact]
    public void CreateBatches_AllReadOnly_SingleBatch()
    {
        var tracker = new SystemDependencyTracker();
        var batcher = new ParallelSystemBatcher(tracker);

        // All systems only read - no conflicts
        tracker.RegisterDependencies(typeof(MovementSystem), new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity)],
            writes: []
        ));
        tracker.RegisterDependencies(typeof(PhysicsSystem), new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity)],
            writes: []
        ));
        tracker.RegisterDependencies(typeof(DamageSystem), new ComponentDependencies(
            reads: [typeof(Position), typeof(Health)],
            writes: []
        ));

        var movementSystem = new MovementSystem();
        var physicsSystem = new PhysicsSystem();
        var damageSystem = new DamageSystem();

        var batches = batcher.CreateBatches([movementSystem, physicsSystem, damageSystem]);

        Assert.Single(batches);
        Assert.Equal(3, batches[0].Count);
    }

    #endregion
}
