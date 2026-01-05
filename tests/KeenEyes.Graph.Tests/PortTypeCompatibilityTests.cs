using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Tests;

public class PortTypeCompatibilityTests
{
    #region Same Type Tests

    [Theory]
    [InlineData(PortTypeId.Float)]
    [InlineData(PortTypeId.Float2)]
    [InlineData(PortTypeId.Float3)]
    [InlineData(PortTypeId.Float4)]
    [InlineData(PortTypeId.Int)]
    [InlineData(PortTypeId.Int2)]
    [InlineData(PortTypeId.Int3)]
    [InlineData(PortTypeId.Int4)]
    [InlineData(PortTypeId.Bool)]
    [InlineData(PortTypeId.Entity)]
    [InlineData(PortTypeId.Flow)]
    [InlineData(PortTypeId.Any)]
    public void CanConnect_SameType_ReturnsTrue(PortTypeId type)
    {
        Assert.True(PortTypeCompatibility.CanConnect(type, type));
    }

    #endregion

    #region Any Type Tests

    [Theory]
    [InlineData(PortTypeId.Float)]
    [InlineData(PortTypeId.Int)]
    [InlineData(PortTypeId.Bool)]
    [InlineData(PortTypeId.Entity)]
    public void CanConnect_ToAny_ReturnsTrue(PortTypeId source)
    {
        Assert.True(PortTypeCompatibility.CanConnect(source, PortTypeId.Any));
    }

    [Fact]
    public void CanConnect_FlowToAny_ReturnsTrue()
    {
        // Even Flow connects to Any
        Assert.True(PortTypeCompatibility.CanConnect(PortTypeId.Flow, PortTypeId.Any));
    }

    #endregion

    #region Float Widening Tests

    [Theory]
    [InlineData(PortTypeId.Float, PortTypeId.Float2)]
    [InlineData(PortTypeId.Float, PortTypeId.Float3)]
    [InlineData(PortTypeId.Float, PortTypeId.Float4)]
    [InlineData(PortTypeId.Float2, PortTypeId.Float3)]
    [InlineData(PortTypeId.Float2, PortTypeId.Float4)]
    [InlineData(PortTypeId.Float3, PortTypeId.Float4)]
    public void CanConnect_FloatWidening_ReturnsTrue(PortTypeId source, PortTypeId target)
    {
        Assert.True(PortTypeCompatibility.CanConnect(source, target));
    }

    [Theory]
    [InlineData(PortTypeId.Float4, PortTypeId.Float3)]
    [InlineData(PortTypeId.Float4, PortTypeId.Float2)]
    [InlineData(PortTypeId.Float4, PortTypeId.Float)]
    [InlineData(PortTypeId.Float3, PortTypeId.Float2)]
    [InlineData(PortTypeId.Float3, PortTypeId.Float)]
    [InlineData(PortTypeId.Float2, PortTypeId.Float)]
    public void CanConnect_FloatNarrowing_ReturnsFalse(PortTypeId source, PortTypeId target)
    {
        Assert.False(PortTypeCompatibility.CanConnect(source, target));
    }

    #endregion

    #region Int Widening Tests

    [Theory]
    [InlineData(PortTypeId.Int, PortTypeId.Int2)]
    [InlineData(PortTypeId.Int, PortTypeId.Int3)]
    [InlineData(PortTypeId.Int, PortTypeId.Int4)]
    [InlineData(PortTypeId.Int2, PortTypeId.Int3)]
    [InlineData(PortTypeId.Int2, PortTypeId.Int4)]
    [InlineData(PortTypeId.Int3, PortTypeId.Int4)]
    public void CanConnect_IntWidening_ReturnsTrue(PortTypeId source, PortTypeId target)
    {
        Assert.True(PortTypeCompatibility.CanConnect(source, target));
    }

    [Theory]
    [InlineData(PortTypeId.Int4, PortTypeId.Int3)]
    [InlineData(PortTypeId.Int4, PortTypeId.Int2)]
    [InlineData(PortTypeId.Int4, PortTypeId.Int)]
    [InlineData(PortTypeId.Int3, PortTypeId.Int2)]
    [InlineData(PortTypeId.Int3, PortTypeId.Int)]
    [InlineData(PortTypeId.Int2, PortTypeId.Int)]
    public void CanConnect_IntNarrowing_ReturnsFalse(PortTypeId source, PortTypeId target)
    {
        Assert.False(PortTypeCompatibility.CanConnect(source, target));
    }

