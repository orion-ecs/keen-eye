using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Tests for <see cref="NetworkClientPlugin"/>.
/// </summary>
public sealed class NetworkClientPluginTests
{
    #region Constructor and Properties

    [Fact]
    public void Constructor_WithDefaultConfig_UsesDefaults()
    {
        var (_, client) = LocalTransport.CreatePair();
        var plugin = new NetworkClientPlugin(client);

        Assert.Equal("NetworkClient", plugin.Name);
        Assert.False(plugin.IsServer);
        Assert.True(plugin.IsClient);
        Assert.Same(client, plugin.Transport);
        Assert.Equal(0u, plugin.CurrentTick);
        Assert.Equal(0, plugin.LocalClientId);
        Assert.False(plugin.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void Constructor_WithCustomConfig_UsesConfig()
    {
        var (_, client) = LocalTransport.CreatePair();
        var config = new ClientNetworkConfig
        {
            ServerAddress = "192.168.1.1",
            ServerPort = 9999,
            EnablePrediction = true
        };
        var plugin = new NetworkClientPlugin(client, config);

        Assert.Equal("192.168.1.1", plugin.Config.ServerAddress);
        Assert.Equal(9999, plugin.Config.ServerPort);
        Assert.True(plugin.Config.EnablePrediction);

        client.Dispose();
    }

    #endregion

    #region Install and Uninstall

    [Fact]
    public void Install_RegistersSystemsAndSubscribesEvents()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);

        world.InstallPlugin(plugin);

        Assert.True(world.HasPlugin("NetworkClient"));

        client.Dispose();
    }

    [Fact]
    public void Uninstall_CleansUpResources()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        world.UninstallPlugin("NetworkClient");

        Assert.False(world.HasPlugin("NetworkClient"));

