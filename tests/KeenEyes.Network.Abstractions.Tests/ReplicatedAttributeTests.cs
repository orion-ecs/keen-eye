using KeenEyes.Network;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="ReplicatedAttribute"/> class.
/// </summary>
public class ReplicatedAttributeTests
{
    [Fact]
    public void Strategy_DefaultsToAuthoritative()
    {
        var attr = new ReplicatedAttribute();

        Assert.Equal(SyncStrategy.Authoritative, attr.Strategy);
    }

    [Fact]
    public void Priority_DefaultsTo128()
    {
        var attr = new ReplicatedAttribute();

        Assert.Equal(128, attr.Priority);
    }

    [Fact]
    public void Strategy_CanBeSet()
    {
        var attr = new ReplicatedAttribute { Strategy = SyncStrategy.Interpolated };

        Assert.Equal(SyncStrategy.Interpolated, attr.Strategy);
    }

    [Fact]
    public void Priority_CanBeSet()
    {
        var attr = new ReplicatedAttribute { Priority = 200 };

        Assert.Equal(200, attr.Priority);
    }

    [Fact]
    public void Frequency_DefaultsToZero()
    {
        var attr = new ReplicatedAttribute();

        Assert.Equal(0, attr.Frequency);
    }

    [Fact]
    public void Frequency_CanBeSet()
    {
        var attr = new ReplicatedAttribute { Frequency = 60 };

        Assert.Equal(60, attr.Frequency);
    }

    [Fact]
    public void GenerateInterpolation_DefaultsToFalse()
    {
        var attr = new ReplicatedAttribute();

        Assert.False(attr.GenerateInterpolation);
    }

    [Fact]
    public void GenerateInterpolation_CanBeSet()
    {
        var attr = new ReplicatedAttribute { GenerateInterpolation = true };

        Assert.True(attr.GenerateInterpolation);
    }

    [Fact]
    public void GeneratePrediction_DefaultsToFalse()
    {
        var attr = new ReplicatedAttribute();

        Assert.False(attr.GeneratePrediction);
    }

    [Fact]
    public void GeneratePrediction_CanBeSet()
    {
        var attr = new ReplicatedAttribute { GeneratePrediction = true };

        Assert.True(attr.GeneratePrediction);
    }

    [Fact]
    public void AttributeUsage_AllowsStructs()
    {
        var usage = typeof(ReplicatedAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false);
        Assert.Single(usage);

        var attr = (AttributeUsageAttribute)usage[0];
        Assert.True(attr.ValidOn.HasFlag(AttributeTargets.Struct));
    }

    [Fact]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        var usage = typeof(ReplicatedAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false);
        Assert.Single(usage);

        var attr = (AttributeUsageAttribute)usage[0];
        Assert.False(attr.AllowMultiple);
    }
}
