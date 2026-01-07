using System.Diagnostics;
using System.Text;
using KeenEyes;
using KeenEyes.Sample.Voxel;

// =============================================================================
// KEEN EYES ECS - Voxel World Demo
// =============================================================================
// A procedural voxel world demonstrating:
// - Chunk-based terrain generation with biomes
// - Simplex noise for natural-looking terrain
// - Player movement with voxel collision
// - ASCII side-view visualization
// =============================================================================

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

// Configuration
const int ViewWidth = 80;
const int ViewHeight = 24;
const float TargetFps = 30f;
const float FixedDeltaTime = 1f / 60f;
const int WorldSeed = 12345;

// Create world and services
using var world = new World();
var blockRegistry = new BlockRegistry();
var worldGenerator = new WorldGenerator(WorldSeed);

// Create systems
var chunkLoader = new ChunkLoaderSystem
{
    Generator = worldGenerator,
    MaxChunksPerFrame = 8
};

var inputSystem = new PlayerInputSystem
{
    MoveSpeed = 6f,
    JumpVelocity = 8f
};

var physicsSystem = new VoxelPhysicsSystem
{
    ChunkLoader = chunkLoader,
    Blocks = blockRegistry,
    Gravity = 20f
};

var visibilitySystem = new ChunkVisibilitySystem();

// Register systems
world
    .AddSystem(chunkLoader)
    .AddSystem(inputSystem)
    .AddSystem(physicsSystem)
    .AddSystem(visibilitySystem);

// Calculate spawn position - find ground level at origin
int spawnX = 0;
int spawnZ = 0;
int spawnY = 64; // Start high and let physics bring us down

// Create player
var player = world.Spawn()
    .WithPosition3D(x: spawnX + 0.5f, y: spawnY, z: spawnZ + 0.5f)
    .WithVelocity3D(x: 0, y: 0, z: 0)
    .WithVoxelCollider(width: 0.6f, height: 1.8f, depth: 0.6f, onGround: false)
    .WithCameraRotation(yaw: 0, pitch: 0)
    .WithViewDistance(horizontal: 3, vertical: 2)
    .WithLocalPlayer()
    .Build();

// Frame buffer for rendering
var frameBuffer = new char[ViewHeight, ViewWidth];
var colorBuffer = new ConsoleColor[ViewHeight, ViewWidth];

// Game state
float gameTime = 0;
var inputState = new PlayerInputState();

// Clear screen and show header
Console.Clear();
Console.WriteLine("KeenEyes ECS - Voxel World Demo");
Console.WriteLine(new string('=', ViewWidth));
Console.WriteLine("WASD = Move | Q/E = Turn | Space = Jump | ESC = Exit");
Console.WriteLine(new string('-', ViewWidth));

const int HeaderLines = 4;
int statsLine = HeaderLines + ViewHeight + 1;

// Game loop
var stopwatch = Stopwatch.StartNew();
var lastFrameTime = stopwatch.Elapsed.TotalSeconds;
var accumulator = 0.0;
var running = true;

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    running = false;
};

while (running)
{
    var currentTime = stopwatch.Elapsed.TotalSeconds;
    var frameTime = currentTime - lastFrameTime;
    lastFrameTime = currentTime;

    // Cap frame time
    if (frameTime > 0.25)
    {
        frameTime = 0.25;
    }

    accumulator += frameTime;

    // Read input
    ReadInput();

    if (inputState is { Forward: false, Backward: false, Left: false, Right: false, Jump: false, TurnLeft: false, TurnRight: false }
        && Console.KeyAvailable)
    {
        // Check for key without blocking
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Escape)
        {
            running = false;
            continue;
        }
    }

    inputSystem.Input = inputState;

    // Fixed timestep updates
    while (accumulator >= FixedDeltaTime)
    {
        world.Update(FixedDeltaTime);
        gameTime += FixedDeltaTime;
        accumulator -= FixedDeltaTime;
    }

    // Render
    RenderFrame();

    // Sleep to cap frame rate
    var elapsed = stopwatch.Elapsed.TotalSeconds - currentTime;
    var sleepTime = (1.0 / TargetFps) - elapsed;
    if (sleepTime > 0)
    {
        Thread.Sleep((int)(sleepTime * 1000));
    }
}

Console.CursorVisible = true;
Console.SetCursorPosition(0, statsLine + 2);
Console.ResetColor();
Console.WriteLine("\nExiting voxel world...");

// =============================================================================
// INPUT HANDLING
// =============================================================================

void ReadInput()
{
    inputState = new PlayerInputState();

    while (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                inputState.Forward = true;
                break;
            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                inputState.Backward = true;
                break;
            case ConsoleKey.A:
                inputState.Left = true;
                break;
            case ConsoleKey.D:
                inputState.Right = true;
                break;
            case ConsoleKey.Q:
            case ConsoleKey.LeftArrow:
                inputState.TurnLeft = true;
                break;
            case ConsoleKey.E:
            case ConsoleKey.RightArrow:
                inputState.TurnRight = true;
                break;
            case ConsoleKey.Spacebar:
                inputState.Jump = true;
                break;
            case ConsoleKey.Escape:
                running = false;
                break;
        }
    }
}

