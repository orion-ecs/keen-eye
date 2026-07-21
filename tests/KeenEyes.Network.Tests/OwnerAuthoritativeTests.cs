using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Owner-authoritative test component: the owning client is authoritative for its value.
/// </summary>
public struct OwnerState : IComponent
{
    /// <summary>The owner-controlled value.</summary>
    public float Value;
}

/// <summary>
/// Server-authoritative test component used to verify per-component echo behavior.
/// </summary>
public struct ServerHealth : IComponent
{
    /// <summary>The server-controlled value.</summary>
    public float Value;
}

/// <summary>
/// Tests for the owner-authoritative synchronization strategy (issue #587):
/// upstream owner state, server-side ownership and validation, echo suppression,
/// runtime strategy metadata, and ownership requests.
/// </summary>
public sealed class OwnerAuthoritativeTests
{
    private const int TickRate = 60;

    private static MockNetworkSerializer CreateSerializer()
    {
        var serializer = new MockNetworkSerializer();

        serializer.RegisterComponent<OwnerState>(
            serialize: (ref BitWriter w, OwnerState c) => w.WriteFloat(c.Value),
            deserialize: (ref BitReader r) => new OwnerState { Value = r.ReadFloat() },
            new NetworkComponentInfo
            {
                Type = typeof(OwnerState),
                NetworkTypeId = 1,
                Strategy = SyncStrategy.OwnerAuthoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });

        serializer.RegisterComponent<ServerHealth>(
            serialize: (ref BitWriter w, ServerHealth c) => w.WriteFloat(c.Value),
            deserialize: (ref BitReader r) => new ServerHealth { Value = r.ReadFloat() },
            new NetworkComponentInfo
            {
                Type = typeof(ServerHealth),
                NetworkTypeId = 2,
                Strategy = SyncStrategy.Authoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });

        return serializer;
    }

    /// <summary>
    /// Performs the connection handshake and returns the assigned client ID.
    /// </summary>
    private static int Handshake(LocalTransport server, LocalTransport client, NetworkClientPlugin clientPlugin)
    {
        server.ListenAsync(7777).Wait();
        clientPlugin.ConnectAsync().Wait();
        server.Update();
        client.Update();
        return clientPlugin.LocalClientId;
    }

    #region Upstream owner state

    [Fact]
    public async Task SendOwnerState_LocallyOwnedEntity_ReachesServerWorld()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        await server.ListenAsync(7777);
        await clientPlugin.ConnectAsync();
        server.Update();
        client.Update();
        var clientId = clientPlugin.LocalClientId;

        // Server owns an entity on behalf of the client.
        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 0f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        // Client has the same entity locally, owns it, and sets its authoritative value.
        var clientEntity = clientPlugin.SpawnNetworkedEntity(netId.Value, clientId);
        clientWorld.Add(clientEntity, new OwnerState { Value = 42f });
        Assert.True(clientWorld.Has<LocallyOwned>(clientEntity));

        // One client frame sends the owner-authoritative state upstream.
        clientWorld.Update(0.1f);
        server.Update();

        Assert.Equal(42f, serverWorld.Get<OwnerState>(serverEntity).Value, 0.001f);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void SendOwnerState_UnchangedValue_NotResent()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        var clientId = Handshake(server, client, clientPlugin);

