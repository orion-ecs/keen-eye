using KeenEyes.Network.Components;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="PredictionState"/> component.
/// </summary>
public class PredictionStateTests
{
    [Fact]
    public void LastConfirmedTick_DefaultsToZero()
    {
        var state = new PredictionState();

        Assert.Equal(0u, state.LastConfirmedTick);
    }

    [Fact]
    public void LastPredictedTick_DefaultsToZero()
    {
        var state = new PredictionState();

        Assert.Equal(0u, state.LastPredictedTick);
    }

    [Fact]
    public void MispredictionDetected_DefaultsToFalse()
    {
        var state = new PredictionState();

        Assert.False(state.MispredictionDetected);
    }

    [Fact]
    public void LastCorrectionMagnitude_DefaultsToZero()
    {
        var state = new PredictionState();

        Assert.Equal(0f, state.LastCorrectionMagnitude);
    }

    [Fact]
    public void LastConfirmedTick_CanBeSet()
    {
        var state = new PredictionState { LastConfirmedTick = 50 };

        Assert.Equal(50u, state.LastConfirmedTick);
    }

    [Fact]
    public void LastPredictedTick_CanBeSet()
    {
        var state = new PredictionState { LastPredictedTick = 100 };

        Assert.Equal(100u, state.LastPredictedTick);
    }

    [Fact]
    public void MispredictionDetected_CanBeSet()
    {
        var state = new PredictionState { MispredictionDetected = true };

        Assert.True(state.MispredictionDetected);
    }

    [Fact]
    public void LastCorrectionMagnitude_CanBeSet()
    {
        var state = new PredictionState { LastCorrectionMagnitude = 5.5f };

        Assert.Equal(5.5f, state.LastCorrectionMagnitude);
    }

    [Fact]
    public void SmoothingAvailable_DefaultsToFalse()
    {
        var state = new PredictionState();

        Assert.False(state.SmoothingAvailable);
    }

    [Fact]
    public void SmoothingAvailable_CanBeSet()
    {
        var state = new PredictionState { SmoothingAvailable = true };

        Assert.True(state.SmoothingAvailable);
    }
}
