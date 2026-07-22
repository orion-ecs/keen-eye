using BenchmarkDotNet.Attributes;

using KeenEyes;
using KeenEyes.Editor.Application;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Measures the data work behind a hierarchy-panel refresh: the root-entity scan plus the
/// recursive parent/child/name walk that <c>HierarchyPanel.RefreshHierarchy</c> performs to
/// build its tree. Widget creation and GPU drawing are deliberately excluded - only the
/// traversal that produces the node list is timed.
/// </summary>
[MemoryDiagnoser]
public class HierarchyRefreshBenchmarks
{
    private EditorWorldManager worldManager = null!;
    private List<HierarchyNode> nodes = null!;

    [Params(1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        worldManager = new EditorWorldManager();
        SceneGenerator.Generate(
            worldManager.World,
            EntityCount,
            sceneRoot: worldManager.CurrentSceneRoot);

        nodes = [with(EntityCount)];
    }

    [GlobalCleanup]
    public void Cleanup() => worldManager.Dispose();

    /// <summary>
    /// Rebuilds the flattened hierarchy node list from the current scene.
    /// </summary>
    [Benchmark]
    public int RefreshTreeData()
    {
        nodes.Clear();

        foreach (var root in worldManager.GetRootEntities())
        {
            AddNode(root, -1, 0);
        }

        return nodes.Count;
    }

    private void AddNode(Entity entity, int parentIndex, int depth)
    {
        var index = nodes.Count;
        nodes.Add(new HierarchyNode(entity, worldManager.GetEntityName(entity), parentIndex, depth));

        foreach (var child in worldManager.GetChildren(entity))
        {
            AddNode(child, index, depth + 1);
        }
    }

    /// <summary>
    /// A flattened hierarchy tree node, mirroring the per-entity data the panel materializes.
    /// </summary>
    private readonly record struct HierarchyNode(Entity Entity, string Name, int ParentIndex, int Depth);
}
