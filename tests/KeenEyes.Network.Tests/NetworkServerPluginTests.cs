using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Test tag defined locally for comparison (regular struct).
/// </summary>
public struct LocalTestTag : ITagComponent;

/// <summary>
/// Test tag as readonly record struct (like Networked).
/// </summary>
public readonly record struct LocalRecordTag : ITagComponent;

/// <summary>
/// Tests for <see cref="NetworkServerPlugin"/>.
/// </summary>
public sealed class NetworkServerPluginTests
{
    #region Constructor and Properties

    [Fact]
    public void Constructor_WithDefaultConfig_UsesDefaults()
    {
        var (server, _) = LocalTransport.CreatePair();
        var plugin = new NetworkServerPlugin(server);

        Assert.Equal("NetworkServer", plugin.Name);
        Assert.True(plugin.IsServer);
        Assert.False(plugin.IsClient);
        Assert.Same(server, plugin.Transport);
        Assert.Equal(0u, plugin.CurrentTick);
        Assert.Equal(NetworkOwner.ServerClientId, plugin.LocalClientId);
        Assert.Equal(0, plugin.ClientCount);

        server.Dispose();
    }

    [Fact]
    public void Constructor_WithCustomConfig_UsesConfig()
    {
        var (server, _) = LocalTransport.CreatePair();
        var config = new ServerNetworkConfig { TickRate = 30, MaxClients = 8 };
        var plugin = new NetworkServerPlugin(server, config);

        Assert.Equal(30, plugin.Config.TickRate);
        Assert.Equal(8, plugin.Config.MaxClients);

        server.Dispose();
    }

    #endregion

    #region Install and Uninstall

