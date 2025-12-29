using KeenEyes.Network;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="QuantizedAttribute"/> class.
/// </summary>
public class QuantizedAttributeTests
{
    [Fact]
    public void Min_IsSetFromConstructor()
    {
        var attr = new QuantizedAttribute(-100f, 100f, 0.01f);

        Assert.Equal(-100f, attr.Min);
    }

    [Fact]
    public void Max_IsSetFromConstructor()
    {
        var attr = new QuantizedAttribute(-100f, 100f, 0.01f);

        Assert.Equal(100f, attr.Max);
    }

    [Fact]
    public void Resolution_IsSetFromConstructor()
    {
        var attr = new QuantizedAttribute(-100f, 100f, 0.01f);

        Assert.Equal(0.01f, attr.Resolution);
    }

    [Fact]
    public void BitsRequired_CalculatesCorrectly()
    {
        // Range 0-10 with resolution 1 = 11 values = 4 bits
        var attr = new QuantizedAttribute(0f, 10f, 1f);

        Assert.True(attr.BitsRequired >= 4);
    }

    [Fact]
    public void CanApplyToField()
    {
        // Verify the attribute can be applied to fields
        var type = typeof(TestComponent);
        var field = type.GetField(nameof(TestComponent.Position))!;
        var attrs = field.GetCustomAttributes(typeof(QuantizedAttribute), false);

        Assert.Single(attrs);
    }

    [Fact]
    public void AttributeUsage_AllowsFieldsAndProperties()
    {
        var usage = typeof(QuantizedAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false);
        Assert.Single(usage);

        var attr = (AttributeUsageAttribute)usage[0];
        Assert.True(attr.ValidOn.HasFlag(AttributeTargets.Field));
        Assert.True(attr.ValidOn.HasFlag(AttributeTargets.Property));
    }

#pragma warning disable CS0649 // Field is never assigned to
    private struct TestComponent
    {
        [Quantized(-100f, 100f, 0.01f)]
        public float Position;
    }
#pragma warning restore CS0649
}
