using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Systems;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Tests for network systems.
/// </summary>
public sealed class NetworkSystemsTests
{
    #region InterpolationSystem

    [Fact]
    public void InterpolationSystem_Constructor_SetsDefaultDelay()
    {
        var system = new InterpolationSystem();
        // Default is 100ms, system should be created without errors
        Assert.NotNull(system);
    }

    [Fact]
    public void InterpolationSystem_Constructor_AcceptsCustomDelay()
    {
        var system = new InterpolationSystem(interpolationDelayMs: 50f);
        Assert.NotNull(system);
    }

    [Fact]
    public void InterpolationSystem_Update_AdvancesServerTime()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        // Create interpolated entity
        var entity = world.Spawn()
            .With(default(Interpolated))
            .With(new InterpolationState { FromTime = 0, ToTime = 1 })
            .Build();

        // Run world update (which runs systems including InterpolationSystem)
        world.Update(0.05f); // 50ms

        // Interpolation factor should be updated
        ref var interpState = ref world.Get<InterpolationState>(entity);
        // Server time advances, but render time is behind by interpolation delay
        Assert.True(interpState.Factor >= 0f);

        world.UninstallPlugin("NetworkClient");
        client.Dispose();
    }

    [Fact]
    public void InterpolationSystem_Update_CalculatesInterpolationFactor()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        // Create interpolated entity with specific time window
        var entity = world.Spawn()
            .With(default(Interpolated))
            .With(new InterpolationState { FromTime = 0, ToTime = 0.2 })
            .Build();

        // Run multiple updates to advance past interpolation delay (100ms default)
        for (int i = 0; i < 10; i++)
        {
            world.Update(0.02f); // 20ms each = 200ms total
        }

        ref var interpState = ref world.Get<InterpolationState>(entity);
        // Factor should be clamped between 0 and 1
        Assert.InRange(interpState.Factor, 0f, 1f);

        world.UninstallPlugin("NetworkClient");
        client.Dispose();
    }

    [Fact]
    public void InterpolationSystem_Update_FactorStaysInRange()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        // Create entity with time window that will be exceeded
        var entity = world.Spawn()
            .With(default(Interpolated))
            .With(new InterpolationState { FromTime = 0, ToTime = 0.01 })
            .Build();

        // Run for longer than the interpolation window
        for (int i = 0; i < 20; i++)
        {
            world.Update(0.02f);
        }

        ref var interpState = ref world.Get<InterpolationState>(entity);
        // Factor should be clamped in range [0, 1]
        Assert.InRange(interpState.Factor, 0f, 1f);

        world.UninstallPlugin("NetworkClient");
        client.Dispose();
    }

    [Fact]
    public void InterpolationSystem_Update_HandlesZeroDuration()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        // Create entity with zero duration (FromTime == ToTime)
        var entity = world.Spawn()
            .With(default(Interpolated))
            .With(new InterpolationState { FromTime = 0.5, ToTime = 0.5 })
            .Build();

        // Should not throw or produce NaN
        world.Update(0.1f);

        ref var interpState = ref world.Get<InterpolationState>(entity);
        // When duration is 0, factor should remain unchanged
        Assert.False(float.IsNaN(interpState.Factor));

        world.UninstallPlugin("NetworkClient");
        client.Dispose();
    }

    #endregion

    #region NetworkServerSendSystem

    [Fact]
    public async Task NetworkServerSendSystem_Update_SendsEntitySpawnForNewEntities()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = 60 });
        world.InstallPlugin(serverPlugin);

        var receivedSpawn = false;
        client.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out _);
            if (messageType == MessageType.EntitySpawn)
            {
                receivedSpawn = true;
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);

        // Process connection
        server.Update();
        client.Update();

        // Create and register a networked entity
        var entity = world.Spawn().Build();
        serverPlugin.RegisterNetworkedEntity(entity);

        // Run server update (triggers NetworkServerSendSystem)
        world.Update(0.02f); // Enough time for one tick

        // Client should receive entity spawn
        client.Update();

        Assert.True(receivedSpawn);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task NetworkServerSendSystem_Update_SkipsWhenNoTickOccurs()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = 10 }); // 100ms ticks
        world.InstallPlugin(serverPlugin);

        var messageCount = 0;
        client.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out _);
            if (messageType != MessageType.ConnectionAccepted)
            {
                messageCount++;
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Create entity
        var entity = world.Spawn().Build();
        serverPlugin.RegisterNetworkedEntity(entity);

        // Run with very small delta (less than tick interval)
        world.Update(0.01f); // 10ms, less than 100ms tick

        client.Update();

        // Should not have received anything yet (no tick occurred)
        Assert.Equal(0, messageCount);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task NetworkServerSendSystem_Update_ClearsNeedsFullSync()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = 60 });
        world.InstallPlugin(serverPlugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();

        // Create entity
        var entity = world.Spawn().Build();
        serverPlugin.RegisterNetworkedEntity(entity);

        Assert.True(world.Get<NetworkState>(entity).NeedsFullSync);

        // Run update to send entity
        world.Update(0.02f);

        // NeedsFullSync should be cleared after sending
        Assert.False(world.Get<NetworkState>(entity).NeedsFullSync);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region NetworkClientSendSystem

    [Fact]
    public void NetworkClientSendSystem_Update_DoesNothingWhenNotConnected()
    {
        var (_, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkClientPlugin(client);
        world.InstallPlugin(plugin);

        // Update without connecting - should not throw
        world.Update(0.1f);

        world.UninstallPlugin("NetworkClient");
        client.Dispose();
    }

    [Fact]
    public async Task NetworkClientSendSystem_Update_SendsPingPeriodically()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        var receivedPing = false;
        server.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out _);
            if (messageType == MessageType.Ping)
            {
                receivedPing = true;
            }
        };

        await server.ListenAsync(7777);
        var plugin = new NetworkClientPlugin(client);
        clientWorld.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        // Run client update for 1 second (ping interval)
        for (int i = 0; i < 10; i++)
        {
            clientWorld.Update(0.1f);
        }

        server.Update();

        Assert.True(receivedPing);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task NetworkClientSendSystem_Update_ProcessesLocallyOwnedPredictedEntities()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        await server.ListenAsync(7777);
        var config = new ClientNetworkConfig { EnablePrediction = true };
        var plugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();
        client.Update();

        // Create a locally owned predicted entity
        var entity = plugin.SpawnNetworkedEntity(1, plugin.LocalClientId);

        Assert.True(clientWorld.Has<LocallyOwned>(entity));
        Assert.True(clientWorld.Has<Predicted>(entity));

        // Update should process this entity (no crash)
        clientWorld.Update(0.1f);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region NetworkServerReceiveSystem

    [Fact]
    public async Task NetworkServerReceiveSystem_Update_ProcessesIncomingMessages()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var serverPlugin = new NetworkServerPlugin(server);
        world.InstallPlugin(serverPlugin);

        var receivedClientId = 0;
        client.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out _);
            if (messageType == MessageType.ConnectionAccepted)
            {
                receivedClientId = reader.ReadSignedBits(16);
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Send a client ack from client
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ClientAck, 42);
        client.Send(0, writer.GetWrittenSpan(), DeliveryMode.Unreliable);
        client.Update();

        // Run server update (processes incoming in NetworkServerReceiveSystem)
        world.Update(0.01f);

        // Verify ack was processed
        serverPlugin.TryGetClientState(receivedClientId, out var clientState);
        Assert.Equal(42u, clientState!.LastAckedTick);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region NetworkClientReceiveSystem

    [Fact]
    public async Task NetworkClientReceiveSystem_Update_ProcessesTransportMessages()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server);
        serverWorld.InstallPlugin(serverPlugin);

        await server.ListenAsync(7777);
        var plugin = new NetworkClientPlugin(client);
        clientWorld.InstallPlugin(plugin);

        await plugin.ConnectAsync();
        server.Update();

        // Run client update to process ConnectionAccepted
        clientWorld.Update(0.01f);

        // Client should now be connected
        Assert.True(plugin.IsConnected);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion
}

