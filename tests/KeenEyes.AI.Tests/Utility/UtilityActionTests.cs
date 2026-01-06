using KeenEyes.AI;
using KeenEyes.AI.Utility;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.Utility;

/// <summary>
/// Tests for the UtilityAction class.
/// </summary>
public class UtilityActionTests
{
    #region CalculateScore Tests

    [Fact]
    public void CalculateScore_WithNoConsiderations_ReturnsWeight()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 0.5f,
            Considerations = []
        };

        var score = action.CalculateScore(entity, blackboard, world);

        score.ShouldBe(0.5f);
    }

    [Fact]
    public void CalculateScore_WithSingleConsideration_ReturnsWeightTimesConsideration()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 1f,
            Considerations = [
                new Consideration
                {
                    Name = "Test",
                    Input = new TestConsiderationInput(0.5f),
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        var score = action.CalculateScore(entity, blackboard, world);

        // With compensation factor: 1 consideration means modFactor = 0
        // So score remains 0.5 * 1 = 0.5
        score.ShouldBe(0.5f);
    }

    [Fact]
    public void CalculateScore_MultipliesConsiderations()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 1f,
            Considerations = [
                new Consideration
                {
                    Name = "C1",
                    Input = new TestConsiderationInput(0.8f),
                    Curve = new LinearCurve { Slope = 1f }
                },
                new Consideration
                {
                    Name = "C2",
                    Input = new TestConsiderationInput(0.5f),
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        var score = action.CalculateScore(entity, blackboard, world);

        // Base: 1.0 * 0.8 * 0.5 = 0.4
        // With compensation for 2 considerations
        score.ShouldBeGreaterThan(0.4f);
        score.ShouldBeLessThanOrEqualTo(1f);
    }

    [Fact]
    public void CalculateScore_WithZeroConsideration_ReturnsZero()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 1f,
            Considerations = [
                new Consideration
                {
                    Name = "Zero",
                    Input = new TestConsiderationInput(0f),
                    Curve = new LinearCurve { Slope = 1f }
                },
                new Consideration
                {
                    Name = "Other",
                    Input = new TestConsiderationInput(0.8f),
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        var score = action.CalculateScore(entity, blackboard, world);

        score.ShouldBe(0f);
    }

    [Fact]
    public void CalculateScore_StoresLastScore()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 0.75f,
            Considerations = []
        };

        action.CalculateScore(entity, blackboard, world);

        action.LastScore.ShouldBe(0.75f);
    }

    [Fact]
    public void CalculateScore_AppliesWeight()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 2f,
            Considerations = [
                new Consideration
                {
                    Name = "Test",
                    Input = new TestConsiderationInput(0.5f),
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        var score = action.CalculateScore(entity, blackboard, world);

        // Weight 2 * consideration 0.5 = 1.0 base
        score.ShouldBe(1f);
    }

    #endregion

    #region Early Exit Tests

    [Fact]
    public void CalculateScore_WithEarlyZero_StopsEvaluating()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var evaluationCount = 0;

        var action = new UtilityAction
        {
            Name = "Test",
            Weight = 1f,
            Considerations = [
                new Consideration
                {
                    Name = "Zero",
                    Input = new TestConsiderationInput(0f),
                    Curve = new LinearCurve { Slope = 1f }
                },
                new Consideration
                {
                    Name = "Tracked",
                    Input = new TrackingConsiderationInput(() => evaluationCount++, 0.5f),
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        action.CalculateScore(entity, blackboard, world);

        // The second consideration should not be evaluated due to early exit
        evaluationCount.ShouldBe(0);
    }

    #endregion

    #region Compensation Factor Tests

    [Fact]
    public void CalculateScore_CompensatesForMultipleConsiderations()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        // Two actions with same raw scores but different number of considerations
        var action1 = new UtilityAction
        {
            Name = "OneConsideration",
            Weight = 1f,
            Considerations = [
                new Consideration
                {
                    Name = "C1",
                    Input = new TestConsiderationInput(0.64f), // 0.8 * 0.8
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        var action2 = new UtilityAction
        {
            Name = "TwoConsiderations",
            Weight = 1f,
            Considerations = [
                new Consideration
                {
                    Name = "C1",
                    Input = new TestConsiderationInput(0.8f),
                    Curve = new LinearCurve { Slope = 1f }
                },
                new Consideration
                {
                    Name = "C2",
                    Input = new TestConsiderationInput(0.8f),
                    Curve = new LinearCurve { Slope = 1f }
                }
            ]
        };

        var score1 = action1.CalculateScore(entity, blackboard, world);
        var score2 = action2.CalculateScore(entity, blackboard, world);

        // Score2 should be compensated upward to be more fair
        // Without compensation: 0.8 * 0.8 = 0.64
        // With compensation: higher than 0.64
        score2.ShouldBeGreaterThan(0.64f);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void UtilityAction_DefaultWeight_IsOne()
    {
        var action = new UtilityAction();

        action.Weight.ShouldBe(1f);
    }

    [Fact]
    public void UtilityAction_DefaultConsiderations_IsEmpty()
    {
        var action = new UtilityAction();

        action.Considerations.ShouldBeEmpty();
    }

    [Fact]
    public void UtilityAction_DefaultName_IsEmpty()
    {
        var action = new UtilityAction();

        action.Name.ShouldBeEmpty();
    }

    #endregion
}

/// <summary>
/// Test input that tracks when it's evaluated.
/// </summary>
internal sealed class TrackingConsiderationInput(Action onEvaluate, float value) : IConsiderationInput
{
    public float GetValue(Entity entity, Blackboard blackboard, IWorld world)
    {
        onEvaluate();
        return value;
    }
}
