using KeenEyes;
using KeenEyes.Network;
using KeenEyes.Network.Transport;
using KeenEyes.Sample.Multiplayer;

// =============================================================================
// KEEN EYES ECS - Multiplayer Networking Demo
// =============================================================================
// This sample demonstrates:
// 1. Server-authoritative networking with entity replication
// 2. Client-side prediction for responsive local player controls
// 3. Interpolation for smooth remote player movement
// 4. Delta compression for bandwidth efficiency
// 5. Full snapshot synchronization
//
// Note: This sample uses LocalTransport for in-process 1:1 testing.
// For real multiplayer with multiple clients, implement INetworkTransport
// using LiteNetLib, ENet, or WebSocket.
// =============================================================================

Console.WriteLine("KeenEyes ECS - Multiplayer Networking Demo");
Console.WriteLine(new string('=', 50));

// Create serializer (shared between server and clients)
var serializer = new GameSerializer();

// =============================================================================
// PART 1: Create Server
// =============================================================================

Console.WriteLine("\n[1] Creating Server\n");

// LocalTransport creates a paired server/client for in-process testing
var (serverTransport, clientTransport) = LocalTransport.CreatePair();

// Create server world
using var serverWorld = new World();

var serverConfig = new ServerNetworkConfig
{
    TickRate = 60,
    MaxClients = 4,
    Serializer = serializer,
};

var serverPlugin = new NetworkServerPlugin(serverTransport, serverConfig);
serverWorld.InstallPlugin(serverPlugin);

// Handle client input on server
serverPlugin.ClientInputReceived += (clientId, tick, inputData) =>
{
    Console.WriteLine($"  Server received input from client {clientId} at tick {tick}");
};

// Start listening
await serverTransport.ListenAsync(7777);
Console.WriteLine("Server started (using LocalTransport for testing)");

// =============================================================================
// PART 2: Create Client
// =============================================================================

Console.WriteLine("\n[2] Creating Client\n");

using var clientWorld = new World();

// Pre-register component types so they exist before receiving network data
// This is required because KeenEyes uses per-world component registries
clientWorld.Components.Register<Position>();
clientWorld.Components.Register<Velocity>();

var clientConfig = new ClientNetworkConfig
{
    ServerAddress = "localhost",
    ServerPort = 7777,
    EnablePrediction = true,
    Serializer = serializer,
};
var clientPlugin = new NetworkClientPlugin(clientTransport, clientConfig);
clientWorld.InstallPlugin(clientPlugin);

clientPlugin.Connected += () => Console.WriteLine("  Client connected!");
clientPlugin.Disconnected += () => Console.WriteLine("  Client disconnected");

// Connect to server
Console.WriteLine("Connecting to server...");
await clientTransport.ConnectAsync("localhost", 7777);

// Process connection messages
serverTransport.Update();
clientTransport.Update();

Console.WriteLine($"  Assigned Client ID: {clientPlugin.LocalClientId}");

// =============================================================================
// PART 3: Create Networked Entities on Server
// =============================================================================

Console.WriteLine("\n[3] Creating Networked Entities\n");

// Create local player entity (owned by the connected client)
var localPlayer = serverWorld.Spawn()
    .WithPosition(x: 100, y: 100)
    .WithVelocity(x: 0, y: 0)
    .Build();
var localPlayerNetId = serverPlugin.RegisterNetworkedEntity(localPlayer, ownerId: clientPlugin.LocalClientId);
Console.WriteLine($"  Local Player: Entity={localPlayer}, NetworkId={localPlayerNetId.Value}, Owner={clientPlugin.LocalClientId}");

// Create remote player entity (simulates another player, server-owned for this demo)
var remotePlayer = serverWorld.Spawn()
    .WithPosition(x: 200, y: 100)
    .WithVelocity(x: -1, y: 0)
    .Build();
var remotePlayerNetId = serverPlugin.RegisterNetworkedEntity(remotePlayer, ownerId: 0);
Console.WriteLine($"  Remote Player: Entity={remotePlayer}, NetworkId={remotePlayerNetId.Value}, Owner=Server");

// Create NPC (server-owned)
var npc = serverWorld.Spawn()
    .WithPosition(x: 150, y: 200)
    .WithVelocity(x: 1, y: 0)
    .Build();
var npcNetId = serverPlugin.RegisterNetworkedEntity(npc, ownerId: 0);
Console.WriteLine($"  NPC: Entity={npc}, NetworkId={npcNetId.Value}, Owner=Server");

// =============================================================================
// PART 4: Send Full Snapshot to Client
// =============================================================================

Console.WriteLine("\n[4] Sending Full Snapshot\n");

// Server sends full snapshot to the client
serverPlugin.SendFullSnapshot(clientPlugin.LocalClientId);

// Process messages
serverTransport.Update();
clientTransport.Update();

// Verify entities replicated to client
Console.WriteLine("  Entities received by client:");
foreach (var entity in clientWorld.Query<Position>())
{
    ref readonly var pos = ref clientWorld.Get<Position>(entity);
    var owner = clientWorld.Has<KeenEyes.Network.Components.NetworkOwner>(entity)
        ? clientWorld.Get<KeenEyes.Network.Components.NetworkOwner>(entity).ClientId
        : -1;
    var isLocal = owner == clientPlugin.LocalClientId;
    Console.WriteLine($"    {entity}: Position={pos}, Owner={owner} ({(isLocal ? "LOCAL" : "REMOTE")})");
}

