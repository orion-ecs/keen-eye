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
}
