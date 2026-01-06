using KeenEyes.AI;
using KeenEyes.AI.Utility;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.Utility;

/// <summary>
/// Tests for the Consideration class.
/// </summary>
public class ConsiderationTests
{
    #region Evaluate Tests

    [Fact]
    public void Evaluate_WithValidInputAndCurve_ReturnsTransformedValue()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.5f),
            Curve = new LinearCurve { Slope = 1f }
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(0.5f);
    }

    [Fact]
    public void Evaluate_WithNullInput_ReturnsOne()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = null,
            Curve = new LinearCurve()
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(1f);
    }

    [Fact]
    public void Evaluate_WithNullCurve_ReturnsOne()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.5f),
            Curve = null
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(1f);
    }

    [Fact]
    public void Evaluate_WithBothNull_ReturnsOne()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = null,
            Curve = null
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(1f);
    }

    [Fact]
    public void Evaluate_AppliesCurveTransformation()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        // Input returns 0.5, curve inverts it to return 0.5 (1 - 0.5)
        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.5f),
            Curve = new LinearCurve { Slope = -1f, YShift = 1f }
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(0.5f);
    }

    [Fact]
    public void Evaluate_WithZeroInput_ReturnsZeroForIdentityCurve()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0f),
            Curve = new LinearCurve { Slope = 1f }
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(0f);
    }

    [Fact]
    public void Evaluate_WithOneInput_ReturnsOneForIdentityCurve()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(1f),
            Curve = new LinearCurve { Slope = 1f }
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(1f);
    }

    #endregion

    #region Debug Value Tests

    [Fact]
    public void Evaluate_StoresLastInputValue()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.7f),
            Curve = new LinearCurve { Slope = 1f }
        };

        consideration.Evaluate(entity, blackboard, world);

        consideration.LastInputValue.ShouldBe(0.7f);
    }

    [Fact]
    public void Evaluate_StoresLastOutputValue()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.5f),
            Curve = new ExponentialCurve { Exponent = 2f } // 0.5^2 = 0.25
        };

        consideration.Evaluate(entity, blackboard, world);

        consideration.LastOutputValue.ShouldBe(0.25f);
    }

    [Fact]
    public void Evaluate_WithNullInputAndCurve_SetsDebugValuesToOne()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = null,
            Curve = null
        };

        consideration.Evaluate(entity, blackboard, world);

        consideration.LastInputValue.ShouldBe(1f);
        consideration.LastOutputValue.ShouldBe(1f);
    }

    #endregion

    #region Different Curve Type Tests

    [Fact]
    public void Evaluate_WithStepCurve_WorksCorrectly()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.6f),
            Curve = new StepCurve { Threshold = 0.5f, LowValue = 0f, HighValue = 1f }
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(1f); // Above threshold
    }

    [Fact]
    public void Evaluate_WithLogisticCurve_WorksCorrectly()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var consideration = new Consideration
        {
            Name = "Test",
            Input = new TestConsiderationInput(0.5f),
            Curve = new LogisticCurve { Midpoint = 0.5f, Steepness = 10f }
        };

        var result = consideration.Evaluate(entity, blackboard, world);

        result.ShouldBe(0.5f, 0.001); // At midpoint
    }

    #endregion
}

/// <summary>
/// Test consideration input that returns a fixed value.
/// </summary>
internal sealed class TestConsiderationInput(float value) : IConsiderationInput
{
    public float GetValue(Entity entity, Blackboard blackboard, IWorld world)
    {
        return value;
    }
}
