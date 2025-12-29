using KeenEyes.Network.Components;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="NetworkId"/> component.
/// </summary>
public class NetworkIdTests
{
    [Fact]
    public void Invalid_HasZeroValue()
    {
        Assert.Equal(0u, NetworkId.Invalid.Value);
    }

    [Fact]
    public void IsValid_WithZeroValue_ReturnsFalse()
    {
        var id = new NetworkId { Value = 0 };

        Assert.False(id.IsValid);
    }

    [Fact]
    public void IsValid_WithNonZeroValue_ReturnsTrue()
    {
        var id = new NetworkId { Value = 1 };

        Assert.True(id.IsValid);
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var id1 = new NetworkId { Value = 42 };
        var id2 = new NetworkId { Value = 42 };

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var id1 = new NetworkId { Value = 42 };
        var id2 = new NetworkId { Value = 43 };

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        var id1 = new NetworkId { Value = 42 };
        var id2 = new NetworkId { Value = 42 };

        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}