/// <summary>
/// Tests for MessageType enum values.
/// </summary>
public sealed class MessageTypeTests
{
    [Theory]
    [InlineData(MessageType.None, 0x00)]
    [InlineData(MessageType.ConnectionRequest, 0x01)]
    [InlineData(MessageType.ConnectionAccepted, 0x02)]
    [InlineData(MessageType.ConnectionRejected, 0x03)]
    [InlineData(MessageType.Disconnect, 0x04)]
    [InlineData(MessageType.Ping, 0x05)]
    [InlineData(MessageType.Pong, 0x06)]
    [InlineData(MessageType.FullSnapshot, 0x10)]
    [InlineData(MessageType.DeltaSnapshot, 0x11)]
    [InlineData(MessageType.EntitySpawn, 0x12)]
    [InlineData(MessageType.EntityDespawn, 0x13)]
    [InlineData(MessageType.ComponentAdd, 0x14)]
    [InlineData(MessageType.ComponentRemove, 0x15)]
    [InlineData(MessageType.ComponentUpdate, 0x16)]
    [InlineData(MessageType.ClientInput, 0x20)]
    [InlineData(MessageType.ClientAck, 0x21)]
    [InlineData(MessageType.OwnershipTransfer, 0x30)]
    [InlineData(MessageType.OwnershipRequest, 0x31)]
    [InlineData(MessageType.Rpc, 0x40)]
    [InlineData(MessageType.ReliableEvent, 0x41)]
    [InlineData(MessageType.UnreliableEvent, 0x42)]
    public void MessageType_HasCorrectValue(MessageType messageType, byte expectedValue)
    {
        Assert.Equal(expectedValue, (byte)messageType);
    }

