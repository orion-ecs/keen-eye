using BenchmarkDotNet.Attributes;

using KeenEyes;
using KeenEyes.Editor.Commands;
using KeenEyes.Editor.Selection;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Measures selection-change latency and a representative undo/redo command cycle at scene
/// scale. The command benchmarks run a full execute -> undo -> redo -> undo cycle (net-neutral,
/// so the world state is identical every iteration) to time both the forward and inverse paths
/// of a command as it manipulates a large world.
/// </summary>
[MemoryDiagnoser]
public class SelectionUndoBenchmarks
{
    private World world = null!;
    private SelectionManager selection = null!;
    private UndoRedoManager undoRedo = null!;

    private Entity entityA;
    private Entity entityB;
    private bool toggle;

    private SetComponentCommand<Health> setComponentCommand = null!;
    private ReparentEntityCommand reparentCommand = null!;

    [Params(1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        var entities = SceneGenerator.Generate(world, EntityCount);

        selection = new SelectionManager();
        undoRedo = new UndoRedoManager();

        // Two distinct actors for the selection-change benchmark.
        entityA = entities[^1];
        entityB = entities[^2];

        // Set-component command targets a health-bearing actor with a changed value.
        var target = entities[^1];
        var newHealth = world.Has<Health>(target)
            ? world.Get<Health>(target) with { Current = 1 }
            : new Health { Current = 1, Max = 100 };
        setComponentCommand = new SetComponentCommand<Health>(world, target, newHealth);

        // Reparent command moves a deep leaf under the scene's top-level root (no cycle).
        reparentCommand = new ReparentEntityCommand(world, entities[^1], entities[0]);
    }

    [GlobalCleanup]
    public void Cleanup() => world.Dispose();

    /// <summary>
    /// A single-entity selection change (the common inspector-driving event).
    /// </summary>
    [Benchmark]
    public Entity SelectionChange()
    {
        toggle = !toggle;
        selection.Select(toggle ? entityA : entityB);
        return selection.PrimarySelection;
    }

    /// <summary>
    /// Execute/undo/redo cycle of a set-component command through the undo stack.
    /// </summary>
    [Benchmark]
    public void SetComponentUndoRedo()
    {
        undoRedo.Execute(setComponentCommand);
        undoRedo.Undo();
        undoRedo.Redo();
        undoRedo.Undo();
    }

    /// <summary>
    /// Execute/undo/redo cycle of a reparent command through the undo stack.
    /// </summary>
    [Benchmark]
    public void ReparentUndoRedo()
    {
        undoRedo.Execute(reparentCommand);
        undoRedo.Undo();
        undoRedo.Redo();
        undoRedo.Undo();
    }
}
