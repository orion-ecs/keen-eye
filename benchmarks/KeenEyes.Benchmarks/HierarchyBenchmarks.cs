using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for entity hierarchy operations: parent-child relationships and traversal.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class HierarchyBenchmarks
{
    private World world = null!;
    private Entity root = default;
    private Entity[] children = null!;
    private Entity deepLeaf = default;

    [Params(10, 100, 1000)]
    public int ChildCount { get; set; }

    [Params(1, 5, 10)]
    public int Depth { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create root entity
        root = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();

        // Create children for flat hierarchy benchmarks
        children = new Entity[ChildCount];
        for (var i = 0; i < ChildCount; i++)
        {
            children[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
            world.SetParent(children[i], root);
        }

        // Create deep hierarchy for depth benchmarks
        var parent = root;
        for (var d = 0; d < Depth; d++)
        {
            var child = world.Spawn()
                .With(new Position { X = d, Y = d })
                .Build();
            world.SetParent(child, parent);
            parent = child;
        }
        deepLeaf = parent;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of setting a parent (includes cycle detection).
    /// </summary>
    [Benchmark]
    public void SetParent()
    {
        var child = world.Spawn().Build();
        world.SetParent(child, root);
        world.Despawn(child);
    }

    /// <summary>
    /// Measures the cost of getting a parent (O(1) lookup).
    /// </summary>
    [Benchmark]
    public Entity GetParent()
    {
        return world.GetParent(children[0]);
    }

    /// <summary>
    /// Measures the cost of enumerating all children.
    /// </summary>
    [Benchmark]
    public int GetChildrenCount()
    {
        var count = 0;
        foreach (var _ in world.GetChildren(root))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures the cost of getting descendants (breadth-first traversal).
    /// </summary>
    [Benchmark]
    public int GetDescendantsCount()
    {
        var count = 0;
        foreach (var _ in world.GetDescendants(root))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures the cost of getting ancestors (walking up hierarchy).
    /// </summary>
    [Benchmark]
    public int GetAncestorsCount()
    {
        var count = 0;
        foreach (var _ in world.GetAncestors(deepLeaf))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures the cost of finding the root entity.
    /// </summary>
    [Benchmark]
    public Entity GetRoot()
    {
        return world.GetRoot(deepLeaf);
    }

    /// <summary>
    /// Measures the cost of removing a child relationship.
    /// </summary>
    [Benchmark]
    public bool RemoveChild()
    {
        // Set up relationship
        var child = world.Spawn().Build();
        world.SetParent(child, root);

        // Measure removal
        var result = world.RemoveChild(root, child);

        world.Despawn(child);
        return result;
    }
}

/// <summary>
/// Benchmarks for recursive despawn operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DespawnRecursiveBenchmarks
{
    private World world = null!;
    private Entity root = default;

    [Params(10, 100)]
    public int TreeSize { get; set; }

    [IterationSetup]
    public void SetupIteration()
    {
        world = new World();

        // Create a tree structure
        root = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();

        // Add children in a balanced way
        var entities = new List<Entity> { root };
        var created = 1;
        var index = 0;

        while (created < TreeSize && index < entities.Count)
        {
            var parent = entities[index];
            // Add up to 3 children per node
            for (var i = 0; i < 3 && created < TreeSize; i++)
            {
                var child = world.Spawn()
                    .With(new Position { X = created, Y = created })
                    .Build();
                world.SetParent(child, parent);
                entities.Add(child);
                created++;
            }
            index++;
        }
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of recursively despawning an entity tree.
    /// </summary>
    [Benchmark]
    public int DespawnRecursive()
    {
        return world.DespawnRecursive(root);
    }
}