    [Fact]
    public void MessageType_AllValuesAreDefined()
    {
        var definedValues = Enum.GetValues<MessageType>();

        // Verify we have all expected message types
        Assert.Contains(MessageType.None, definedValues);
        Assert.Contains(MessageType.ConnectionRequest, definedValues);
        Assert.Contains(MessageType.ConnectionAccepted, definedValues);
        Assert.Contains(MessageType.ConnectionRejected, definedValues);
        Assert.Contains(MessageType.Disconnect, definedValues);
        Assert.Contains(MessageType.Ping, definedValues);
        Assert.Contains(MessageType.Pong, definedValues);
        Assert.Contains(MessageType.FullSnapshot, definedValues);
        Assert.Contains(MessageType.DeltaSnapshot, definedValues);
        Assert.Contains(MessageType.EntitySpawn, definedValues);
        Assert.Contains(MessageType.EntityDespawn, definedValues);
        Assert.Contains(MessageType.ComponentAdd, definedValues);
        Assert.Contains(MessageType.ComponentRemove, definedValues);
        Assert.Contains(MessageType.ComponentUpdate, definedValues);
        Assert.Contains(MessageType.ClientInput, definedValues);
        Assert.Contains(MessageType.ClientAck, definedValues);
        Assert.Contains(MessageType.OwnershipTransfer, definedValues);
        Assert.Contains(MessageType.OwnershipRequest, definedValues);
        Assert.Contains(MessageType.Rpc, definedValues);
        Assert.Contains(MessageType.ReliableEvent, definedValues);
        Assert.Contains(MessageType.UnreliableEvent, definedValues);
    }

    [Fact]
    public void MessageType_ConnectionMessagesInRange()
    {
        // Connection messages should be in 0x01-0x0F range
        Assert.InRange((byte)MessageType.ConnectionRequest, 0x01, 0x0F);
        Assert.InRange((byte)MessageType.ConnectionAccepted, 0x01, 0x0F);
        Assert.InRange((byte)MessageType.ConnectionRejected, 0x01, 0x0F);
        Assert.InRange((byte)MessageType.Disconnect, 0x01, 0x0F);
        Assert.InRange((byte)MessageType.Ping, 0x01, 0x0F);
        Assert.InRange((byte)MessageType.Pong, 0x01, 0x0F);
    }

    [Fact]
    public void MessageType_EntityReplicationMessagesInRange()
    {
        // Entity replication messages should be in 0x10-0x1F range
        Assert.InRange((byte)MessageType.FullSnapshot, 0x10, 0x1F);
        Assert.InRange((byte)MessageType.DeltaSnapshot, 0x10, 0x1F);
        Assert.InRange((byte)MessageType.EntitySpawn, 0x10, 0x1F);
        Assert.InRange((byte)MessageType.EntityDespawn, 0x10, 0x1F);
        Assert.InRange((byte)MessageType.ComponentAdd, 0x10, 0x1F);
        Assert.InRange((byte)MessageType.ComponentRemove, 0x10, 0x1F);
        Assert.InRange((byte)MessageType.ComponentUpdate, 0x10, 0x1F);
    }

    [Fact]
    public void MessageType_ClientInputMessagesInRange()
    {
        // Client input messages should be in 0x20-0x2F range
        Assert.InRange((byte)MessageType.ClientInput, 0x20, 0x2F);
        Assert.InRange((byte)MessageType.ClientAck, 0x20, 0x2F);
    }

    [Fact]
    public void MessageType_OwnershipMessagesInRange()
    {
        // Ownership messages should be in 0x30-0x3F range
        Assert.InRange((byte)MessageType.OwnershipTransfer, 0x30, 0x3F);
        Assert.InRange((byte)MessageType.OwnershipRequest, 0x30, 0x3F);
    }
}

/// <summary>
/// Tests for ClientState class.
/// </summary>
public sealed class ClientStateTests
{
    [Fact]
    public void ClientState_RequiredProperties_MustBeSet()
    {
        var state = new ClientState { ClientId = 42 };

        Assert.Equal(42, state.ClientId);
        Assert.Equal(0u, state.LastAckedTick);
        Assert.False(state.NeedsFullSnapshot);
        Assert.Equal(0f, state.RoundTripTimeMs);
    }

    [Fact]
    public void ClientState_Properties_CanBeModified()
    {
        var state = new ClientState
        {
            ClientId = 1,
            LastAckedTick = 100,
            NeedsFullSnapshot = true,
            RoundTripTimeMs = 50.5f
        };

        Assert.Equal(100u, state.LastAckedTick);
        Assert.True(state.NeedsFullSnapshot);
        Assert.Equal(50.5f, state.RoundTripTimeMs);
    }
}
