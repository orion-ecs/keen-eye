// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Dependencies;

namespace KeenEyes.Editor.Tests.Plugins.Dependencies;

/// <summary>
/// Tests for <see cref="DependencyGraph"/>.
/// </summary>
public sealed class DependencyGraphTests
{
    #region AddPlugin Tests

    [Fact]
    public void AddPlugin_IncreasesPluginCount()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        graph.AddPlugin("plugin-a");
        graph.AddPlugin("plugin-b");

        // Assert
        Assert.Equal(2, graph.PluginCount);
    }

    [Fact]
    public void AddPlugin_DuplicateId_DoesNotIncreaseCount()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddPlugin("plugin-a");

        // Act
        graph.AddPlugin("plugin-a");

        // Assert
        Assert.Equal(1, graph.PluginCount);
    }

    #endregion

    #region AddDependency Tests

    [Fact]
    public void AddDependency_AddsBothPlugins()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        graph.AddDependency("dependency", "dependent");

        // Assert
        Assert.Equal(2, graph.PluginCount);
    }

    [Fact]
    public void AddDependency_CreatesDependentRelationship()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        graph.AddDependency("dependency", "dependent");
        var dependents = graph.GetDependents("dependency");

        // Assert
        Assert.Contains("dependent", dependents);
    }

    #endregion

    #region TopologicalSort Tests - Simple Chain

    [Fact]
    public void TopologicalSort_SimpleChain_ReturnsDependenciesFirst()
    {
        // Arrange: A depends on B, B depends on C
        // Graph edges: C → B → A (load order should be C, B, A)
        var graph = new DependencyGraph();
        graph.AddDependency("C", "B"); // B depends on C
        graph.AddDependency("B", "A"); // A depends on B

        // Act
        var (order, cycle) = graph.TopologicalSort();
        var orderList = order.ToList();

        // Assert
        Assert.Empty(cycle);
        Assert.Equal(3, orderList.Count);
        Assert.True(orderList.IndexOf("C") < orderList.IndexOf("B"));
        Assert.True(orderList.IndexOf("B") < orderList.IndexOf("A"));
    }

    [Fact]
    public void TopologicalSort_SinglePlugin_ReturnsPlugin()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddPlugin("lonely");

        // Act
        var (order, cycle) = graph.TopologicalSort();

        // Assert
        Assert.Empty(cycle);
        Assert.Single(order);
        Assert.Equal("lonely", order[0]);
    }

    [Fact]
    public void TopologicalSort_EmptyGraph_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var (order, cycle) = graph.TopologicalSort();

        // Assert
        Assert.Empty(order);
        Assert.Empty(cycle);
    }

    #endregion

    #region TopologicalSort Tests - Diamond Pattern

    [Fact]
    public void TopologicalSort_DiamondPattern_ReturnsDependenciesFirst()
    {
        // Arrange: A depends on B and C, B and C both depend on D
        // Graph: D → B → A
        //        D → C ↗
        var graph = new DependencyGraph();
        graph.AddDependency("D", "B"); // B depends on D
        graph.AddDependency("D", "C"); // C depends on D
        graph.AddDependency("B", "A"); // A depends on B
        graph.AddDependency("C", "A"); // A depends on C

        // Act
        var (order, cycle) = graph.TopologicalSort();
        var orderList = order.ToList();

        // Assert
        Assert.Empty(cycle);
        Assert.Equal(4, orderList.Count);
        Assert.True(orderList.IndexOf("D") < orderList.IndexOf("B"));
        Assert.True(orderList.IndexOf("D") < orderList.IndexOf("C"));
        Assert.True(orderList.IndexOf("B") < orderList.IndexOf("A"));
        Assert.True(orderList.IndexOf("C") < orderList.IndexOf("A"));
    }

    #endregion

    #region TopologicalSort Tests - Cycle Detection

    [Fact]
    public void TopologicalSort_SimpleCycle_ReturnsParticipants()
    {
        // Arrange: A → B → C → A (cycle)
        var graph = new DependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");
        graph.AddDependency("C", "A"); // Creates cycle

        // Act
        var (_, cycle) = graph.TopologicalSort();

        // Assert
        Assert.NotEmpty(cycle);
        Assert.Contains("A", cycle);
        Assert.Contains("B", cycle);
        Assert.Contains("C", cycle);
    }

    [Fact]
    public void TopologicalSort_CycleWithExternalNode_ReturnsOnlyCycleParticipants()
    {
        // Arrange: D → A → B → C → A (D is not in cycle)
        var graph = new DependencyGraph();
        graph.AddDependency("D", "A");
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");
        graph.AddDependency("C", "A"); // Creates cycle

        // Act
        var (order, cycle) = graph.TopologicalSort();

        // Assert
        Assert.NotEmpty(cycle);
        // D should be in the order (it was processed before cycle was detected)
        Assert.Contains("D", order);
        // A, B, C are in the cycle
        Assert.DoesNotContain("A", order);
    }

    #endregion

    #region FindCyclePath Tests

    [Fact]
    public void FindCyclePath_WithCycle_ReturnsPath()
    {
        // Arrange: A → B → C → A
        var graph = new DependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");
        graph.AddDependency("C", "A");

        // Act
        var path = graph.FindCyclePath("A");

        // Assert
        Assert.NotEmpty(path);
        // Path should end with the start node to show the cycle
        Assert.Equal(path[0], path[^1]);
    }

    [Fact]
    public void FindCyclePath_NoCycle_ReturnsEmpty()
    {
        // Arrange: A → B → C (no cycle)
        var graph = new DependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");

        // Act
        var path = graph.FindCyclePath("A");

        // Assert
        Assert.Empty(path);
    }

    #endregion

    #region GetDependents Tests

    [Fact]
    public void GetDependents_WithDependents_ReturnsDependents()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddDependency("core", "ui");
        graph.AddDependency("core", "audio");

        // Act
        var dependents = graph.GetDependents("core");

        // Assert
        Assert.Equal(2, dependents.Count);
        Assert.Contains("ui", dependents);
        Assert.Contains("audio", dependents);
    }

    [Fact]
    public void GetDependents_NoDependents_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddPlugin("leaf");

        // Act
        var dependents = graph.GetDependents("leaf");

        // Assert
        Assert.Empty(dependents);
    }

    [Fact]
    public void GetDependents_UnknownPlugin_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var dependents = graph.GetDependents("unknown");

        // Assert
        Assert.Empty(dependents);
    }

    #endregion

    #region GetAllDependents Tests

    [Fact]
    public void GetAllDependents_ReturnsTransitiveDependents()
    {
        // Arrange: A → B → C (C depends on B, B depends on A)
        var graph = new DependencyGraph();
        graph.AddDependency("A", "B"); // B depends on A
        graph.AddDependency("B", "C"); // C depends on B

        // Act
        var allDependents = graph.GetAllDependents("A");

        // Assert
        Assert.Equal(2, allDependents.Count);
        Assert.Contains("B", allDependents);
        Assert.Contains("C", allDependents);
    }

    [Fact]
    public void GetAllDependents_DiamondPattern_ReturnsAll()
    {
        // Arrange: D → B → A, D → C → A
        var graph = new DependencyGraph();
        graph.AddDependency("D", "B");
        graph.AddDependency("D", "C");
        graph.AddDependency("B", "A");
        graph.AddDependency("C", "A");

        // Act
        var allDependents = graph.GetAllDependents("D");

        // Assert
        Assert.Equal(3, allDependents.Count);
        Assert.Contains("B", allDependents);
        Assert.Contains("C", allDependents);
        Assert.Contains("A", allDependents);
    }

    [Fact]
    public void GetAllDependents_LeafNode_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();
        graph.AddDependency("core", "leaf");

        // Act
        var allDependents = graph.GetAllDependents("leaf");

        // Assert
        Assert.Empty(allDependents);
    }

    #endregion
}
