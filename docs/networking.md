# Networking Guide

The networking system provides authoritative server replication with client-side prediction and interpolation for responsive multiplayer games.

## Architecture Overview

KeenEyes networking uses a **server-authoritative** model:

```
┌─────────────────────────────────────────────────────────────┐
│                        SERVER                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Game Logic  │─▶│  Network    │─▶│ NetworkServerPlugin │  │
│  │  (World)    │  │  Systems    │  │    (Transport)      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   CLIENT 1      │ │   CLIENT 2      │ │   CLIENT N      │
│ ┌─────────────┐ │ │ ┌─────────────┐ │ │ ┌─────────────┐ │
│ │ Prediction  │ │ │ │Interpolation│ │ │ │Interpolation│ │
│ │ (local)     │ │ │ │ (remote)    │ │ │ │ (remote)    │ │
│ └─────────────┘ │ │ └─────────────┘ │ │ └─────────────┘ │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

**Key concepts:**
- **Server** owns the game state and replicates to clients
- **Clients** receive state updates and render the game
- **Prediction** allows responsive local player controls
- **Interpolation** smooths remote entity movement

## Getting Started

### 1. Define Replicated Components

Mark components for network replication using `[Replicated]`:

```csharp
using KeenEyes.Network;

[Replicated]
public partial struct Position
{
    public float X;
    public float Y;
    public float Z;
}

[Replicated(GenerateInterpolation = true)]
public partial struct Velocity
{
    public float X;
    public float Y;
}
```

The source generator creates serialization code and optional interpolation helpers.

### 2. Create the Server

```csharp
using KeenEyes;
using KeenEyes.Network;
using KeenEyes.Network.Transport;

// Create transport (use LocalTransport for testing, or implement your own)
var transport = new LocalTransport();
await transport.ListenAsync(7777);

// Configure server
var config = new ServerNetworkConfig
{
    TickRate = 60,           // Network ticks per second
    MaxClients = 16,         // Maximum concurrent connections
    Serializer = new MySerializer()  // Your component serializer
};

// Create world with network plugin
using var world = new World();
var serverPlugin = new NetworkServerPlugin(transport, config);
world.InstallPlugin(serverPlugin);

// Create a networked entity
var player = world.Spawn()
    .With(new Position { X = 0, Y = 0, Z = 0 })
    .With(new Velocity { X = 0, Y = 0 })
    .Build();

// Register for replication (assigns network ID)
serverPlugin.RegisterNetworkedEntity(player, ownerId: 1);
```

### 3. Create the Client

```csharp
var transport = new LocalTransport();

var config = new ClientNetworkConfig
{
    ServerAddress = "127.0.0.1",
    ServerPort = 7777,
    EnablePrediction = true,
    Serializer = new MySerializer(),
    Interpolator = new MyInterpolator()
};

using var world = new World();
var clientPlugin = new NetworkClientPlugin(transport, config);
world.InstallPlugin(clientPlugin);

// Connect to server
await clientPlugin.ConnectAsync();

// Handle connection events
clientPlugin.Connected += () => Console.WriteLine("Connected!");
clientPlugin.Disconnected += () => Console.WriteLine("Disconnected!");
```

### 4. Game Loop

Both server and client run their game loops:

```csharp
var stopwatch = Stopwatch.StartNew();
var lastTime = 0.0;

while (running)
{
    var currentTime = stopwatch.Elapsed.TotalSeconds;
    var deltaTime = (float)(currentTime - lastTime);
    lastTime = currentTime;

    // Process network transport
    transport.Update();

    // Update ECS world (runs network systems automatically)
    world.Update(deltaTime);
}
```

## Sync Strategies

Choose the appropriate sync strategy for each component type:

| Strategy | Use Case | Latency | Bandwidth |
|----------|----------|---------|-----------|
| `Authoritative` | NPCs, world objects, game state | High | Low |
| `Interpolated` | Remote players, projectiles | Medium | Medium |
| `Predicted` | Local player | Low | Higher |
| `OwnerAuthoritative` | Cosmetics, non-critical data | Lowest | Low |

### Authoritative (Default)

Server state is applied directly. Use for server-controlled entities:

```csharp
[Replicated(Strategy = SyncStrategy.Authoritative)]
public partial struct Health
{
    public int Current;
    public int Max;
}
```

### Interpolated

Smooth movement for remote entities by blending between snapshots:

```csharp
[Replicated(
    Strategy = SyncStrategy.Interpolated,
    GenerateInterpolation = true)]
