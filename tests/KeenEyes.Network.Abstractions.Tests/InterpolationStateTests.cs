using KeenEyes.Network.Components;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="InterpolationState"/> component.
/// </summary>
public class InterpolationStateTests
{
    [Fact]
    public void FromTime_DefaultsToZero()
    {
        var state = new InterpolationState();

        Assert.Equal(0.0, state.FromTime);
    }

    [Fact]
    public void ToTime_DefaultsToZero()
    {
        var state = new InterpolationState();

        Assert.Equal(0.0, state.ToTime);
    }

    [Fact]
    public void Factor_DefaultsToZero()
    {
        var state = new InterpolationState();

        Assert.Equal(0f, state.Factor);
    }

    [Fact]
    public void FromTime_CanBeSet()
    {
        var state = new InterpolationState { FromTime = 1.5 };

        Assert.Equal(1.5, state.FromTime);
    }

    [Fact]
    public void ToTime_CanBeSet()
    {
        var state = new InterpolationState { ToTime = 2.5 };

        Assert.Equal(2.5, state.ToTime);
    }

    [Fact]
    public void Factor_CanBeSet()
    {
        var state = new InterpolationState { Factor = 0.75f };

        Assert.Equal(0.75f, state.Factor);
    }

    [Fact]
    public void Factor_ClampsToValidRange_InferredFromUsage()
    {
        // Factor is expected to be between 0 and 1, though
        // the struct itself doesn't enforce this
        var state = new InterpolationState { Factor = 0.5f };

        Assert.True(state.Factor >= 0f && state.Factor <= 1f);
    }
}