// =============================================================================
// RENDERING - Side View (XY plane looking down Z axis)
// =============================================================================

void RenderFrame()
{
    // Clear buffers
    for (int y = 0; y < ViewHeight; y++)
    {
        for (int x = 0; x < ViewWidth; x++)
        {
            frameBuffer[y, x] = ' ';
            colorBuffer[y, x] = ConsoleColor.DarkBlue; // Sky color
        }
    }

    // Get player position
    ref readonly var playerPos = ref world.Get<Position3D>(player);
    ref readonly var playerRot = ref world.Get<CameraRotation>(player);

    int centerWorldX = (int)MathF.Floor(playerPos.X);
    int centerWorldY = (int)MathF.Floor(playerPos.Y);
    int centerWorldZ = (int)MathF.Floor(playerPos.Z);

    // Render side view (XY plane at player's Z)
    int halfWidth = ViewWidth / 2;
    int halfHeight = ViewHeight / 2;

    for (int screenY = 0; screenY < ViewHeight; screenY++)
    {
        for (int screenX = 0; screenX < ViewWidth; screenX++)
        {
            // Map screen to world coordinates
            int worldX = centerWorldX + (screenX - halfWidth);
            int worldY = centerWorldY + (halfHeight - screenY); // Flip Y

            // Sample blocks at multiple Z depths for depth effect
            byte blockId = BlockId.Air;
            for (int dz = 0; dz <= 3; dz++)
            {
                int sampleZ = centerWorldZ + dz;
                byte sampled = chunkLoader.GetBlock(worldX, worldY, sampleZ);

                if (sampled != BlockId.Air)
                {
                    blockId = sampled;
                    break;
                }
            }

            // Get block visual
            if (blockId != BlockId.Air)
            {
                frameBuffer[screenY, screenX] = blockRegistry.GetSymbol(blockId);
                colorBuffer[screenY, screenX] = blockRegistry.GetColor(blockId);
            }
            else
            {
                // Sky gradient based on height
                int worldHeight = centerWorldY + (halfHeight - screenY);
                if (worldHeight < 32) // Below sea level
                {
                    frameBuffer[screenY, screenX] = '~';
                    colorBuffer[screenY, screenX] = ConsoleColor.DarkBlue;
                }
                else if (worldHeight < 50)
                {
                    colorBuffer[screenY, screenX] = ConsoleColor.Blue;
                }
                else
                {
                    colorBuffer[screenY, screenX] = ConsoleColor.Cyan;
                }
            }
        }
    }

    // Draw player marker at center
    int playerScreenX = halfWidth;
    int playerScreenY = halfHeight;
    if (playerScreenX >= 0 && playerScreenX < ViewWidth &&
        playerScreenY >= 0 && playerScreenY < ViewHeight)
    {
        frameBuffer[playerScreenY, playerScreenX] = '@';
        colorBuffer[playerScreenY, playerScreenX] = ConsoleColor.Yellow;

        // Draw player body (2 blocks tall)
        if (playerScreenY + 1 < ViewHeight)
        {
            frameBuffer[playerScreenY + 1, playerScreenX] = 'O';
            colorBuffer[playerScreenY + 1, playerScreenX] = ConsoleColor.Yellow;
        }
    }

    // Draw direction indicator
    float yaw = playerRot.Yaw;
    char dirChar;
    if (yaw >= -0.4f && yaw <= 0.4f)
    {
        dirChar = '^';
    }
    else if (yaw > 0.4f && yaw < 2.7f)
    {
        dirChar = '>';
    }
    else if (yaw >= 2.7f || yaw <= -2.7f)
    {
        dirChar = 'v';
    }
    else
    {
        dirChar = '<';
    }

    if (playerScreenY > 0)
    {
        frameBuffer[playerScreenY - 1, playerScreenX] = dirChar;
        colorBuffer[playerScreenY - 1, playerScreenX] = ConsoleColor.White;
    }

    // Draw frame to console
    for (int y = 0; y < ViewHeight; y++)
    {
        Console.SetCursorPosition(0, HeaderLines + y);
        ConsoleColor lastColor = ConsoleColor.Black;

        for (int x = 0; x < ViewWidth; x++)
        {
            var color = colorBuffer[y, x];
            if (color != lastColor)
            {
                Console.ForegroundColor = color;
                lastColor = color;
            }
            Console.Write(frameBuffer[y, x]);
        }
    }

    // Draw stats
    Console.SetCursorPosition(0, statsLine);
    Console.ResetColor();

    // Get current biome
    var biome = worldGenerator.DetermineBiome(centerWorldX, centerWorldZ);
    ref readonly var collider = ref world.Get<VoxelCollider>(player);

    var groundStatus = collider.OnGround ? "Ground" : "Air   ";

    Console.Write($"Pos: ({playerPos.X,6:F1}, {playerPos.Y,5:F1}, {playerPos.Z,6:F1}) | ");
    Console.Write($"Biome: {biome,-10} | ");
    Console.Write($"Chunks: {chunkLoader.LoadedChunkCount,3} | ");
    Console.Write($"Status: {groundStatus} | ");
    Console.Write($"Time: {gameTime,6:F1}s   ");
}
