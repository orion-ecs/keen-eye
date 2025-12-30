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
// Supports three transport modes:
// - Local:  In-process paired transport for quick testing (default)
// - TCP:    Reliable ordered delivery over TCP sockets
// - UDP:    Configurable reliability with multiple delivery modes
//
// Run with --help for usage information.
// =============================================================================

var options = TransportOptions.Parse(args);

if (options.ShowHelp)
{
    TransportOptions.PrintHelp();
    return;
}

Console.WriteLine("KeenEyes ECS - Multiplayer Networking Demo");
Console.WriteLine(new string('=', 50));
Console.WriteLine($"Transport: {options.Transport}");

switch (options.Transport)
{
    case TransportType.Local:
        await RunLocalDemo();
        break;

    case TransportType.Tcp:
    case TransportType.Udp:
        if (options.IsServer)
        {
            await RunServerDemo(options);
        }
        else
        {
            await RunClientDemo(options);
        }
        break;
}

// =============================================================================
// LOCAL DEMO - Server and Client in same process
// =============================================================================
async Task RunLocalDemo()
{
    Console.WriteLine("Mode: Local (in-process server + client)\n");

    var serializer = new GameSerializer();
    var (serverTransport, clientTransport) = TransportOptions.CreateLocalTransportPair();

    try
    {
        // --- Create Server ---
        Console.WriteLine("[1] Creating Server\n");

        using var serverWorld = new World();
        var serverConfig = new ServerNetworkConfig
        {
            TickRate = 60,
            MaxClients = 4,
            Serializer = serializer,
        };

        var serverPlugin = new NetworkServerPlugin(serverTransport, serverConfig);
        serverWorld.InstallPlugin(serverPlugin);

        serverPlugin.ClientInputReceived += (clientId, tick, inputData) =>
        {
            Console.WriteLine($"  Server received input from client {clientId} at tick {tick}");
        };

        await serverTransport.ListenAsync(7777);
        Console.WriteLine("Server started on port 7777");

        // --- Create Client ---
        Console.WriteLine("\n[2] Creating Client\n");

        using var clientWorld = new World();
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

        Console.WriteLine("Connecting to server...");
        await clientTransport.ConnectAsync("localhost", 7777);

        serverTransport.Update();
        clientTransport.Update();

        Console.WriteLine($"  Assigned Client ID: {clientPlugin.LocalClientId}");

        // --- Create Networked Entities ---
        Console.WriteLine("\n[3] Creating Networked Entities\n");

        var localPlayer = serverWorld.Spawn()
            .WithPosition(x: 100, y: 100)
            .WithVelocity(x: 0, y: 0)
            .Build();
        var localPlayerNetId = serverPlugin.RegisterNetworkedEntity(localPlayer, ownerId: clientPlugin.LocalClientId);
        Console.WriteLine($"  Local Player: Entity={localPlayer}, NetworkId={localPlayerNetId.Value}, Owner={clientPlugin.LocalClientId}");

        var remotePlayer = serverWorld.Spawn()
            .WithPosition(x: 200, y: 100)
            .WithVelocity(x: -1, y: 0)
            .Build();
        var remotePlayerNetId = serverPlugin.RegisterNetworkedEntity(remotePlayer, ownerId: 0);
        Console.WriteLine($"  Remote Player: Entity={remotePlayer}, NetworkId={remotePlayerNetId.Value}, Owner=Server");

        var npc = serverWorld.Spawn()
            .WithPosition(x: 150, y: 200)
            .WithVelocity(x: 1, y: 0)
            .Build();
        var npcNetId = serverPlugin.RegisterNetworkedEntity(npc, ownerId: 0);
        Console.WriteLine($"  NPC: Entity={npc}, NetworkId={npcNetId.Value}, Owner=Server");

        // --- Send Snapshot ---
        Console.WriteLine("\n[4] Sending Full Snapshot\n");

        serverPlugin.SendFullSnapshot(clientPlugin.LocalClientId);
        serverTransport.Update();
        clientTransport.Update();

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

        // --- Game Loop ---
        Console.WriteLine("\n[5] Simulating Game Loop\n");

        const float deltaTime = 1f / 60f;
        const int simulationFrames = 10;

        Console.WriteLine($"Running {simulationFrames} frames of simulation...\n");

        for (int frame = 0; frame < simulationFrames; frame++)
        {
            // Move entities
            {
                ref var npcPos = ref serverWorld.Get<Position>(npc);
                ref readonly var npcVel = ref serverWorld.Get<Velocity>(npc);
                npcPos.X += npcVel.X * deltaTime * 60;
            }

            {
                ref var remotePos = ref serverWorld.Get<Position>(remotePlayer);
                ref readonly var remoteVel = ref serverWorld.Get<Velocity>(remotePlayer);
                remotePos.X += remoteVel.X * deltaTime * 60;
            }

            {
                ref var localPos = ref serverWorld.Get<Position>(localPlayer);
                localPos.X += 2.0f;
            }

            if (serverPlugin.Tick(deltaTime))
            {
                serverWorld.Update(deltaTime);
            }

            clientWorld.Update(deltaTime);
            serverTransport.Update();
            clientTransport.Update();

            if (frame % 3 == 0)
            {
                ref readonly var serverLocalPos = ref serverWorld.Get<Position>(localPlayer);
                ref readonly var serverRemotePos = ref serverWorld.Get<Position>(remotePlayer);
                ref readonly var serverNpcPos = ref serverWorld.Get<Position>(npc);
                Console.WriteLine($"  Frame {frame}:");
                Console.WriteLine($"    Server: Local={serverLocalPos}, Remote={serverRemotePos}, NPC={serverNpcPos}");
            }
        }

        // --- Delta Compression Demo ---
        Console.WriteLine("\n[6] Delta Compression Demo\n");

        var baseline = new Position { X = 100, Y = 100 };
        var current = new Position { X = 110, Y = 100 };

        var dirtyMask = current.GetDirtyMask(baseline);
        Console.WriteLine($"  Baseline: {baseline}");
        Console.WriteLine($"  Current:  {current}");
        Console.WriteLine($"  Dirty Mask: 0b{Convert.ToString(dirtyMask, 2).PadLeft(2, '0')} (bit 0=X, bit 1=Y)");
        Console.WriteLine($"  Result: Only X is sent over network, saving 50% bandwidth!");

        // --- Hierarchy Demo ---
        Console.WriteLine("\n[7] Entity Hierarchy Demo\n");

        var weapon = serverWorld.Spawn()
            .WithPosition(x: 10, y: 0)
            .Build();
        var weaponNetId = serverPlugin.RegisterNetworkedEntity(weapon);

        serverWorld.SetParent(weapon, localPlayer);
        serverPlugin.SendHierarchyChange(weapon, localPlayer);
        serverTransport.Update();
        clientTransport.Update();

        Console.WriteLine($"  Created weapon (NetworkId={weaponNetId.Value}) attached to LocalPlayer");
        Console.WriteLine($"  Server: weapon parent = {serverWorld.GetParent(weapon)}");

        if (clientPlugin.NetworkIds.TryGetLocalEntity(weaponNetId.Value, out var clientWeapon))
        {
            var clientWeaponParent = clientWorld.GetParent(clientWeapon);
            Console.WriteLine($"  Client: weapon parent = {clientWeaponParent}");
        }

        // --- Cleanup ---
        Console.WriteLine("\n[8] Cleanup\n");

        serverWorld.UninstallPlugin("NetworkServer");
        clientWorld.UninstallPlugin("NetworkClient");
    }
    finally
    {
        serverTransport.Dispose();
        clientTransport.Dispose();
    }

    PrintSummary();
}

