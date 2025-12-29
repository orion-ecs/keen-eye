using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="DeliveryMode"/> enum.
/// </summary>
public class DeliveryModeTests
{
    [Fact]
    public void Unreliable_HasExpectedValue()
    {
        Assert.Equal(0, (int)DeliveryMode.Unreliable);
    }

    [Fact]
    public void UnreliableSequenced_HasExpectedValue()
    {
        Assert.Equal(1, (int)DeliveryMode.UnreliableSequenced);
    }

    [Fact]
    public void ReliableUnordered_HasExpectedValue()
    {
        Assert.Equal(2, (int)DeliveryMode.ReliableUnordered);
    }

    [Fact]
    public void ReliableOrdered_HasExpectedValue()
    {
        Assert.Equal(3, (int)DeliveryMode.ReliableOrdered);
    }
}