// =============================================================================
// PART 5: Simulate Game Loop with Movement
// =============================================================================

Console.WriteLine("\n[5] Simulating Game Loop\n");

const float deltaTime = 1f / 60f;
const int simulationFrames = 10;

Console.WriteLine($"Running {simulationFrames} frames of simulation...\n");

for (int frame = 0; frame < simulationFrames; frame++)
{
    // --- Server Update ---

    // Move NPC in a pattern
    {
        ref var npcPos = ref serverWorld.Get<Position>(npc);
        ref readonly var npcVel = ref serverWorld.Get<Velocity>(npc);
        npcPos.X += npcVel.X * deltaTime * 60;
    }

    // Move remote player
    {
        ref var remotePos = ref serverWorld.Get<Position>(remotePlayer);
        ref readonly var remoteVel = ref serverWorld.Get<Velocity>(remotePlayer);
        remotePos.X += remoteVel.X * deltaTime * 60;
    }

    // Simulate local player input (would normally come from client)
    {
        ref var localPos = ref serverWorld.Get<Position>(localPlayer);
        localPos.X += 2.0f; // Move right
    }

    // Server tick
    if (serverPlugin.Tick(deltaTime))
    {
        serverWorld.Update(deltaTime);
    }

    // --- Client Update ---
    clientWorld.Update(deltaTime);

    // Process network messages
    serverTransport.Update();
    clientTransport.Update();

    // Print state every few frames
    if (frame % 3 == 0)
    {
        ref readonly var serverLocalPos = ref serverWorld.Get<Position>(localPlayer);
        ref readonly var serverRemotePos = ref serverWorld.Get<Position>(remotePlayer);
        ref readonly var serverNpcPos = ref serverWorld.Get<Position>(npc);
        Console.WriteLine($"  Frame {frame}:");
        Console.WriteLine($"    Server: Local={serverLocalPos}, Remote={serverRemotePos}, NPC={serverNpcPos}");
    }
}

// =============================================================================
// PART 6: Delta Compression Demo
// =============================================================================

Console.WriteLine("\n[6] Delta Compression Demo\n");

// Position supports delta serialization via INetworkDeltaSerializable
var baseline = new Position { X = 100, Y = 100 };
var current = new Position { X = 110, Y = 100 }; // Only X changed

var dirtyMask = current.GetDirtyMask(baseline);
Console.WriteLine($"  Baseline: {baseline}");
Console.WriteLine($"  Current:  {current}");
Console.WriteLine($"  Dirty Mask: 0b{Convert.ToString(dirtyMask, 2).PadLeft(2, '0')} (bit 0=X, bit 1=Y)");
Console.WriteLine($"  Result: Only X is sent over network, saving 50% bandwidth!");

// =============================================================================
// PART 7: Entity Hierarchy Demo
// =============================================================================

Console.WriteLine("\n[7] Entity Hierarchy Demo\n");

// Create a weapon attached to local player
var weapon = serverWorld.Spawn()
    .WithPosition(x: 10, y: 0) // Offset from player
    .Build();
var weaponNetId = serverPlugin.RegisterNetworkedEntity(weapon);

// Set parent-child relationship
serverWorld.SetParent(weapon, localPlayer);

// Send hierarchy change to client
serverPlugin.SendHierarchyChange(weapon, localPlayer);
serverTransport.Update();
clientTransport.Update();

Console.WriteLine($"  Created weapon (NetworkId={weaponNetId.Value}) attached to LocalPlayer");
Console.WriteLine($"  Server: weapon parent = {serverWorld.GetParent(weapon)}");

// Verify on client
if (clientPlugin.NetworkIds.TryGetLocalEntity(weaponNetId.Value, out var clientWeapon))
{
    var clientWeaponParent = clientWorld.GetParent(clientWeapon);
    Console.WriteLine($"  Client: weapon parent = {clientWeaponParent}");
}

// =============================================================================
// PART 8: Cleanup
// =============================================================================

Console.WriteLine("\n[8] Cleanup\n");

serverWorld.UninstallPlugin("NetworkServer");
clientWorld.UninstallPlugin("NetworkClient");

serverTransport.Dispose();
clientTransport.Dispose();

Console.WriteLine("All connections closed.");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Multiplayer Demo Complete!");
Console.WriteLine("\nKey Features Demonstrated:");
Console.WriteLine("  - Server-authoritative entity replication");
Console.WriteLine("  - Network ID assignment and mapping");
Console.WriteLine("  - Full snapshot sync for clients");
Console.WriteLine("  - Entity hierarchy replication (parent-child)");
Console.WriteLine("  - Delta compression for bandwidth efficiency");
Console.WriteLine("  - Component pre-registration for network types");
Console.WriteLine("\nFor real multiplayer, replace LocalTransport with:");
Console.WriteLine("  - TcpTransport (built-in, reliable ordered)");
Console.WriteLine("  - UdpTransport (built-in, configurable reliability)");
Console.WriteLine("  - WebSocket (for browser clients)");
