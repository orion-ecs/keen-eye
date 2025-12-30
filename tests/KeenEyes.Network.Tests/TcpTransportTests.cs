#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

using KeenEyes.Network.Transport;
using KeenEyes.Network.Transport.Tcp;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for the <see cref="TcpTransport"/> class.
/// </summary>
public class TcpTransportTests
{
    [Fact]
    public async Task ListenAsync_SetsStateToConnected()
    {
        using var server = new TcpTransport();

        await server.ListenAsync(0);

        Assert.Equal(ConnectionState.Connected, server.State);
        Assert.True(server.IsServer);
        Assert.True(server.LocalPort > 0);
    }

    [Fact]
    public async Task ConnectAsync_WhenServerListening_SetsStateToConnected()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client.ConnectAsync("127.0.0.1", port);

        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.True(client.IsClient);
    }

    [Fact]
    public async Task Send_DeliversMessageFromClientToServer()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client.ConnectAsync("127.0.0.1", port);

        // Wait for server to accept connection
        await Task.Delay(50);
        server.Update();

        byte[]? receivedData = null;
        int? receivedFromId = null;
        server.DataReceived += (connId, data) =>
        {
            receivedData = data.ToArray();
            receivedFromId = connId;
        };

        byte[] testData = [1, 2, 3, 4, 5];
        client.Send(0, testData, DeliveryMode.ReliableOrdered);

        // Wait for data to be transmitted
        await Task.Delay(100);
        server.Update();

        Assert.NotNull(receivedData);
        Assert.Equal(testData, receivedData);
        Assert.NotNull(receivedFromId);
    }

    [Fact]
    public async Task Send_DeliversMessageFromServerToClient()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        int? connectedClientId = null;
        server.ClientConnected += id => connectedClientId = id;

        await client.ConnectAsync("127.0.0.1", port);

        // Wait for server to accept connection
        await Task.Delay(50);
        server.Update();

        Assert.NotNull(connectedClientId);

        byte[]? receivedData = null;
        client.DataReceived += (_, data) =>
        {
            receivedData = data.ToArray();
        };

        byte[] testData = [10, 20, 30, 40];
        server.Send(connectedClientId.Value, testData, DeliveryMode.ReliableOrdered);

        // Wait for data to be transmitted
        await Task.Delay(100);
        client.Update();

        Assert.NotNull(receivedData);
        Assert.Equal(testData, receivedData);
    }

    [Fact]
    public async Task ClientConnected_RaisedOnServerWhenClientConnects()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        int? connectedClientId = null;
        server.ClientConnected += id => connectedClientId = id;

        await client.ConnectAsync("127.0.0.1", port);

        // Wait for server to accept connection
        await Task.Delay(100);
        server.Update();

        Assert.NotNull(connectedClientId);
        Assert.True(connectedClientId > 0);
    }

    [Fact]
    public async Task Disconnect_SetsStateToDisconnected()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client.ConnectAsync("127.0.0.1", port);

        client.Disconnect();

        Assert.Equal(ConnectionState.Disconnected, client.State);
    }

    [Fact]
    public async Task Disconnect_SpecificClient_RaisesClientDisconnected()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        int? connectedClientId = null;
        server.ClientConnected += id => connectedClientId = id;

        int? disconnectedClientId = null;
        server.ClientDisconnected += id => disconnectedClientId = id;

        await client.ConnectAsync("127.0.0.1", port);
        await Task.Delay(50);
        server.Update();

        Assert.NotNull(connectedClientId);

        server.Disconnect(connectedClientId.Value);

        // Wait for disconnect to process
        await Task.Delay(50);

        Assert.NotNull(disconnectedClientId);
        Assert.Equal(connectedClientId, disconnectedClientId);
    }

    [Fact]
    public void Dispose_DisconnectsTransport()
    {
        var server = new TcpTransport();

        server.Dispose();

        Assert.Equal(ConnectionState.Disconnected, server.State);
    }

    [Fact]
    public async Task SendToAll_SendsToAllConnectedClients()
    {
        using var server = new TcpTransport();
        using var client1 = new TcpTransport();
        using var client2 = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client1.ConnectAsync("127.0.0.1", port);
        await client2.ConnectAsync("127.0.0.1", port);

        await Task.Delay(100);
        server.Update();

        byte[]? received1 = null;
        byte[]? received2 = null;
        client1.DataReceived += (_, data) => received1 = data.ToArray();
        client2.DataReceived += (_, data) => received2 = data.ToArray();

        byte[] testData = [99, 88, 77];
        server.SendToAll(testData, DeliveryMode.ReliableOrdered);

        await Task.Delay(100);
        client1.Update();
        client2.Update();

        Assert.NotNull(received1);
        Assert.Equal(testData, received1);
        Assert.NotNull(received2);
        Assert.Equal(testData, received2);
    }

    [Fact]
    public async Task SendToAllExcept_ExcludesSpecifiedClient()
    {
        using var server = new TcpTransport();
        using var client1 = new TcpTransport();
        using var client2 = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        var connectedClients = new List<int>();
        server.ClientConnected += id => connectedClients.Add(id);

        await client1.ConnectAsync("127.0.0.1", port);
        await Task.Delay(50);
        server.Update();

        await client2.ConnectAsync("127.0.0.1", port);
        await Task.Delay(50);
        server.Update();

        Assert.Equal(2, connectedClients.Count);

        byte[]? received1 = null;
        byte[]? received2 = null;
        client1.DataReceived += (_, data) => received1 = data.ToArray();
        client2.DataReceived += (_, data) => received2 = data.ToArray();

        byte[] testData = [11, 22, 33];
        server.SendToAllExcept(connectedClients[0], testData, DeliveryMode.ReliableOrdered);

        await Task.Delay(100);
        client1.Update();
        client2.Update();

        Assert.Null(received1); // First client was excluded
        Assert.NotNull(received2);
        Assert.Equal(testData, received2);
    }

    [Fact]
    public async Task GetStatistics_ReturnsValidStatistics()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        int? clientId = null;
        server.ClientConnected += id => clientId = id;

        await client.ConnectAsync("127.0.0.1", port);
        await Task.Delay(50);
        server.Update();

        byte[] testData = [1, 2, 3, 4];
        client.Send(0, testData, DeliveryMode.ReliableOrdered);
        await Task.Delay(50);

        var stats = client.GetStatistics(0);

        Assert.True(stats.BytesSent > 0);
        Assert.Equal(0, stats.PacketLossPercent); // TCP guarantees delivery
    }

    [Fact]
    public async Task GetRoundTripTime_ReturnsNegativeOne()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client.ConnectAsync("127.0.0.1", port);

        var rtt = client.GetRoundTripTime(0);

        Assert.Equal(-1, rtt); // TCP doesn't expose RTT directly
    }

    [Fact]
    public void State_DefaultsToDisconnected()
    {
        using var transport = new TcpTransport();

        Assert.Equal(ConnectionState.Disconnected, transport.State);
    }

    [Fact]
    public void IsServer_DefaultsToFalse()
    {
        using var transport = new TcpTransport();

        Assert.False(transport.IsServer);
    }

    [Fact]
    public void IsClient_DefaultsToFalse()
    {
        using var transport = new TcpTransport();

        Assert.False(transport.IsClient);
    }

    [Fact]
    public async Task StateChanged_FiredOnConnectionStateChange()
    {
        using var server = new TcpTransport();
        var stateChanges = new List<ConnectionState>();
        server.StateChanged += state => stateChanges.Add(state);

        await server.ListenAsync(0);

        Assert.Contains(ConnectionState.Connected, stateChanges);
    }

    [Fact]
    public async Task SendToAll_ThrowsOnClient()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client.ConnectAsync("127.0.0.1", port);

        byte[] testData = [1, 2, 3];
        Assert.Throws<InvalidOperationException>(() => client.SendToAll(testData, DeliveryMode.ReliableOrdered));
    }

    [Fact]
    public async Task ListenAsync_WhenAlreadyListening_Throws()
    {
        using var server = new TcpTransport();

        await server.ListenAsync(0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => server.ListenAsync(0));
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_Throws()
    {
        using var server = new TcpTransport();
        using var client = new TcpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;
        await client.ConnectAsync("127.0.0.1", port);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ConnectAsync("127.0.0.1", port));
    }

    [Fact]
    public void LocalPort_BeforeListen_ReturnsNegativeOne()
    {
        using var transport = new TcpTransport();

        Assert.Equal(-1, transport.LocalPort);
    }
}