    #endregion

    #region Int to Float Conversion Tests

    [Fact]
    public void CanConnect_IntToFloat_ReturnsTrue()
    {
        Assert.True(PortTypeCompatibility.CanConnect(PortTypeId.Int, PortTypeId.Float));
    }

    [Fact]
    public void CanConnect_FloatToInt_ReturnsFalse()
    {
        Assert.False(PortTypeCompatibility.CanConnect(PortTypeId.Float, PortTypeId.Int));
    }

    #endregion

    #region Flow Type Tests

    [Theory]
    [InlineData(PortTypeId.Float)]
    [InlineData(PortTypeId.Int)]
    [InlineData(PortTypeId.Bool)]
    [InlineData(PortTypeId.Entity)]
    public void CanConnect_FlowToNonFlow_ReturnsFalse(PortTypeId target)
    {
        Assert.False(PortTypeCompatibility.CanConnect(PortTypeId.Flow, target));
    }

    [Theory]
    [InlineData(PortTypeId.Float)]
    [InlineData(PortTypeId.Int)]
    [InlineData(PortTypeId.Bool)]
    [InlineData(PortTypeId.Entity)]
    public void CanConnect_NonFlowToFlow_ReturnsFalse(PortTypeId source)
    {
        Assert.False(PortTypeCompatibility.CanConnect(source, PortTypeId.Flow));
    }

    [Fact]
    public void CanConnect_FlowToFlow_ReturnsTrue()
    {
        Assert.True(PortTypeCompatibility.CanConnect(PortTypeId.Flow, PortTypeId.Flow));
    }

    #endregion

    #region Incompatible Types Tests

    [Theory]
    [InlineData(PortTypeId.Bool, PortTypeId.Float)]
    [InlineData(PortTypeId.Bool, PortTypeId.Int)]
    [InlineData(PortTypeId.Entity, PortTypeId.Float)]
    [InlineData(PortTypeId.Entity, PortTypeId.Int)]
    [InlineData(PortTypeId.Entity, PortTypeId.Bool)]
    [InlineData(PortTypeId.Float, PortTypeId.Bool)]
    [InlineData(PortTypeId.Int, PortTypeId.Bool)]
    [InlineData(PortTypeId.Float, PortTypeId.Entity)]
    [InlineData(PortTypeId.Float2, PortTypeId.Int2)]
    public void CanConnect_IncompatibleTypes_ReturnsFalse(PortTypeId source, PortTypeId target)
    {
        Assert.False(PortTypeCompatibility.CanConnect(source, target));
    }

    #endregion

    #region RequiresConversion Tests

    [Theory]
    [InlineData(PortTypeId.Float, PortTypeId.Float2)]
    [InlineData(PortTypeId.Int, PortTypeId.Float)]
    [InlineData(PortTypeId.Int, PortTypeId.Int2)]
    public void RequiresConversion_ValidWidening_ReturnsTrue(PortTypeId source, PortTypeId target)
    {
        Assert.True(PortTypeCompatibility.RequiresConversion(source, target));
    }

    [Theory]
    [InlineData(PortTypeId.Float)]
    [InlineData(PortTypeId.Int)]
    [InlineData(PortTypeId.Bool)]
    public void RequiresConversion_SameType_ReturnsFalse(PortTypeId type)
    {
        Assert.False(PortTypeCompatibility.RequiresConversion(type, type));
    }

    [Theory]
    [InlineData(PortTypeId.Float)]
    [InlineData(PortTypeId.Int)]
    [InlineData(PortTypeId.Bool)]
    public void RequiresConversion_ToAny_ReturnsFalse(PortTypeId source)
    {
        // Any is a universal acceptor - no conversion needed
        Assert.False(PortTypeCompatibility.RequiresConversion(source, PortTypeId.Any));
    }

    [Fact]
    public void RequiresConversion_IncompatibleTypes_ReturnsFalse()
    {
        // Incompatible types can't connect, so no conversion
        Assert.False(PortTypeCompatibility.RequiresConversion(PortTypeId.Bool, PortTypeId.Float));
    }

    #endregion
}