public partial struct Position
{
    public float X;
    public float Y;
}
```

The client renders slightly behind server time (typically 100ms) and interpolates between received states for smooth visuals.

### Predicted

Local player runs ahead of the server for responsive controls:

```csharp
[Replicated(
    Strategy = SyncStrategy.Predicted,
    GeneratePrediction = true)]
public partial struct PlayerPosition
{
    public float X;
    public float Y;
}
```

When server state arrives, the client:
1. Compares server state to predicted state
2. If mismatch: rolls back and re-simulates from the server state
3. Applies any pending inputs to catch up to current tick

### Owner Authoritative

Client owns the data; server only validates:

```csharp
[Replicated(Strategy = SyncStrategy.OwnerAuthoritative)]
public partial struct Cosmetics
{
    public int SkinId;
    public int HatId;
}
```

**Warning:** Vulnerable to cheating. Only use for non-gameplay-critical data.

## Delta Compression

For bandwidth efficiency, components can implement delta serialization:

```csharp
[Replicated(SupportsDelta = true)]
public partial struct Transform : INetworkDeltaSerializable<Transform>
{
    public float X;
    public float Y;
    public float Rotation;

    public uint GetDirtyMask(in Transform baseline)
    {
        uint mask = 0;
        if (MathF.Abs(X - baseline.X) > 0.001f) mask |= 1;
        if (MathF.Abs(Y - baseline.Y) > 0.001f) mask |= 2;
        if (MathF.Abs(Rotation - baseline.Rotation) > 0.001f) mask |= 4;
        return mask;
    }

    public void NetworkSerializeDelta(ref BitWriter writer, in Transform baseline, uint mask)
    {
        if ((mask & 1) != 0) writer.WriteFloat(X);
        if ((mask & 2) != 0) writer.WriteFloat(Y);
        if ((mask & 4) != 0) writer.WriteFloat(Rotation);
    }

    public void NetworkDeserializeDelta(ref BitReader reader, ref Transform baseline, uint mask)
    {
        if ((mask & 1) != 0) baseline.X = reader.ReadFloat();
        if ((mask & 2) != 0) baseline.Y = reader.ReadFloat();
        if ((mask & 4) != 0) baseline.Rotation = reader.ReadFloat();
    }
}
```

Only changed fields are sent, reducing bandwidth by 50-80% for typical game state.

## Input Handling

For predicted entities, send inputs to the server:

```csharp
// Define your input structure
public struct PlayerInput : INetworkInput
{
    public uint Tick { get; set; }
    public float MoveX;
    public float MoveY;
    public bool Jump;
}

// Record and send input each frame
var input = new PlayerInput
{
    MoveX = GetHorizontalAxis(),
    MoveY = GetVerticalAxis(),
    Jump = IsJumpPressed()
};

clientPlugin.RecordInput(localPlayerEntity, input);
```

The server receives inputs via event:

```csharp
serverPlugin.ClientInputReceived += (clientId, tick, inputData) =>
{
    // Deserialize and apply input to the client's entity
    var input = DeserializeInput(inputData);
    ApplyInputToEntity(clientId, input);
};
```

## Entity Ownership

Entities can be owned by the server or a specific client:

```csharp
// Server-owned (default)
serverPlugin.RegisterNetworkedEntity(npcEntity, ownerId: 0);

// Client-owned (for players)
serverPlugin.RegisterNetworkedEntity(playerEntity, ownerId: clientId);
```

Transfer ownership dynamically:

```csharp
// Server sends ownership transfer
Span<byte> buffer = stackalloc byte[16];
var writer = new NetworkMessageWriter(buffer);
writer.WriteHeader(MessageType.OwnershipTransfer, currentTick);
writer.WriteNetworkId(entityNetworkId);
writer.WriteSignedBits(newOwnerId, 16);
serverPlugin.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
```

## Entity Hierarchy

Parent-child relationships are automatically replicated:

```csharp
// On server
world.SetParent(weaponEntity, playerEntity);

// Send hierarchy change to clients
serverPlugin.SendHierarchyChange(weaponEntity, playerEntity);
```

## Late Joiners

New clients automatically receive a full world snapshot:

```csharp
// Server handles this automatically when client connects
// But you can manually trigger if needed:
serverPlugin.SendFullSnapshot(clientId);
```

## Transport Layer

Implement `INetworkTransport` for your networking library:

```csharp
public interface INetworkTransport : IDisposable
{
    event Action<int>? ClientConnected;
    event Action<int>? ClientDisconnected;
    event Action<int, ReadOnlySpan<byte>>? DataReceived;
    event Action<ConnectionState>? StateChanged;

