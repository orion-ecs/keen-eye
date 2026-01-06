using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for the <see cref="LocalTransport"/> class.
/// </summary>
public class LocalTransportTests
{
    [Fact]
    public void CreatePair_ReturnsTwoConnectedTransports()
    {
        var (server, client) = LocalTransport.CreatePair();

        Assert.NotNull(server);
        Assert.NotNull(client);
    }

    [Fact]
    public async Task ListenAsync_SetsStateToConnected()
    {
        var (server, _) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);

        Assert.Equal(ConnectionState.Connected, server.State);
        Assert.True(server.IsServer);
    }

    [Fact]
    public async Task ConnectAsync_SetsStateToConnected()
    {
        var (server, client) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);
        await client.ConnectAsync("localhost", 7777, ct);

        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.True(client.IsClient);
    }

    [Fact]
    public async Task Send_DeliversMessageToPeer()
    {
        var (server, client) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);
        await client.ConnectAsync("localhost", 7777, ct);

        byte[]? receivedData = null;
        server.DataReceived += (connId, data) =>
        {
            receivedData = data.ToArray();
        };

        byte[] testData = [1, 2, 3, 4];
        client.Send(0, testData, DeliveryMode.ReliableOrdered);
        server.Update();

        Assert.NotNull(receivedData);
        Assert.Equal(testData, receivedData);
    }

    [Fact]
    public async Task Disconnect_SetsStateToDisconnected()
    {
        var (server, client) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);
        await client.ConnectAsync("localhost", 7777, ct);

        client.Disconnect();

        Assert.Equal(ConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task ClientConnected_RaisedOnServerWhenClientConnects()
    {
        var (server, client) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);

        int? connectedClientId = null;
        server.ClientConnected += id => connectedClientId = id;

        await client.ConnectAsync("localhost", 7777, ct);

        Assert.NotNull(connectedClientId);
    }

    [Fact]
    public void SimulatedLatencyMs_DefaultsToZero()
    {
        var (server, _) = LocalTransport.CreatePair();

        Assert.Equal(0f, server.SimulatedLatencyMs);
    }

    [Fact]
    public void SimulatedPacketLossPercent_ClampsToValidRange()
    {
        var (server, _) = LocalTransport.CreatePair();

        server.SimulatedPacketLossPercent = 150;

        Assert.Equal(100f, server.SimulatedPacketLossPercent);
    }

    [Fact]
    public void GetRoundTripTime_ReturnsSimulatedLatencyTimes2()
    {
        var (server, _) = LocalTransport.CreatePair();
        server.SimulatedLatencyMs = 50f;

        var rtt = server.GetRoundTripTime(1);

        Assert.Equal(100f, rtt);
    }

    [Fact]
    public void Dispose_DisconnectsTransport()
    {
        var (server, _) = LocalTransport.CreatePair();

        server.Dispose();

        Assert.Equal(ConnectionState.Disconnected, server.State);
    }

    [Fact]
    public async Task SendToAll_SendsToAllConnectedClients()
    {
        var (server, client) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);
        await client.ConnectAsync("localhost", 7777, ct);

        byte[]? receivedData = null;
        client.DataReceived += (_, data) =>
        {
            receivedData = data.ToArray();
        };

        byte[] testData = [5, 6, 7, 8];
        server.SendToAll(testData, DeliveryMode.ReliableOrdered);
        client.Update();

        Assert.NotNull(receivedData);
        Assert.Equal(testData, receivedData);
    }

    [Fact]
    public async Task GetStatistics_ReturnsValidStatistics()
    {
        var (server, client) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        await server.ListenAsync(7777, ct);
        await client.ConnectAsync("localhost", 7777, ct);

        byte[] testData = [1, 2, 3, 4];
        client.Send(0, testData, DeliveryMode.Unreliable);

        var stats = client.GetStatistics(0);

        Assert.Equal(4, stats.BytesSent);
    }

    [Fact]
    public void State_DefaultsToDisconnected()
    {
        var (server, _) = LocalTransport.CreatePair();

        Assert.Equal(ConnectionState.Disconnected, server.State);
    }

    [Fact]
    public void IsServer_DefaultsToFalse()
    {
        var (server, _) = LocalTransport.CreatePair();

        Assert.False(server.IsServer);
    }

    [Fact]
    public async Task StateChanged_FiredOnConnectionStateChange()
    {
        var (server, _) = LocalTransport.CreatePair();
        var ct = TestContext.Current.CancellationToken;

        var stateChanges = new List<ConnectionState>();
        server.StateChanged += state => stateChanges.Add(state);

        await server.ListenAsync(7777, ct);

        Assert.Contains(ConnectionState.Connected, stateChanges);
    }

    [Fact]
    public void SimulatedLatencyMs_CannotBeNegative()
    {
        var (server, _) = LocalTransport.CreatePair();

        server.SimulatedLatencyMs = -50;

        Assert.Equal(0f, server.SimulatedLatencyMs);
    }

    [Fact]
    public void SimulatedPacketLossPercent_CannotBeNegative()
    {
        var (server, _) = LocalTransport.CreatePair();

        server.SimulatedPacketLossPercent = -10;

        Assert.Equal(0f, server.SimulatedPacketLossPercent);
    }
}