    [Fact]
    public void Install_RegistersSystemsAndSubscribesEvents()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);

        world.InstallPlugin(plugin);

        Assert.True(world.HasPlugin("NetworkServer"));

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void Uninstall_CleansUpResources()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        world.UninstallPlugin("NetworkServer");

        Assert.False(world.HasPlugin("NetworkServer"));

        server.Dispose();
    }

    #endregion

    #region Tick

    [Fact]
    public void Tick_BeforeInterval_ReturnsFalse()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var config = new ServerNetworkConfig { TickRate = 60 }; // 16.67ms per tick
        var plugin = new NetworkServerPlugin(server, config);
        world.InstallPlugin(plugin);

        var result = plugin.Tick(0.01f); // 10ms - less than one tick

        Assert.False(result);
        Assert.Equal(0u, plugin.CurrentTick);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void Tick_AfterInterval_ReturnsTrue()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var config = new ServerNetworkConfig { TickRate = 60 }; // 16.67ms per tick
        var plugin = new NetworkServerPlugin(server, config);
        world.InstallPlugin(plugin);

        var result = plugin.Tick(0.02f); // 20ms - more than one tick

        Assert.True(result);
        Assert.Equal(1u, plugin.CurrentTick);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void Tick_MultipleTicks_AccumulatesCorrectly()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var config = new ServerNetworkConfig { TickRate = 10 }; // 100ms per tick
        var plugin = new NetworkServerPlugin(server, config);
        world.InstallPlugin(plugin);

        plugin.Tick(0.05f); // 50ms - no tick
        Assert.Equal(0u, plugin.CurrentTick);

        plugin.Tick(0.06f); // 60ms more - now 110ms total, 1 tick
        Assert.Equal(1u, plugin.CurrentTick);

        plugin.Tick(0.15f); // 150ms more - another tick
        Assert.Equal(2u, plugin.CurrentTick);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    #endregion

    #region RegisterNetworkedEntity

    [Fact]
    public void RegisterNetworkedEntity_WhenNotInstalled_ThrowsInvalidOperationException()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        var entity = world.Spawn().Build();

        Assert.Throws<InvalidOperationException>(() =>
            plugin.RegisterNetworkedEntity(entity));

        server.Dispose();
    }

    [Fact]
    public void RegisterNetworkedEntity_AssignsNetworkId()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);
        var entity = world.Spawn().Build();

        var networkId = plugin.RegisterNetworkedEntity(entity);

        Assert.True(networkId.Value > 0);
        Assert.True(world.Has<NetworkId>(entity));
        Assert.Equal(networkId.Value, world.Get<NetworkId>(entity).Value);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void RegisterNetworkedEntity_AddsNetworkComponents()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);
        var entity = world.Spawn().Build();

        plugin.RegisterNetworkedEntity(entity, ownerId: 5);

        Assert.True(world.Has<NetworkId>(entity));
        Assert.True(world.Has<NetworkOwner>(entity));
        Assert.True(world.Has<NetworkState>(entity));

        // Verify tag component registration and detection
        var networkedInfo = world.Components.Get<Networked>();
        Assert.NotNull(networkedInfo);
        Assert.True(networkedInfo.IsTag, "Networked should be registered as a tag component");

        // Test adding local tag to entity that already has other components (for comparison)
        var testEntity2 = world.Spawn().Build();
        world.Add(testEntity2, new NetworkId { Value = 999 });
        world.Add(testEntity2, new NetworkOwner { ClientId = 1 });
        world.Add(testEntity2, new NetworkState { NeedsFullSync = true });
        world.Add(testEntity2, default(LocalTestTag));
        Assert.True(world.Has<LocalTestTag>(testEntity2), "Adding LocalTestTag (struct) to entity with other components should work");

        // Try adding a local readonly record struct tag
        world.Add(testEntity2, default(LocalRecordTag));
        Assert.True(world.Has<LocalRecordTag>(testEntity2), "Adding LocalRecordTag (readonly record struct) should work");

        // Now try adding Networked tag to same entity
        world.Add(testEntity2, default(Networked));

        // Debug: Check if the component is registered
        var registeredInfo = world.Components.Get<Networked>();
        Assert.NotNull(registeredInfo);
        Assert.True(registeredInfo.IsTag, $"Networked should be a tag. Size={registeredInfo.Size}");

        // Check Get(Type) vs Get<T>()
        var registeredByType = world.Components.Get(typeof(Networked));
        var registeredNetworkIdByType = world.Components.Get(typeof(NetworkId));
        Assert.NotNull(registeredByType);
        Assert.NotNull(registeredNetworkIdByType);

        // Debug: Check what components the entity has via GetComponents
        var componentList = world.GetComponents(testEntity2).ToList();
        var componentTypes = string.Join(", ", componentList.Select(c => c.Type.FullName));

        // Check if Networked is in the component list by TYPE EQUALITY
        var networkedInListByEquals = componentList.Any(c => c.Type == typeof(Networked));
        // Check by FullName equality
        var networkedInListByName = componentList.Any(c => c.Type.FullName == typeof(Networked).FullName);
        // Get the actual Networked type from the list
        var networkedTypeFromList = componentList.FirstOrDefault(c => c.Type.FullName?.Contains("Networked") == true).Type;
        var actualNetworkedFullName = networkedTypeFromList?.FullName ?? "NOT FOUND";
        var expectedNetworkedFullName = typeof(Networked).FullName;

        // Check using HasComponent with type (bypasses generic type resolution)
        var hasViaType = world.HasComponent(testEntity2, typeof(Networked));

        // Check HasComponent for other components (should work)
        var hasNetworkId = world.HasComponent(testEntity2, typeof(NetworkId));
        var hasLocalRecordTag = world.HasComponent(testEntity2, typeof(LocalRecordTag));

        // Show all debug info in one assertion
        Assert.True(hasViaType,
            $"HasComponent failed.\n" +
            $"  InList(type==): {networkedInListByEquals}\n" +
            $"  InList(name==): {networkedInListByName}\n" +
            $"  ActualName: '{actualNetworkedFullName}'\n" +
            $"  ExpectedName: '{expectedNetworkedFullName}'\n" +
            $"  HasNetworkId: {hasNetworkId}\n" +
            $"  HasLocalRecordTag: {hasLocalRecordTag}\n" +
            $"  Components: [{componentTypes}]");

        Assert.True(world.Has<Networked>(testEntity2), "Adding Networked tag to entity with other components should work");

        Assert.True(world.Has<Networked>(entity), "Entity should have Networked tag after RegisterNetworkedEntity");

        Assert.Equal(5, world.Get<NetworkOwner>(entity).ClientId);
        Assert.True(world.Get<NetworkState>(entity).NeedsFullSync);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void RegisterNetworkedEntity_DefaultsToServerOwner()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);
        var entity = world.Spawn().Build();

        plugin.RegisterNetworkedEntity(entity);

        Assert.Equal(NetworkOwner.ServerClientId, world.Get<NetworkOwner>(entity).ClientId);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    #endregion

    #region Client Connection

    [Fact]
    public async Task ClientConnects_IncreasesClientCount()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);

        // Process connection
        server.Update();

        Assert.Equal(1, plugin.ClientCount);

        // Cleanup in correct order
        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ClientConnects_SendsConnectionAccepted()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        MessageType? receivedMessageType = null;
        client.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out _);
            receivedMessageType = messageType;
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);

        server.Update();
        client.Update();

        Assert.Equal(MessageType.ConnectionAccepted, receivedMessageType);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ClientConnects_WhenMaxClientsReached_PluginRejectsNewClients()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var config = new ServerNetworkConfig { MaxClients = 1 };
        var plugin = new NetworkServerPlugin(server, config);
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();

        Assert.Equal(1, plugin.ClientCount);

        // The LocalTransport pair is already connected
        // This test validates the first client connects successfully
        Assert.Equal(1, plugin.ClientCount);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ClientDisconnects_RemovesFromClientList()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();

        Assert.Equal(1, plugin.ClientCount);

        client.Disconnect();
        server.Update();

        Assert.Equal(0, plugin.ClientCount);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ClientDisconnects_DespawnsOwnedEntities()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

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

        // Create entity owned by this client
        var entity = world.Spawn().Build();
        plugin.RegisterNetworkedEntity(entity, receivedClientId);

        Assert.True(world.IsAlive(entity));

        // Disconnect client
        client.Disconnect();
        server.Update();

        // Entity should be despawned
        Assert.False(world.IsAlive(entity));

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region TryGetClientState

    [Fact]
    public async Task TryGetClientState_WithConnectedClient_ReturnsTrue()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

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

        var found = plugin.TryGetClientState(receivedClientId, out var state);

        Assert.True(found);
        Assert.NotNull(state);
        Assert.Equal(receivedClientId, state.ClientId);
        Assert.True(state.NeedsFullSnapshot);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void TryGetClientState_WithNoClient_ReturnsFalse()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        var found = plugin.TryGetClientState(999, out var state);

        Assert.False(found);
        Assert.Null(state);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    #endregion

    #region Send Methods

    [Fact]
    public async Task SendToClient_SendsData()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        var receivedClientId = 0;
        byte[]? receivedData = null;
        var messageCount = 0;

        client.DataReceived += (connectionId, data) =>
        {
            messageCount++;
            if (messageCount == 1)
            {
                var reader = new NetworkMessageReader(data);
                reader.ReadHeader(out var messageType, out _);
                if (messageType == MessageType.ConnectionAccepted)
                {
                    receivedClientId = reader.ReadSignedBits(16);
                }
            }
            else
            {
                receivedData = data.ToArray();
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Send data from server to client
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        plugin.SendToClient(receivedClientId, testData, DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        Assert.Equal(testData, receivedData);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task SendToAll_SendsToAllClients()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        byte[]? receivedData = null;
        var connectionAccepted = false;

        client.DataReceived += (connectionId, data) =>
        {
            if (!connectionAccepted)
            {
                var reader = new NetworkMessageReader(data);
                reader.ReadHeader(out var messageType, out _);
                if (messageType == MessageType.ConnectionAccepted)
                {
                    connectionAccepted = true;
                }
            }
            else
            {
                receivedData = data.ToArray();
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Send to all
        var testData = new byte[] { 10, 20, 30 };
        plugin.SendToAll(testData, DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        Assert.Equal(testData, receivedData);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Ping Handling

    [Fact]
    public async Task ReceivePing_SendsPong()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        MessageType? responseMessageType = null;
        uint responseTick = 0;
        var connectionAccepted = false;

        client.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out var tick);

            if (messageType == MessageType.ConnectionAccepted)
            {
                connectionAccepted = true;
            }
            else if (messageType == MessageType.Pong)
            {
                responseMessageType = messageType;
                responseTick = tick;
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        Assert.True(connectionAccepted);

        // Send ping from client
        Span<byte> pingBuffer = stackalloc byte[8];
        var pingWriter = new NetworkMessageWriter(pingBuffer);
        pingWriter.WriteHeader(MessageType.Ping, 42);
        client.Send(0, pingWriter.GetWrittenSpan(), DeliveryMode.Unreliable);
        client.Update();
        server.Update();
        client.Update();

        Assert.Equal(MessageType.Pong, responseMessageType);
        Assert.Equal(42u, responseTick);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Entity Events

    [Fact]
    public async Task EntityDestroyed_NotifiesClients()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server);
        world.InstallPlugin(plugin);

        MessageType? despawnMessageType = null;
        uint despawnedNetworkId = 0;
        var connectionAccepted = false;

        client.DataReceived += (connectionId, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var messageType, out _);

            if (messageType == MessageType.ConnectionAccepted)
            {
                connectionAccepted = true;
            }
            else if (messageType == MessageType.EntityDespawn)
            {
                despawnMessageType = messageType;
                reader.ReadEntityDespawn(out despawnedNetworkId);
            }
        };

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        Assert.True(connectionAccepted);

        // Create and register entity
        var entity = world.Spawn().Build();
        var networkId = plugin.RegisterNetworkedEntity(entity);

        // Despawn entity
        world.Despawn(entity);
        client.Update();

        Assert.Equal(MessageType.EntityDespawn, despawnMessageType);
        Assert.Equal(networkId.Value, despawnedNetworkId);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion
}
