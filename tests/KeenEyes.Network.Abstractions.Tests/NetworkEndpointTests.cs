using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for NetworkEndpoint struct.
/// </summary>
public class NetworkEndpointTests
{
    #region Factory Method Tests

    [Fact]
    public void Localhost_CreatesCorrectEndpoint()
    {
        var endpoint = NetworkEndpoint.Localhost(8080);

        Assert.Equal("127.0.0.1", endpoint.Address);
        Assert.Equal(8080, endpoint.Port);
    }

    [Fact]
    public void Any_CreatesCorrectEndpoint()
    {
        var endpoint = NetworkEndpoint.Any(7777);

        Assert.Equal("0.0.0.0", endpoint.Address);
        Assert.Equal(7777, endpoint.Port);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        var endpoint = new NetworkEndpoint { Address = "192.168.1.1", Port = 8080 };

        Assert.Equal("192.168.1.1:8080", endpoint.ToString());
    }

    [Fact]
    public void ToString_WithLocalhost_ReturnsCorrectFormat()
    {
        var endpoint = NetworkEndpoint.Localhost(7777);

        Assert.Equal("127.0.0.1:7777", endpoint.ToString());
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_ValidEndpoint_ReturnsCorrectEndpoint()
    {
        var endpoint = NetworkEndpoint.Parse("192.168.1.100:9000");

        Assert.Equal("192.168.1.100", endpoint.Address);
        Assert.Equal(9000, endpoint.Port);
    }

    [Fact]
    public void Parse_Localhost_ReturnsCorrectEndpoint()
    {
        var endpoint = NetworkEndpoint.Parse("127.0.0.1:8080");

        Assert.Equal("127.0.0.1", endpoint.Address);
        Assert.Equal(8080, endpoint.Port);
    }

    [Fact]
    public void Parse_PortZero_ReturnsCorrectEndpoint()
    {
        var endpoint = NetworkEndpoint.Parse("localhost:0");

        Assert.Equal("localhost", endpoint.Address);
        Assert.Equal(0, endpoint.Port);
    }

    [Fact]
    public void Parse_MaxPort_ReturnsCorrectEndpoint()
    {
        var endpoint = NetworkEndpoint.Parse("example.com:65535");

        Assert.Equal("example.com", endpoint.Address);
        Assert.Equal(65535, endpoint.Port);
    }

    [Fact]
    public void Parse_NoColon_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => NetworkEndpoint.Parse("invalid"));

        Assert.Contains("Invalid endpoint format", ex.Message);
        Assert.Contains("Expected 'address:port'", ex.Message);
    }

    [Fact]
    public void Parse_InvalidPort_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => NetworkEndpoint.Parse("localhost:abc"));

        Assert.Contains("Invalid port", ex.Message);
    }

    [Fact]
    public void Parse_PortTooHigh_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => NetworkEndpoint.Parse("localhost:65536"));

        Assert.Contains("Invalid port", ex.Message);
    }

    [Fact]
    public void Parse_NegativePort_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => NetworkEndpoint.Parse("localhost:-1"));

        Assert.Contains("Invalid port", ex.Message);
    }

    [Fact]
    public void Parse_IPv6WithPort_ReturnsCorrectEndpoint()
    {
        // IPv6 addresses use brackets, so colon search finds the last one
        var endpoint = NetworkEndpoint.Parse("[::1]:8080");

        Assert.Equal("[::1]", endpoint.Address);
        Assert.Equal(8080, endpoint.Port);
    }

    #endregion

    #region TryParse Tests

    [Fact]
    public void TryParse_ValidEndpoint_ReturnsTrue()
    {
        var result = NetworkEndpoint.TryParse("192.168.1.1:8080", out var endpoint);

        Assert.True(result);
        Assert.Equal("192.168.1.1", endpoint.Address);
        Assert.Equal(8080, endpoint.Port);
    }

    [Fact]
    public void TryParse_InvalidEndpoint_ReturnsFalse()
    {
        var result = NetworkEndpoint.TryParse("invalid", out var endpoint);

        Assert.False(result);
        Assert.Equal(default, endpoint);
    }

    [Fact]
    public void TryParse_InvalidPort_ReturnsFalse()
    {
        var result = NetworkEndpoint.TryParse("localhost:abc", out var endpoint);

        Assert.False(result);
        Assert.Equal(default, endpoint);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var endpoint1 = new NetworkEndpoint { Address = "127.0.0.1", Port = 8080 };
        var endpoint2 = new NetworkEndpoint { Address = "127.0.0.1", Port = 8080 };

        Assert.Equal(endpoint1, endpoint2);
        Assert.True(endpoint1 == endpoint2);
    }

    [Fact]
    public void Equality_DifferentAddress_AreNotEqual()
    {
        var endpoint1 = new NetworkEndpoint { Address = "127.0.0.1", Port = 8080 };
        var endpoint2 = new NetworkEndpoint { Address = "192.168.1.1", Port = 8080 };

        Assert.NotEqual(endpoint1, endpoint2);
        Assert.True(endpoint1 != endpoint2);
    }

    [Fact]
    public void Equality_DifferentPort_AreNotEqual()
    {
        var endpoint1 = new NetworkEndpoint { Address = "127.0.0.1", Port = 8080 };
        var endpoint2 = new NetworkEndpoint { Address = "127.0.0.1", Port = 9090 };

        Assert.NotEqual(endpoint1, endpoint2);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHashCode()
    {
        var endpoint1 = new NetworkEndpoint { Address = "127.0.0.1", Port = 8080 };
        var endpoint2 = new NetworkEndpoint { Address = "127.0.0.1", Port = 8080 };

        Assert.Equal(endpoint1.GetHashCode(), endpoint2.GetHashCode());
    }

    #endregion
}
