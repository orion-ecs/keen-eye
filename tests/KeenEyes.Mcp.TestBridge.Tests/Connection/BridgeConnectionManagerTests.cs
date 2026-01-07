using KeenEyes.Mcp.TestBridge.Connection;

namespace KeenEyes.Mcp.TestBridge.Tests.Connection;

/// <summary>
/// Tests for BridgeConnectionManager.
/// </summary>
public sealed class BridgeConnectionManagerTests : IAsyncDisposable
{
    private BridgeConnectionManager? manager;

    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultValues_SetsDefaults()
    {
        manager = new BridgeConnectionManager();

        manager.HeartbeatInterval.ShouldBe(TimeSpan.FromSeconds(5));
        manager.PingTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        manager.MaxPingFailures.ShouldBe(3);
        manager.ConnectionTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        manager.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_CustomValues_SetsCustomValues()
    {
        manager = new BridgeConnectionManager
        {
            HeartbeatInterval = TimeSpan.FromSeconds(10),
            PingTimeout = TimeSpan.FromSeconds(30),
            MaxPingFailures = 5,
            ConnectionTimeout = TimeSpan.FromSeconds(60)
        };

        manager.HeartbeatInterval.ShouldBe(TimeSpan.FromSeconds(10));
        manager.PingTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        manager.MaxPingFailures.ShouldBe(5);
        manager.ConnectionTimeout.ShouldBe(TimeSpan.FromSeconds(60));
    }

    #endregion

    #region Connection State Tests

    [Fact]
    public void IsConnected_BeforeConnect_ReturnsFalse()
    {
        manager = new BridgeConnectionManager();

        manager.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void GetBridge_WhenNotConnected_ThrowsInvalidOperationException()
    {
        manager = new BridgeConnectionManager();

        var exception = Should.Throw<InvalidOperationException>(() => manager.GetBridge());
        exception.Message.ShouldContain("Not connected");
    }

    [Fact]
    public void TryGetBridge_WhenNotConnected_ReturnsFalse()
    {
        manager = new BridgeConnectionManager();

        var result = manager.TryGetBridge(out var bridge);

        result.ShouldBeFalse();
        bridge.ShouldBeNull();
    }

    #endregion

    #region Status Tests

    [Fact]
    public void GetStatus_WhenNotConnected_ReturnsDisconnectedStatus()
    {
        manager = new BridgeConnectionManager();

        var status = manager.GetStatus();

        status.IsConnected.ShouldBeFalse();
        status.LastPingMs.ShouldBeNull();
        status.LastPingTime.ShouldBeNull();
        status.PipeName.ShouldBeNull();
        status.ConnectionUptime.ShouldBeNull();
        status.ConsecutivePingFailures.ShouldBe(0);
    }

    [Fact]
    public void ConnectionUptime_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.ConnectionUptime.ShouldBeNull();
    }

    [Fact]
    public void TransportMode_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.TransportMode.ShouldBeNull();
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void PipeName_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.PipeName.ShouldBeNull();
    }

    [Fact]
    public void Host_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.Host.ShouldBeNull();
    }

    [Fact]
    public void Port_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.Port.ShouldBeNull();
    }

    [Fact]
    public void LastPingLatencyMs_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.LastPingLatencyMs.ShouldBeNull();
    }

    [Fact]
    public void LastPingTime_WhenNotConnected_ReturnsNull()
    {
        manager = new BridgeConnectionManager();

        manager.LastPingTime.ShouldBeNull();
    }

    [Fact]
    public void ConsecutivePingFailures_WhenNotConnected_ReturnsZero()
    {
        manager = new BridgeConnectionManager();

        manager.ConsecutivePingFailures.ShouldBe(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_WhenNotConnected_CompletesSuccessfully()
    {
        manager = new BridgeConnectionManager();

        await Should.NotThrowAsync(async () => await manager.DisposeAsync());
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        manager = new BridgeConnectionManager();

        await manager.DisposeAsync();
        await Should.NotThrowAsync(async () => await manager.DisposeAsync());
    }

    [Fact]
    public async Task GetBridge_AfterDispose_ThrowsObjectDisposedException()
    {
        manager = new BridgeConnectionManager();
        await manager.DisposeAsync();

        Should.Throw<ObjectDisposedException>(() => manager.GetBridge());
    }

    [Fact]
    public async Task TryGetBridge_AfterDispose_ReturnsFalse()
    {
        manager = new BridgeConnectionManager();
        await manager.DisposeAsync();

        var result = manager.TryGetBridge(out var bridge);

        result.ShouldBeFalse();
        bridge.ShouldBeNull();
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (manager != null)
        {
            await manager.DisposeAsync();
        }
    }
}