    Task ListenAsync(int port, CancellationToken cancellationToken = default);
    Task ConnectAsync(string address, int port, CancellationToken cancellationToken = default);
    void Disconnect(int clientId = 0);
    void Send(int clientId, ReadOnlySpan<byte> data, DeliveryMode mode);
    void SendToAll(ReadOnlySpan<byte> data, DeliveryMode mode);
    void SendToAllExcept(int excludeClientId, ReadOnlySpan<byte> data, DeliveryMode mode);
    void Update();
}
```

### LocalTransport (Testing)

Use `LocalTransport` for testing without real networking:

```csharp
var (serverTransport, clientTransport) = LocalTransport.CreatePair();

// Server uses serverTransport
// Client uses clientTransport
// Messages are delivered synchronously
```

### Production Transports

For production, integrate with networking libraries like:
- **LiteNetLib** - UDP with reliability layers
- **ENet** - Reliable UDP protocol
- **WebSocket** - For browser clients

## Message Types

The protocol uses these message types:

| Type | Direction | Purpose |
|------|-----------|---------|
| `ConnectionAccepted` | S→C | Server accepts connection, assigns client ID |
| `ConnectionRejected` | S→C | Server rejects connection with reason |
| `EntitySpawn` | S→C | New entity created |
| `EntityDespawn` | S→C | Entity destroyed |
| `FullSnapshot` | S→C | Complete world state (for late joiners) |
| `DeltaSnapshot` | S→C | Incremental state update |
| `ComponentUpdate` | S→C | Full component value |
| `ComponentDelta` | S→C | Delta-compressed component |
| `HierarchyChange` | S→C | Parent-child relationship changed |
| `OwnershipTransfer` | S→C | Entity ownership changed |
| `ClientInput` | C→S | Player input |
| `ClientAck` | C→S | Acknowledge received tick |
| `Ping`/`Pong` | Both | RTT measurement |

## Configuration Reference

### ServerNetworkConfig

```csharp
var config = new ServerNetworkConfig
{
    TickRate = 60,           // Network updates per second
    MaxClients = 32,         // Maximum connections
    Serializer = serializer, // Component serializer
};
```

### ClientNetworkConfig

```csharp
var config = new ClientNetworkConfig
{
    ServerAddress = "127.0.0.1",
    ServerPort = 7777,
    TickRate = 60,
    EnablePrediction = true,
    InputBufferSize = 64,    // Frames of input to buffer
    Serializer = serializer,
    Interpolator = interpolator,
    InputApplicator = inputApplicator
};
```

## Best Practices

### Bandwidth Optimization

1. **Use delta compression** for frequently-updated components
2. **Prioritize entities** - nearby entities update more frequently
3. **Quantize values** - reduce precision for rotation (e.g., 16-bit angles)
4. **Skip unchanged entities** - only send when state changes

### Latency Hiding

1. **Predict local player** movement client-side
2. **Interpolate remote players** between snapshots
3. **Buffer inputs** to handle packet loss
4. **Extrapolate** when snapshots are late (with limits)

### Security

1. **Validate all inputs** on the server
2. **Never trust client state** for gameplay-critical data
3. **Use `OwnerAuthoritative`** only for cosmetic data
4. **Rate-limit inputs** to prevent spam

## Debugging

### Measure RTT

```csharp
// Client sends ping periodically
clientPlugin.SendPing();

// Check RTT
Console.WriteLine($"RTT: {clientPlugin.RoundTripTimeMs}ms");
```

### Monitor Network State

```csharp
// Check connection status
if (clientPlugin.IsConnected)
{
    Console.WriteLine($"Connected as client {clientPlugin.LocalClientId}");
    Console.WriteLine($"Last server tick: {clientPlugin.LastReceivedTick}");
}

// Server stats
Console.WriteLine($"Connected clients: {serverPlugin.ClientCount}");
foreach (var client in serverPlugin.GetConnectedClients())
{
    Console.WriteLine($"  Client {client.ClientId}: RTT={client.RoundTripTimeMs}ms");
}
```

## See Also

- [Plugin System](plugins.md) - How plugins work
- [Systems](systems.md) - System execution phases
- [Serialization](serialization.md) - Binary serialization
