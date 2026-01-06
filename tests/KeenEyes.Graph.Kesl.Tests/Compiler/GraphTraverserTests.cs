using KeenEyes.Graph.Kesl.Compiler;

namespace KeenEyes.Graph.Kesl.Tests.Compiler;

public class GraphTraverserTests
{
    private readonly GraphTraverser traverser = new();

    #region Topological Sort Tests

    [Fact]
    public void TopologicalSort_SingleNode_ReturnsSingleNode()
    {
        using var builder = new TestGraphBuilder();
        var root = builder.CreateComputeShader();

        var sorted = traverser.TopologicalSort(builder.Canvas, builder.World);

        Assert.Single(sorted);
        Assert.Equal(root, sorted[0]);
    }

    [Fact]
    public void TopologicalSort_LinearChain_ReturnsInDependencyOrder()
    {
        using var builder = new TestGraphBuilder();
        var constant = builder.CreateFloatConstant(1.0f);
        var add = builder.CreateAddNode();
        var compute = builder.CreateComputeShader();

        // constant -> add -> compute (via execute port)
        builder.Connect(constant, 0, add, 0);

        var sorted = traverser.TopologicalSort(builder.Canvas, builder.World);

        // Dependencies should come before dependents
        var constantIndex = sorted.IndexOf(constant);
        var addIndex = sorted.IndexOf(add);

        Assert.True(constantIndex < addIndex, "Constant should come before Add in topological order");
    }

    [Fact]
    public void TopologicalSort_MultipleInputs_BothDependenciesFirst()
    {
        using var builder = new TestGraphBuilder();
        var const1 = builder.CreateFloatConstant(1.0f);
        var const2 = builder.CreateFloatConstant(2.0f);
        var add = builder.CreateAddNode();

        builder.Connect(const1, 0, add, 0); // Left input
        builder.Connect(const2, 0, add, 1); // Right input

        var sorted = traverser.TopologicalSort(builder.Canvas, builder.World);

        var const1Index = sorted.IndexOf(const1);
        var const2Index = sorted.IndexOf(const2);
        var addIndex = sorted.IndexOf(add);

        Assert.True(const1Index < addIndex);
        Assert.True(const2Index < addIndex);
    }

    [Fact]
    public void TopologicalSort_SharedDependency_DependencyProcessedOnce()
    {
        using var builder = new TestGraphBuilder();
        var shared = builder.CreateFloatConstant(1.0f);
        var add1 = builder.CreateAddNode();
        var add2 = builder.CreateAddNode();

        // shared connects to both add nodes
        builder.Connect(shared, 0, add1, 0);
        builder.Connect(shared, 0, add2, 0);

        var sorted = traverser.TopologicalSort(builder.Canvas, builder.World);

        // Shared should appear exactly once
        var sharedCount = sorted.Count(n => n == shared);
        Assert.Equal(1, sharedCount);

        // And before both add nodes
        var sharedIndex = sorted.IndexOf(shared);
        var add1Index = sorted.IndexOf(add1);
        var add2Index = sorted.IndexOf(add2);

        Assert.True(sharedIndex < add1Index);
        Assert.True(sharedIndex < add2Index);
    }

    #endregion

    #region Input Connection Tests

    [Fact]
    public void GetInputConnections_NoConnections_ReturnsEmpty()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateAddNode();

        var inputs = traverser.GetInputConnections(node, 0, builder.Canvas, builder.World).ToList();

        Assert.Empty(inputs);
    }

    [Fact]
    public void GetInputConnections_SingleConnection_ReturnsSource()
    {
        using var builder = new TestGraphBuilder();
        var source = builder.CreateFloatConstant(1.0f);
        var target = builder.CreateAddNode();

        builder.Connect(source, 0, target, 0);

        var inputs = traverser.GetInputConnections(target, 0, builder.Canvas, builder.World).ToList();

        Assert.Single(inputs);
        Assert.Equal(source, inputs[0].SourceNode);
        Assert.Equal(0, inputs[0].SourcePort);
    }

    [Fact]
    public void GetInputConnections_MultiplePortConnections_ReturnsCorrectPort()
    {
        using var builder = new TestGraphBuilder();
        var source1 = builder.CreateFloatConstant(1.0f);
        var source2 = builder.CreateFloatConstant(2.0f);
        var add = builder.CreateAddNode();

        builder.Connect(source1, 0, add, 0); // Port 0
        builder.Connect(source2, 0, add, 1); // Port 1

        var port0Inputs = traverser.GetInputConnections(add, 0, builder.Canvas, builder.World).ToList();
        var port1Inputs = traverser.GetInputConnections(add, 1, builder.Canvas, builder.World).ToList();

        Assert.Single(port0Inputs);
        Assert.Equal(source1, port0Inputs[0].SourceNode);

        Assert.Single(port1Inputs);
        Assert.Equal(source2, port1Inputs[0].SourceNode);
    }

    #endregion
}

file static class ListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(list[i], item))
            {
                return i;
            }
        }
        return -1;
    }
}
