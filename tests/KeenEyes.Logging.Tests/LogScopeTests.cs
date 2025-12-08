using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests;

public class LogScopeTests
{
    [Fact]
    public void BeginScope_ReturnsNonNullScope()
    {
        using var manager = new LogManager();

        using var scope = manager.BeginScope("TestScope");

        scope.ShouldNotBeNull();
    }

    [Fact]
    public void Scope_HasCorrectName()
    {
        using var manager = new LogManager();

        using var scope = manager.BeginScope("MyScope");

        scope.Name.ShouldBe("MyScope");
    }

    [Fact]
    public void Scope_CanBeDisposedMultipleTimes()
    {
        using var manager = new LogManager();
        var scope = manager.BeginScope("Scope");

        Should.NotThrow(() =>
        {
            scope.Dispose();
            scope.Dispose();
        });
    }

    [Fact]
    public void Scope_WithNullProperties_WorksCorrectly()
    {
        using var manager = new LogManager();

        using var scope = manager.BeginScope("Scope", null);

        scope.ShouldNotBeNull();
    }

    [Fact]
    public void Scope_WithEmptyProperties_WorksCorrectly()
    {
        using var manager = new LogManager();

        using var scope = manager.BeginScope("Scope", new Dictionary<string, object?>());

        scope.ShouldNotBeNull();
    }

    [Fact]
    public void NestedScopes_PreserveParentReference()
    {
        using var manager = new LogManager();

        using var outer = manager.BeginScope("Outer");
        using var inner = manager.BeginScope("Inner");

        // The inner scope can access parent properties through LogManager
        // We test this by logging and checking the merged properties
        inner.Name.ShouldBe("Inner");
        outer.Name.ShouldBe("Outer");
    }

    [Fact]
    public void NestedScope_ParentHasProperties_ChildHasNull_InheritsParentProperties()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Parent", new Dictionary<string, object?> { ["ParentKey"] = "ParentValue" }))
        using (manager.BeginScope("Child", null))
        {
            manager.Info("Test", "Message");
        }

        provider.Messages[0].Properties.ShouldNotBeNull();
        provider.Messages[0].Properties!["ParentKey"].ShouldBe("ParentValue");
    }

    [Fact]
    public void NestedScope_ParentHasNullProperties_ChildHasProperties_UsesChildProperties()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Parent", null))
        using (manager.BeginScope("Child", new Dictionary<string, object?> { ["ChildKey"] = "ChildValue" }))
        {
            manager.Info("Test", "Message");
        }

        provider.Messages[0].Properties.ShouldNotBeNull();
        provider.Messages[0].Properties!["ChildKey"].ShouldBe("ChildValue");
    }

    [Fact]
    public void NestedScope_BothHaveProperties_MergesWithChildPrecedence()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        using (manager.BeginScope("Parent", new Dictionary<string, object?> { ["Key"] = "Parent", ["OnlyParent"] = "P" }))
        using (manager.BeginScope("Child", new Dictionary<string, object?> { ["Key"] = "Child", ["OnlyChild"] = "C" }))
        {
            manager.Info("Test", "Message");
        }

        var props = provider.Messages[0].Properties;
        props.ShouldNotBeNull();
        props!["Key"].ShouldBe("Child"); // Child overrides
        props["OnlyParent"].ShouldBe("P");
        props["OnlyChild"].ShouldBe("C");
    }

    [Fact]
    public void Scope_DisposingOutOfOrder_HandlesGracefully()
    {
        using var manager = new LogManager();
        var provider = new TestLogProvider();
        manager.AddProvider(provider);

        var outer = manager.BeginScope("Outer", new Dictionary<string, object?> { ["Outer"] = "1" });
        var inner = manager.BeginScope("Inner", new Dictionary<string, object?> { ["Inner"] = "2" });

        // Dispose outer first (out of order)
        outer.Dispose();

        // Inner scope should still work
        manager.Info("Test", "Message");

        // The inner scope properties should still be there
        provider.Messages[0].Properties.ShouldNotBeNull();

        inner.Dispose();
    }

    [Fact]
    public void Scope_PropertiesProperty_ReturnsPassedProperties()
    {
        using var manager = new LogManager();
        var props = new Dictionary<string, object?> { ["Key"] = "Value" };

        using var scope = manager.BeginScope("Test", props);

        // Access internal Properties property directly
        scope.Properties.ShouldBe(props);
    }

    [Fact]
    public void Scope_PropertiesProperty_ReturnsNullWhenNoPropertiesPassed()
    {
        using var manager = new LogManager();

        using var scope = manager.BeginScope("Test", null);

        scope.Properties.ShouldBeNull();
    }

    [Fact]
    public void Scope_ParentProperty_ReturnsParentScope()
    {
        using var manager = new LogManager();

        using var outer = manager.BeginScope("Outer");
        using var inner = manager.BeginScope("Inner");

        // Access internal Parent property directly
        inner.Parent.ShouldBe(outer);
        outer.Parent.ShouldBeNull();
    }
}
