using KeenEyes.Graph.Kesl.Preview;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests.Preview;

/// <summary>
/// Tests for <see cref="PreviewEntityManager"/>.
/// </summary>
public class PreviewEntityManagerTests
{
    private static readonly SourceLocation testLocation = new("test", 0, 0);

    #region RebuildFromBindings Tests

    [Fact]
    public void RebuildFromBindings_CreatesEntitiesWithComponents()
    {
        var manager = new PreviewEntityManager();
        var bindings = new List<QueryBinding>
        {
            new(AccessMode.Write, "Position", testLocation),
            new(AccessMode.Read, "Velocity", testLocation)
        };

        manager.RebuildFromBindings(bindings);

        Assert.Equal(3, manager.Entities.Count);
        Assert.All(manager.Entities, e =>
        {
            Assert.Contains("Position", e.Components.Keys);
            Assert.Contains("Velocity", e.Components.Keys);
        });
    }

    [Fact]
    public void RebuildFromBindings_ExcludesWithoutBindings()
    {
        var manager = new PreviewEntityManager();
        var bindings = new List<QueryBinding>
        {
            new(AccessMode.Write, "Position", testLocation),
            new(AccessMode.Without, "Frozen", testLocation)
        };

        manager.RebuildFromBindings(bindings);

        Assert.All(manager.Entities, e =>
        {
            Assert.Contains("Position", e.Components.Keys);
            Assert.DoesNotContain("Frozen", e.Components.Keys);
        });
    }

    [Fact]
    public void RebuildFromBindings_VariesValuesByEntityIndex()
    {
        var manager = new PreviewEntityManager();
        var bindings = new List<QueryBinding>
        {
            new(AccessMode.Write, "Position", testLocation)
        };

        manager.RebuildFromBindings(bindings);

        // Entity 0 should have X=0, entity 1 should have X=1, etc.
        Assert.Equal(0f, manager.Entities[0].Components["Position"].Fields["X"]);
        Assert.Equal(1f, manager.Entities[1].Components["Position"].Fields["X"]);
        Assert.Equal(2f, manager.Entities[2].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void RebuildFromBindings_RespectsEntityCount()
    {
        var manager = new PreviewEntityManager { EntityCount = 5 };
        var bindings = new List<QueryBinding>
        {
            new(AccessMode.Write, "Position", testLocation)
        };

        manager.RebuildFromBindings(bindings);

        Assert.Equal(5, manager.Entities.Count);
    }

    [Fact]
    public void RebuildFromBindings_ClearsExistingEntities()
    {
        var manager = new PreviewEntityManager();
        var bindings1 = new List<QueryBinding>
        {
            new(AccessMode.Write, "Position", testLocation)
        };
        var bindings2 = new List<QueryBinding>
        {
            new(AccessMode.Write, "Rotation", testLocation)
        };

        manager.RebuildFromBindings(bindings1);
        manager.RebuildFromBindings(bindings2);

        Assert.All(manager.Entities, e =>
        {
            Assert.DoesNotContain("Position", e.Components.Keys);
            Assert.Contains("Rotation", e.Components.Keys);
        });
    }

    [Fact]
    public void RebuildFromBindings_EmptyBindings_CreatesEmptyEntities()
    {
        var manager = new PreviewEntityManager();
        var bindings = new List<QueryBinding>();

        manager.RebuildFromBindings(bindings);

        Assert.Equal(3, manager.Entities.Count);
        Assert.All(manager.Entities, e => Assert.Empty(e.Components));
    }

    #endregion

    #region CloneCurrentState Tests

    [Fact]
    public void CloneCurrentState_CreatesDeepCopy()
    {
        var manager = new PreviewEntityManager();
        manager.RebuildFromBindings(new[]
        {
            new QueryBinding(AccessMode.Write, "Position", testLocation)
        });

        var clone = manager.CloneCurrentState();

        // Modify original
        manager.Entities[0].Components["Position"].Fields["X"] = 999f;

        // Clone should be unchanged
        Assert.NotEqual(999f, clone[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void CloneCurrentState_PreservesComponentTypeNames()
    {
        var manager = new PreviewEntityManager();
        manager.RebuildFromBindings(new[]
        {
            new QueryBinding(AccessMode.Write, "Position", testLocation)
        });

        var clone = manager.CloneCurrentState();

        Assert.Equal("Position", clone[0].Components["Position"].TypeName);
    }

    [Fact]
    public void CloneCurrentState_PreservesEntityIndices()
    {
        var manager = new PreviewEntityManager();
        manager.RebuildFromBindings(new[]
        {
            new QueryBinding(AccessMode.Write, "Position", testLocation)
        });

        var clone = manager.CloneCurrentState();

        Assert.Equal(0, clone[0].Index);
        Assert.Equal(1, clone[1].Index);
        Assert.Equal(2, clone[2].Index);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_RestoresInitialValues()
    {
        var manager = new PreviewEntityManager();
        manager.RebuildFromBindings(new[]
        {
            new QueryBinding(AccessMode.Write, "Position", testLocation)
        });

        var originalX = manager.Entities[0].Components["Position"].Fields["X"];

        // Modify values
        manager.Entities[0].Components["Position"].Fields["X"] = 999f;

        // Reset
        manager.Reset();

        Assert.Equal(originalX, manager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Reset_WithNoBindings_DoesNothing()
    {
        var manager = new PreviewEntityManager();

        // Should not throw
        manager.Reset();

        Assert.Empty(manager.Entities);
    }

    #endregion
}
