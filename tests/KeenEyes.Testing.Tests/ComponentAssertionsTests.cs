namespace KeenEyes.Testing.Tests;

/// <summary>
/// Unit tests for the <see cref="ComponentAssertions"/> class.
/// </summary>
public partial class ComponentAssertionsTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct TestComponent : IEquatable<TestComponent>
    {
        public int Value;

        public readonly bool Equals(TestComponent other) => Value == other.Value;
        public override readonly bool Equals(object? obj) => obj is TestComponent other && Equals(other);
        public override readonly int GetHashCode() => Value.GetHashCode();
        public override readonly string ToString() => $"TestComponent {{ Value = {Value} }}";
    }

    [Component]
    private partial struct PositionComponent
    {
        public float X;
        public float Y;

        public override readonly string ToString() => $"Position {{ X = {X}, Y = {Y} }}";
    }

    [Component]
    private partial struct HealthComponent
    {
        public int Current;
        public int Max;
    }
#pragma warning restore CS0649

    #region ShouldEqual Tests

    [Fact]
    public void ShouldEqual_WhenEqual_DoesNotThrow()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };
        var expected = new TestComponent { Value = 42 };

        // Act & Assert - should not throw
        var result = component.ShouldEqual(expected);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ShouldEqual_WhenNotEqual_Throws()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };
        var expected = new TestComponent { Value = 100 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() => component.ShouldEqual(expected));
        Assert.Contains("TestComponent", ex.Message);
        Assert.Contains("42", ex.Message);
        Assert.Contains("100", ex.Message);
    }

    [Fact]
    public void ShouldEqual_WithReason_IncludesReasonInMessage()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };
        var expected = new TestComponent { Value = 100 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldEqual(expected, "values should match"));
        Assert.Contains("because values should match", ex.Message);
    }

    [Fact]
    public void ShouldEqual_NonEquatableComponent_UsesObjectEquals()
    {
        // Arrange
        var component = new PositionComponent { X = 10, Y = 20 };
        var expected = new PositionComponent { X = 10, Y = 20 };

        // Act & Assert - should not throw (default struct comparison)
        var result = component.ShouldEqual(expected);
        Assert.Equal(10, result.X);
    }

    #endregion

    #region ShouldNotEqual Tests

    [Fact]
    public void ShouldNotEqual_WhenNotEqual_DoesNotThrow()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };
        var unexpected = new TestComponent { Value = 100 };

        // Act & Assert - should not throw
        var result = component.ShouldNotEqual(unexpected);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ShouldNotEqual_WhenEqual_Throws()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };
        var unexpected = new TestComponent { Value = 42 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() => component.ShouldNotEqual(unexpected));
        Assert.Contains("TestComponent", ex.Message);
        Assert.Contains("not equal", ex.Message);
    }

    #endregion

    #region ShouldMatch Tests

    [Fact]
    public void ShouldMatch_WhenPredicateIsTrue_DoesNotThrow()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };

        // Act & Assert - should not throw
        var result = component.ShouldMatch(c => c.Value > 0);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ShouldMatch_WhenPredicateIsFalse_Throws()
    {
        // Arrange
        var component = new TestComponent { Value = -5 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldMatch(c => c.Value > 0));
        Assert.Contains("TestComponent", ex.Message);
        Assert.Contains("match predicate", ex.Message);
    }

    [Fact]
    public void ShouldMatch_WithNullPredicate_ThrowsArgumentNull()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            component.ShouldMatch(null!));
    }

    #endregion

    #region ShouldNotMatch Tests

    [Fact]
    public void ShouldNotMatch_WhenPredicateIsFalse_DoesNotThrow()
    {
        // Arrange
        var component = new TestComponent { Value = -5 };

        // Act & Assert - should not throw
        var result = component.ShouldNotMatch(c => c.Value > 0);
        Assert.Equal(-5, result.Value);
    }

    [Fact]
    public void ShouldNotMatch_WhenPredicateIsTrue_Throws()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldNotMatch(c => c.Value > 0));
        Assert.Contains("TestComponent", ex.Message);
        Assert.Contains("not match predicate", ex.Message);
    }

    #endregion

    #region ShouldHaveField Tests

    [Fact]
    public void ShouldHaveField_WhenValueMatches_DoesNotThrow()
    {
        // Arrange
        var component = new PositionComponent { X = 10, Y = 20 };

        // Act & Assert - should not throw
        var result = component.ShouldHaveField(p => p.X, 10);
        Assert.Equal(10, result.X);
    }

    [Fact]
    public void ShouldHaveField_WhenValueDoesNotMatch_Throws()
    {
        // Arrange
        var component = new PositionComponent { X = 10, Y = 20 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldHaveField(p => p.X, 15));
        Assert.Contains("PositionComponent", ex.Message);
        Assert.Contains("X", ex.Message);
        Assert.Contains("15", ex.Message);
        Assert.Contains("10", ex.Message);
    }

    [Fact]
    public void ShouldHaveField_WithReason_IncludesReasonInMessage()
    {
        // Arrange
        var component = new PositionComponent { X = 10, Y = 20 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldHaveField(p => p.X, 15, "X should be at spawn point"));
        Assert.Contains("because X should be at spawn point", ex.Message);
    }

    [Fact]
    public void ShouldHaveField_CanChainMultipleAssertions()
    {
        // Arrange
        var component = new PositionComponent { X = 10, Y = 20 };

        // Act & Assert - chaining should work
        component
            .ShouldHaveField(p => p.X, 10)
            .ShouldHaveField(p => p.Y, 20);
    }

    #endregion

    #region ShouldHaveFieldMatching Tests

    [Fact]
    public void ShouldHaveFieldMatching_WhenPredicateIsTrue_DoesNotThrow()
    {
        // Arrange
        var component = new HealthComponent { Current = 80, Max = 100 };

        // Act & Assert - should not throw
        var result = component.ShouldHaveFieldMatching(h => h.Current, c => c >= 0 && c <= 100);
        Assert.Equal(80, result.Current);
    }

    [Fact]
    public void ShouldHaveFieldMatching_WhenPredicateIsFalse_Throws()
    {
        // Arrange
        var component = new HealthComponent { Current = 150, Max = 100 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldHaveFieldMatching(h => h.Current, c => c <= 100));
        Assert.Contains("HealthComponent", ex.Message);
        Assert.Contains("Current", ex.Message);
        Assert.Contains("150", ex.Message);
    }

    #endregion

    #region ShouldHaveFieldInRange Tests

    [Fact]
    public void ShouldHaveFieldInRange_WhenInRange_DoesNotThrow()
    {
        // Arrange
        var component = new HealthComponent { Current = 50, Max = 100 };

        // Act & Assert - should not throw
        var result = component.ShouldHaveFieldInRange(h => h.Current, 0, 100);
        Assert.Equal(50, result.Current);
    }

    [Fact]
    public void ShouldHaveFieldInRange_WhenBelowRange_Throws()
    {
        // Arrange
        var component = new HealthComponent { Current = -10, Max = 100 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldHaveFieldInRange(h => h.Current, 0, 100));
        Assert.Contains("HealthComponent", ex.Message);
        Assert.Contains("Current", ex.Message);
        Assert.Contains("-10", ex.Message);
        Assert.Contains("[0, 100]", ex.Message);
    }

    [Fact]
    public void ShouldHaveFieldInRange_WhenAboveRange_Throws()
    {
        // Arrange
        var component = new HealthComponent { Current = 150, Max = 100 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() =>
            component.ShouldHaveFieldInRange(h => h.Current, 0, 100));
        Assert.Contains("HealthComponent", ex.Message);
        Assert.Contains("150", ex.Message);
    }

    [Fact]
    public void ShouldHaveFieldInRange_AtMinBoundary_DoesNotThrow()
    {
        // Arrange
        var component = new HealthComponent { Current = 0, Max = 100 };

        // Act & Assert - should not throw (inclusive)
        component.ShouldHaveFieldInRange(h => h.Current, 0, 100);
    }

    [Fact]
    public void ShouldHaveFieldInRange_AtMaxBoundary_DoesNotThrow()
    {
        // Arrange
        var component = new HealthComponent { Current = 100, Max = 100 };

        // Act & Assert - should not throw (inclusive)
        component.ShouldHaveFieldInRange(h => h.Current, 0, 100);
    }

    #endregion

    #region ShouldBeDefault Tests

    [Fact]
    public void ShouldBeDefault_WhenDefault_DoesNotThrow()
    {
        // Arrange
        var component = new TestComponent();

        // Act & Assert - should not throw
        var result = component.ShouldBeDefault();
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void ShouldBeDefault_WhenNotDefault_Throws()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() => component.ShouldBeDefault());
        Assert.Contains("TestComponent", ex.Message);
        Assert.Contains("default", ex.Message);
    }

    #endregion

    #region ShouldNotBeDefault Tests

    [Fact]
    public void ShouldNotBeDefault_WhenNotDefault_DoesNotThrow()
    {
        // Arrange
        var component = new TestComponent { Value = 42 };

        // Act & Assert - should not throw
        var result = component.ShouldNotBeDefault();
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ShouldNotBeDefault_WhenDefault_Throws()
    {
        // Arrange
        var component = new TestComponent();

        // Act & Assert
        var ex = Assert.Throws<AssertionException>(() => component.ShouldNotBeDefault());
        Assert.Contains("TestComponent", ex.Message);
        Assert.Contains("not be default", ex.Message);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ComponentAssertions_WithWorld_WorksWithGetComponent()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Act
        var component = world.Get<TestComponent>(entity);

        // Assert
        component.ShouldEqual(new TestComponent { Value = 42 });
        component.ShouldMatch(c => c.Value > 0);
        component.ShouldHaveField(c => c.Value, 42);
        component.ShouldNotBeDefault();
    }

    [Fact]
    public void ComponentAssertions_Chaining_WorksCorrectly()
    {
        // Arrange
        var component = new HealthComponent { Current = 80, Max = 100 };

        // Act & Assert - all assertions should pass and chain
        component
            .ShouldMatch(h => h.Current <= h.Max)
            .ShouldHaveField(h => h.Current, 80)
            .ShouldHaveField(h => h.Max, 100)
            .ShouldHaveFieldInRange(h => h.Current, 0, 100)
            .ShouldNotBeDefault();
    }

    #endregion
}