// =============================================================================
// SERVER DEMO - Standalone server for TCP/UDP
// =============================================================================
async Task RunServerDemo(TransportOptions opts)
{
    Console.WriteLine($"Mode: {opts.Transport} Server on port {opts.Port}\n");

    var serializer = new GameSerializer();
    var transport = opts.CreateTransport();

    try
    {
        using var world = new World();
        var config = new ServerNetworkConfig
        {
            TickRate = 60,
            MaxClients = 4,
            Serializer = serializer,
        };

        var serverPlugin = new NetworkServerPlugin(transport, config);
        world.InstallPlugin(serverPlugin);

        var clientEntities = new Dictionary<int, Entity>();

        serverPlugin.ClientInputReceived += (clientId, tick, inputData) =>
        {
            Console.WriteLine($"  Received input from client {clientId} at tick {tick}");
        };

        transport.ClientConnected += clientId =>
        {
            Console.WriteLine($"\n  Client {clientId} connected!");

            // Create player entity for this client
            var player = world.Spawn()
                .WithPosition(x: 100 + clientId * 50, y: 100)
                .WithVelocity(x: 0, y: 0)
                .Build();
            var netId = serverPlugin.RegisterNetworkedEntity(player, ownerId: clientId);
            clientEntities[clientId] = player;

            Console.WriteLine($"  Created player for client {clientId}: Entity={player}, NetworkId={netId.Value}");

            // Send full snapshot to new client
            serverPlugin.SendFullSnapshot(clientId);
            Console.WriteLine($"  Sent full snapshot to client {clientId}");
        };

        transport.ClientDisconnected += clientId =>
        {
            Console.WriteLine($"\n  Client {clientId} disconnected");

            if (clientEntities.TryGetValue(clientId, out var entity))
            {
                world.Despawn(entity);
                clientEntities.Remove(clientId);
                Console.WriteLine($"  Removed player entity for client {clientId}");
            }
        };

        // Create NPC
        var npc = world.Spawn()
            .WithPosition(x: 200, y: 200)
            .WithVelocity(x: 1, y: 0)
            .Build();
        serverPlugin.RegisterNetworkedEntity(npc, ownerId: 0);
        Console.WriteLine($"Created NPC: {npc}");

        // Start listening
        await transport.ListenAsync(opts.Port);
        Console.WriteLine($"\nServer listening on port {opts.Port}");
        Console.WriteLine("Waiting for clients... (Press Ctrl+C to stop)\n");

        // Game loop
        const float deltaTime = 1f / 60f;
        var lastTime = DateTime.UtcNow;
        var running = true;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            running = false;
            Console.WriteLine("\nShutting down server...");
        };

        while (running)
        {
            var now = DateTime.UtcNow;
            var elapsed = (float)(now - lastTime).TotalSeconds;

            if (elapsed >= deltaTime)
            {
                lastTime = now;

                // Move NPC
                ref var npcPos = ref world.Get<Position>(npc);
                ref readonly var npcVel = ref world.Get<Velocity>(npc);
                npcPos.X += npcVel.X;

                // Bounce NPC
                if (npcPos.X > 300 || npcPos.X < 100)
                {
                    ref var vel = ref world.Get<Velocity>(npc);
                    vel.X = -vel.X;
                }

                if (serverPlugin.Tick(deltaTime))
                {
                    world.Update(deltaTime);
                }

                transport.Update();
            }

            await Task.Delay(1);
        }

        world.UninstallPlugin("NetworkServer");
    }
    finally
    {
        transport.Dispose();
    }

    Console.WriteLine("Server stopped.");
}