        client.Dispose();
    }

    #endregion

    #region Connect and Disconnect

    [Fact]
    public async Task ConnectAsync_ConnectsToServer()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();

        await server.ListenAsync(7777);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();

        Assert.Equal(ConnectionState.Connected, client.State);

        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Connected_Event_RaisedOnConnectionAccepted()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        using var serverWorld = new World();

        await server.ListenAsync(7777);
        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        var connectedRaised = false;
        plugin.Connected += () => connectedRaised = true;

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        Assert.True(connectedRaised);
        Assert.True(plugin.IsConnected);
        Assert.True(plugin.LocalClientId > 0);

        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Disconnect_DisconnectsFromServer()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();

        await server.ListenAsync(7777);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        plugin.Disconnect();

        Assert.Equal(ConnectionState.Disconnected, client.State);

        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Disconnected_Event_RaisedOnDisconnect()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        using var serverWorld = new World();

        await server.ListenAsync(7777);
        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        var disconnectedRaised = false;
        plugin.Disconnected += () => disconnectedRaised = true;

        plugin.Disconnect();

        Assert.True(disconnectedRaised);
        Assert.False(plugin.IsConnected);
        Assert.Equal(0, plugin.LocalClientId);

        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region SpawnNetworkedEntity

    [Fact]
    public void SpawnNetworkedEntity_WhenNotInstalled_ThrowsInvalidOperationException()
    {
        var (_, client) = LocalTransport.CreatePair();
        var plugin = new NetworkClientPlugin(client);

        Assert.Throws<InvalidOperationException>(() =>
            plugin.SpawnNetworkedEntity(1, 0));

        client.Dispose();
    }

    [Fact]
    public void SpawnNetworkedEntity_CreatesEntityWithNetworkComponents()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        var entity = plugin.SpawnNetworkedEntity(networkId: 42, ownerId: 5);

        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<NetworkId>(entity));
        Assert.True(world.Has<NetworkOwner>(entity));
        Assert.True(world.Has<NetworkState>(entity));
        Assert.True(world.Has<Networked>(entity));

        Assert.Equal(42u, world.Get<NetworkId>(entity).Value);
        Assert.Equal(5, world.Get<NetworkOwner>(entity).ClientId);

        client.Dispose();
    }

    [Fact]
    public async Task SpawnNetworkedEntity_WithLocalOwner_AddsLocallyOwnedAndPredicted()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        using var serverWorld = new World();

        await server.ListenAsync(7777);
        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var config = new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            EnablePrediction = true
        };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        // Spawn entity owned by this client
        var entity = plugin.SpawnNetworkedEntity(networkId: 1, ownerId: plugin.LocalClientId);

        Assert.True(world.Has<LocallyOwned>(entity));
        Assert.True(world.Has<Predicted>(entity));
        Assert.True(world.Has<PredictionState>(entity));
        Assert.False(world.Has<RemotelyOwned>(entity));
        Assert.False(world.Has<Interpolated>(entity));

        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task SpawnNetworkedEntity_WithRemoteOwner_AddsRemotelyOwnedAndInterpolated()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        using var serverWorld = new World();

        await server.ListenAsync(7777);
        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        // Spawn entity owned by a different client
        var entity = plugin.SpawnNetworkedEntity(networkId: 1, ownerId: 999);

        Assert.True(world.Has<RemotelyOwned>(entity));
        Assert.True(world.Has<Interpolated>(entity));
        Assert.True(world.Has<InterpolationState>(entity));
        Assert.False(world.Has<LocallyOwned>(entity));
        Assert.False(world.Has<Predicted>(entity));

        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region DespawnNetworkedEntity

    [Fact]
    public void DespawnNetworkedEntity_RemovesEntity()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        var entity = plugin.SpawnNetworkedEntity(networkId: 100, ownerId: 0);
        Assert.True(world.IsAlive(entity));

        plugin.DespawnNetworkedEntity(100);

        Assert.False(world.IsAlive(entity));

        client.Dispose();
    }

    [Fact]
    public void DespawnNetworkedEntity_WithUnknownId_DoesNothing()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        // Should not throw
        plugin.DespawnNetworkedEntity(999);

        client.Dispose();
    }

    [Fact]
    public void DespawnNetworkedEntity_WhenNotInstalled_DoesNothing()
    {
        var (_, client) = LocalTransport.CreatePair();
        var plugin = new NetworkClientPlugin(client);

        // Should not throw (context is null)
        plugin.DespawnNetworkedEntity(1);

        client.Dispose();
    }

    #endregion

    #region UpdateTick

    [Fact]
    public void UpdateTick_UpdatesCurrentAndLastReceivedTick()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        plugin.UpdateTick(42);

        Assert.Equal(42u, plugin.CurrentTick);
        Assert.Equal(42u, plugin.LastReceivedTick);

        client.Dispose();
    }

    #endregion

    #region SendToServer

    [Fact]
    public async Task SendToServer_SendsDataToServer()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();

        await server.ListenAsync(7777);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        byte[]? receivedData = null;
        server.DataReceived += (connectionId, data) =>
        {
            receivedData = data.ToArray();
        };

        await plugin.ConnectAsync();

        var testData = new byte[] { 1, 2, 3, 4, 5 };
        plugin.SendToServer(testData, DeliveryMode.ReliableOrdered);
        client.Update();
        server.Update();

        Assert.Equal(testData, receivedData);

        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region AcknowledgeTick

    [Fact]
    public async Task AcknowledgeTick_SendsClientAckMessage()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();

        await server.ListenAsync(7777);

        var config = new ClientNetworkConfig { ServerAddress = "localhost", ServerPort = 7777 };
        var plugin = new NetworkClientPlugin(client, config);
        world.InstallPlugin(plugin);

        MessageType? receivedMessageType = null;
        uint receivedTick = 0;

        server.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out var tick);
            receivedMessageType = messageType;
            receivedTick = tick;
        };

        await plugin.ConnectAsync();

        plugin.AcknowledgeTick(123);
        client.Update();
        server.Update();

        Assert.Equal(MessageType.ClientAck, receivedMessageType);
        Assert.Equal(123u, receivedTick);

        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Message Handling

    [Fact]
    public async Task ReceiveEntitySpawn_SpawnsLocalEntity()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        using var serverWorld = new World();

        await server.ListenAsync(7777);
        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update(); // Process ConnectionAccepted

        // Server sends EntitySpawn (with 0 components)
        Span<byte> buffer = stackalloc byte[32];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.EntitySpawn, 1);
        writer.WriteEntitySpawn(networkId: 55, ownerId: 0);
        writer.WriteComponentCount(0); // No components attached
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        // Verify entity was spawned
        var entities = world.Query<NetworkId>().ToList();
        Assert.Single(entities);
        Assert.Equal(55u, world.Get<NetworkId>(entities[0]).Value);

        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ReceiveEntityDespawn_DespawnsLocalEntity()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        using var serverWorld = new World();

        await server.ListenAsync(7777);
        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        // Spawn entity locally
        var entity = plugin.SpawnNetworkedEntity(networkId: 77, ownerId: 0);
        Assert.True(world.IsAlive(entity));

        // Server sends EntityDespawn
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.EntityDespawn, 2);
        writer.WriteEntityDespawn(77);
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        // Entity should be despawned
        Assert.False(world.IsAlive(entity));

        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ReceiveConnectionRejected_RaisesEvent()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();

        await server.ListenAsync(7777);

        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        string? rejectionReason = null;
        plugin.ConnectionRejected += reason => rejectionReason = reason;

        await plugin.ConnectAsync();

        // Manually send rejection message with reason code
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ConnectionRejected, 0);
        writer.WriteByte(1); // Server is full reason code
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        Assert.NotNull(rejectionReason);
        Assert.Contains("full", rejectionReason, StringComparison.OrdinalIgnoreCase);

        client.Dispose();
        server.Dispose();
    }

    #endregion
}