        var ownerStateUpdates = 0;
        server.DataReceived += (_, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var type, out uint _);
            if (type == MessageType.OwnerStateUpdate)
            {
                ownerStateUpdates++;
            }
        };

        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 0f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        var clientEntity = clientPlugin.SpawnNetworkedEntity(netId.Value, clientId);
        clientWorld.Add(clientEntity, new OwnerState { Value = 7f });

        // First frame: value changed, one update sent.
        clientWorld.Update(0.1f);
        server.Update();
        Assert.Equal(1, ownerStateUpdates);

        // Second frame: value unchanged, no additional update (dirty-checked).
        clientWorld.Update(0.1f);
        server.Update();
        Assert.Equal(1, ownerStateUpdates);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Server receive + validation

    [Fact]
    public void HandleOwnerStateUpdate_NonOwnerClient_ServerStateUnchanged()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        server.ListenAsync(7777).Wait();
        client.ConnectAsync("localhost", 7777).Wait();
        server.Update();
        client.Update();

        // Entity is owned by client 2, but the connected/sending client is client 1.
        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 5f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, ownerId: 2);

        SendOwnerStateMessage(client, netId.Value, new OwnerState { Value = 99f });
        server.Update();

        // Spoofed update rejected: server state unchanged.
        Assert.Equal(5f, serverWorld.Get<OwnerState>(serverEntity).Value, 0.001f);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void HandleOwnerStateUpdate_ValidationHookRejects_ServerStateUnchanged()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
            OwnerStateValidator = _ => false, // Reject everything.
        });
        serverWorld.InstallPlugin(serverPlugin);

        server.ListenAsync(7777).Wait();
        client.ConnectAsync("localhost", 7777).Wait();
        server.Update();
        client.Update();
        var clientId = serverPlugin.GetConnectedClients().First().ClientId;

        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 5f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        SendOwnerStateMessage(client, netId.Value, new OwnerState { Value = 99f });
        server.Update();

        // Rejected by the validation hook: server state unchanged.
        Assert.Equal(5f, serverWorld.Get<OwnerState>(serverEntity).Value, 0.001f);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void HandleOwnerStateUpdate_ValidationHookAccepts_ServerStateApplied()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        var validated = new List<int>();
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
            OwnerStateValidator = ctx =>
            {
                validated.Add(ctx.ClientId);
                return true;
            },
        });
        serverWorld.InstallPlugin(serverPlugin);

        server.ListenAsync(7777).Wait();
        client.ConnectAsync("localhost", 7777).Wait();
        server.Update();
        client.Update();
        var clientId = serverPlugin.GetConnectedClients().First().ClientId;

        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 5f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        SendOwnerStateMessage(client, netId.Value, new OwnerState { Value = 99f });
        server.Update();

        Assert.Equal(99f, serverWorld.Get<OwnerState>(serverEntity).Value, 0.001f);
        Assert.Contains(clientId, validated);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void HandleOwnerStateUpdate_ServerAuthoritativeComponent_Rejected()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        server.ListenAsync(7777).Wait();
        client.ConnectAsync("localhost", 7777).Wait();
        server.Update();
        client.Update();
        var clientId = serverPlugin.GetConnectedClients().First().ClientId;

        // Entity owned by the sender, but the component is server-authoritative.
        var serverEntity = serverWorld.Spawn().With(new ServerHealth { Value = 100f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        var serializer = CreateSerializer();
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.OwnerStateUpdate, 1);
        writer.WriteNetworkId(netId.Value);
        writer.WriteComponentCount(1);
        writer.WriteComponent(serializer, typeof(ServerHealth), new ServerHealth { Value = 1f });
        client.Send(0, writer.GetWrittenSpan(), DeliveryMode.UnreliableSequenced);
        server.Update();

        // Non-owner-authoritative component ignored: server state unchanged.
        Assert.Equal(100f, serverWorld.Get<ServerHealth>(serverEntity).Value, 0.001f);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Echo suppression

    [Fact]
    public void ServerSend_OwnerAuthoritativeComponent_NotEchoedToOwner()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        server.ListenAsync(7777).Wait();
        client.ConnectAsync("localhost", 7777).Wait();
        server.Update();
        client.Update();
        var clientId = serverPlugin.GetConnectedClients().First().ClientId;

        var componentDeltas = 0;
        client.DataReceived += (_, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var type, out uint _);
            if (type == MessageType.ComponentDelta)
            {
                componentDeltas++;
            }
        };

        // Entity owned by the client with only an owner-authoritative component.
        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 1f }).Build();
        serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        // First tick: full sync (EntitySpawn), not a delta.
        serverWorld.Update(0.02f);
        client.Update();

        // Server changes the owner-authoritative value and ticks again.
        serverWorld.Set(serverEntity, new OwnerState { Value = 2f });
        serverWorld.Update(0.02f);
        client.Update();

        // The owner never receives its own owner-authoritative state back.
        Assert.Equal(0, componentDeltas);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void ServerSend_ServerAuthoritativeComponentOnClientOwnedEntity_ReachesOwner()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        server.ListenAsync(7777).Wait();
        client.ConnectAsync("localhost", 7777).Wait();
        server.Update();
        client.Update();
        var clientId = serverPlugin.GetConnectedClients().First().ClientId;

        var componentDeltas = 0;
        client.DataReceived += (_, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var type, out uint _);
            if (type == MessageType.ComponentDelta)
            {
                componentDeltas++;
            }
        };

        // Entity owned by the client but carrying a server-authoritative component.
        var serverEntity = serverWorld.Spawn().With(new ServerHealth { Value = 100f }).Build();
        serverPlugin.RegisterNetworkedEntity(serverEntity, clientId);

        serverWorld.Update(0.02f); // full sync
        client.Update();

        serverWorld.Set(serverEntity, new ServerHealth { Value = 50f });
        serverWorld.Update(0.02f); // delta
        client.Update();

        // Server-authoritative components still reach the owner (per-component split).
        Assert.True(componentDeltas >= 1);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void ClientReceive_OwnerAuthoritativeComponentOnLocallyOwnedEntity_Ignored()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreateSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        var clientId = Handshake(server, client, clientPlugin);

        // Locally owned entity with the client's authoritative value.
        var clientEntity = clientPlugin.SpawnNetworkedEntity(1u, clientId);
        clientWorld.Add(clientEntity, new OwnerState { Value = 42f });

        // Server sends a component update for that entity (as would arrive in a snapshot).
        var serializer = CreateSerializer();
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ComponentUpdate, 1);
        writer.WriteNetworkId(1u);
        writer.WriteComponentCount(1);
        writer.WriteComponent(serializer, typeof(OwnerState), new OwnerState { Value = 999f });
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        client.Update();

        // The client's own state wins; the incoming server value is ignored.
        Assert.Equal(42f, clientWorld.Get<OwnerState>(clientEntity).Value, 0.001f);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Strategy metadata

    [Fact]
    public void GetRegisteredComponentInfo_IdentifiesOwnerAuthoritativeVsAuthoritative()
    {
        var serializer = CreateSerializer();

        var infos = serializer.GetRegisteredComponentInfo().ToList();

        var ownerInfo = infos.Single(i => i.Type == typeof(OwnerState));
        var authInfo = infos.Single(i => i.Type == typeof(ServerHealth));

        Assert.Equal(SyncStrategy.OwnerAuthoritative, ownerInfo.Strategy);
        Assert.Equal(SyncStrategy.Authoritative, authInfo.Strategy);
    }

    #endregion

    #region Ownership requests

    [Fact]
    public void RequestOwnership_PolicyGrants_TransfersOwnership()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
            OwnershipRequestPolicy = _ => true,
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreateSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        var clientId = Handshake(server, client, clientPlugin);

        // Server-owned entity; client has a remote copy.
        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 0f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, NetworkOwner.ServerClientId);
        var clientEntity = clientPlugin.SpawnNetworkedEntity(netId.Value, NetworkOwner.ServerClientId);
        Assert.True(clientWorld.Has<RemotelyOwned>(clientEntity));

        clientPlugin.RequestOwnership(netId.Value);
        server.Update(); // server grants + broadcasts transfer
        client.Update(); // client applies transfer

        Assert.Equal(clientId, serverWorld.Get<NetworkOwner>(serverEntity).ClientId);
        Assert.True(clientWorld.Has<LocallyOwned>(clientEntity));
        Assert.False(clientWorld.Has<RemotelyOwned>(clientEntity));

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void RequestOwnership_PolicyDeniesByDefault_OwnershipUnchanged()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
            // No OwnershipRequestPolicy -> default deny.
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreateSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        Handshake(server, client, clientPlugin);

        var serverEntity = serverWorld.Spawn().With(new OwnerState { Value = 0f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity, NetworkOwner.ServerClientId);
        var clientEntity = clientPlugin.SpawnNetworkedEntity(netId.Value, NetworkOwner.ServerClientId);

        clientPlugin.RequestOwnership(netId.Value);
        server.Update();
        client.Update();

        // Denied: ownership stays with the server on both sides.
        Assert.Equal(NetworkOwner.ServerClientId, serverWorld.Get<NetworkOwner>(serverEntity).ClientId);
        Assert.True(clientWorld.Has<RemotelyOwned>(clientEntity));
        Assert.False(clientWorld.Has<LocallyOwned>(clientEntity));

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    private static void SendOwnerStateMessage(LocalTransport client, uint networkId, OwnerState state)
    {
        var serializer = CreateSerializer();
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.OwnerStateUpdate, 1);
        writer.WriteNetworkId(networkId);
        writer.WriteComponentCount(1);
        writer.WriteComponent(serializer, typeof(OwnerState), state);
        client.Send(0, writer.GetWrittenSpan(), DeliveryMode.UnreliableSequenced);
    }
}