// =============================================================================
// CLIENT DEMO - Standalone client for TCP/UDP
// =============================================================================
async Task RunClientDemo(TransportOptions opts)
{
    Console.WriteLine($"Mode: {opts.Transport} Client connecting to {opts.Address}:{opts.Port}\n");

    var serializer = new GameSerializer();
    var transport = opts.CreateTransport();

    try
    {
        using var world = new World();

        // Pre-register components
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        var config = new ClientNetworkConfig
        {
            ServerAddress = opts.Address,
            ServerPort = opts.Port,
            EnablePrediction = true,
            Serializer = serializer,
        };

        var clientPlugin = new NetworkClientPlugin(transport, config);
        world.InstallPlugin(clientPlugin);

        var connected = false;

        clientPlugin.Connected += () =>
        {
            connected = true;
            Console.WriteLine($"  Connected! Client ID: {clientPlugin.LocalClientId}");
        };

        clientPlugin.Disconnected += () =>
        {
            connected = false;
            Console.WriteLine("  Disconnected from server");
        };

        // Connect to server
        Console.WriteLine($"Connecting to {opts.Address}:{opts.Port}...");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await transport.ConnectAsync(opts.Address, opts.Port, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Connection timed out.");
            return;
        }

        transport.Update();

        if (!connected)
        {
            Console.WriteLine("Failed to connect.");
            return;
        }

        Console.WriteLine("\nConnected to server! (Press Ctrl+C to disconnect)\n");

        // Game loop
        const float deltaTime = 1f / 60f;
        var lastTime = DateTime.UtcNow;
        var lastPrintTime = DateTime.UtcNow;
        var running = true;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            running = false;
            Console.WriteLine("\nDisconnecting...");
        };

        while (running && connected)
        {
            var now = DateTime.UtcNow;
            var elapsed = (float)(now - lastTime).TotalSeconds;

            if (elapsed >= deltaTime)
            {
                lastTime = now;
                world.Update(deltaTime);
                transport.Update();

                // Print entity status periodically
                if ((now - lastPrintTime).TotalSeconds >= 1.0)
                {
                    lastPrintTime = now;
                    var entityCount = 0;

                    foreach (var entity in world.Query<Position>())
                    {
                        entityCount++;
                        ref readonly var pos = ref world.Get<Position>(entity);
                        var owner = world.Has<KeenEyes.Network.Components.NetworkOwner>(entity)
                            ? world.Get<KeenEyes.Network.Components.NetworkOwner>(entity).ClientId
                            : -1;
                        var isLocal = owner == clientPlugin.LocalClientId;
                        Console.WriteLine($"  {entity}: {pos} ({(isLocal ? "LOCAL" : "REMOTE")})");
                    }

                    if (entityCount == 0)
                    {
                        Console.WriteLine("  (waiting for entities from server...)");
                    }
                }
            }

            await Task.Delay(1);
        }

        transport.Disconnect();
        world.UninstallPlugin("NetworkClient");
    }
    finally
    {
        transport.Dispose();
    }

    Console.WriteLine("Client stopped.");
}

// =============================================================================
// Summary
// =============================================================================
void PrintSummary()
{
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
    Console.WriteLine("\nTransport Options:");
    Console.WriteLine("  - Local:  In-process testing (this demo)");
    Console.WriteLine("  - TCP:    dotnet run -- -t tcp -s  (server)");
    Console.WriteLine("            dotnet run -- -t tcp -c  (client)");
    Console.WriteLine("  - UDP:    dotnet run -- -t udp -s  (server)");
    Console.WriteLine("            dotnet run -- -t udp -c  (client)");
}
