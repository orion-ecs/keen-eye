using KeenEyes;

namespace KeenEyes.Sample.Voxel;

// =============================================================================
// VOXEL GAME SYSTEMS
// =============================================================================

/// <summary>
/// Manages chunk loading and unloading based on player position.
/// </summary>
[System(Phase = SystemPhase.EarlyUpdate, Order = 0)]
public partial class ChunkLoaderSystem : SystemBase
{
    private readonly Dictionary<(int, int, int), Entity> loadedChunks = [];
    private readonly HashSet<(int, int, int)> chunksToLoad = [];
    private readonly HashSet<(int, int, int)> chunksToUnload = [];

    /// <summary>Reference to the world generator.</summary>
    public WorldGenerator Generator { get; init; } = null!;

    /// <summary>Maximum chunks to load per frame.</summary>
    public int MaxChunksPerFrame { get; set; } = 4;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Find player and their view distance
        foreach (var entity in World.Query<Position3D, ViewDistance>().With<LocalPlayer>())
        {
            ref readonly var pos = ref World.Get<Position3D>(entity);
            ref readonly var view = ref World.Get<ViewDistance>(entity);

            int playerChunkX = (int)MathF.Floor(pos.X / VoxelData.Size);
            int playerChunkY = (int)MathF.Floor(pos.Y / VoxelData.Size);
            int playerChunkZ = (int)MathF.Floor(pos.Z / VoxelData.Size);

            // Determine chunks that should be loaded
            chunksToLoad.Clear();
            for (int dy = -view.Vertical; dy <= view.Vertical; dy++)
            {
                for (int dz = -view.Horizontal; dz <= view.Horizontal; dz++)
                {
                    for (int dx = -view.Horizontal; dx <= view.Horizontal; dx++)
                    {
                        var coord = (playerChunkX + dx, playerChunkY + dy, playerChunkZ + dz);
                        if (!loadedChunks.ContainsKey(coord))
                        {
                            chunksToLoad.Add(coord);
                        }
                    }
                }
            }

            // Determine chunks to unload
            chunksToUnload.Clear();
            foreach (var coord in loadedChunks.Keys)
            {
                int dx = Math.Abs(coord.Item1 - playerChunkX);
                int dy = Math.Abs(coord.Item2 - playerChunkY);
                int dz = Math.Abs(coord.Item3 - playerChunkZ);

                if (dx > view.Horizontal + 1 || dy > view.Vertical + 1 || dz > view.Horizontal + 1)
                {
                    chunksToUnload.Add(coord);
                }
            }

            // Unload distant chunks
            foreach (var coord in chunksToUnload)
            {
                if (loadedChunks.TryGetValue(coord, out var chunkEntity))
                {
                    World.Despawn(chunkEntity);
                    loadedChunks.Remove(coord);
                }
            }

            // Load new chunks (limited per frame)
            int loaded = 0;
            foreach (var coord in chunksToLoad)
            {
                if (loaded >= MaxChunksPerFrame)
                {
                    break;
                }

                LoadChunk(coord.Item1, coord.Item2, coord.Item3);
                loaded++;
            }
        }
    }

    private void LoadChunk(int cx, int cy, int cz)
    {
        var voxelData = Generator.GenerateChunk(cx, cy, cz, out var biome, out var heightMap);

        var chunk = World.Spawn()
            .WithChunkCoord(x: cx, y: cy, z: cz)
            .With(voxelData)
            .WithChunkBiome(biome: biome)
            .With(heightMap)
            .WithChunkLoaded()
            .WithChunkDirty()
            .Build();

        loadedChunks[(cx, cy, cz)] = chunk;
    }

    /// <summary>
    /// Gets a loaded chunk entity by coordinates.
    /// </summary>
    public Entity? GetChunk(int cx, int cy, int cz)
    {
        return loadedChunks.TryGetValue((cx, cy, cz), out var entity) ? entity : null;
    }

    /// <summary>
    /// Gets the block at world coordinates.
    /// </summary>
    public byte GetBlock(int worldX, int worldY, int worldZ)
    {
        int cx = (int)MathF.Floor(worldX / (float)VoxelData.Size);
        int cy = (int)MathF.Floor(worldY / (float)VoxelData.Size);
        int cz = (int)MathF.Floor(worldZ / (float)VoxelData.Size);

        if (!loadedChunks.TryGetValue((cx, cy, cz), out var chunk))
        {
            return BlockId.Air;
        }

        if (!World.IsAlive(chunk))
        {
            return BlockId.Air;
        }

        ref readonly var voxels = ref World.Get<VoxelData>(chunk);

        int lx = ((worldX % VoxelData.Size) + VoxelData.Size) % VoxelData.Size;
        int ly = ((worldY % VoxelData.Size) + VoxelData.Size) % VoxelData.Size;
        int lz = ((worldZ % VoxelData.Size) + VoxelData.Size) % VoxelData.Size;

        return VoxelDataHelper.GetBlock(in voxels, lx, ly, lz);
    }

    /// <summary>
    /// Gets the number of loaded chunks.
    /// </summary>
    public int LoadedChunkCount => loadedChunks.Count;
}

/// <summary>
/// Handles player movement input and applies velocity.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 0)]
public partial class PlayerInputSystem : SystemBase
{
    /// <summary>Movement speed in blocks per second.</summary>
    public float MoveSpeed { get; set; } = 8f;

    /// <summary>Jump velocity.</summary>
    public float JumpVelocity { get; set; } = 10f;

    /// <summary>Current input state.</summary>
    public PlayerInputState Input { get; set; }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position3D, Velocity3D, VoxelCollider, CameraRotation>().With<LocalPlayer>())
        {
            ref var velocity = ref World.Get<Velocity3D>(entity);
            ref readonly var collider = ref World.Get<VoxelCollider>(entity);
            ref readonly var rotation = ref World.Get<CameraRotation>(entity);

            // Calculate movement direction based on camera yaw
            float cos = MathF.Cos(rotation.Yaw);
            float sin = MathF.Sin(rotation.Yaw);

            float moveX = 0;
            float moveZ = 0;

            if (Input.Forward)
            {
                moveX += sin;
                moveZ += cos;
            }

            if (Input.Backward)
            {
                moveX -= sin;
                moveZ -= cos;
            }

            if (Input.Left)
            {
                moveX += cos;
                moveZ -= sin;
            }

            if (Input.Right)
            {
                moveX -= cos;
                moveZ += sin;
            }

            // Normalize diagonal movement
            float length = MathF.Sqrt(moveX * moveX + moveZ * moveZ);
            if (length > 0.01f)
            {
                moveX /= length;
                moveZ /= length;
            }

            // Apply horizontal velocity
            velocity.X = moveX * MoveSpeed;
            velocity.Z = moveZ * MoveSpeed;

            // Jump if on ground
            if (Input.Jump && collider.OnGround)
            {
                velocity.Y = JumpVelocity;
            }

            // Camera rotation (simplified for demo)
            if (Input.TurnLeft || Input.TurnRight)
            {
                ref var rot = ref World.Get<CameraRotation>(entity);
                rot.Yaw += (Input.TurnLeft ? -1 : 1) * 2f * deltaTime;
            }
        }
    }
}

/// <summary>
/// Input state for player control.
/// </summary>
public struct PlayerInputState
{
    /// <summary>Moving forward (W key).</summary>
    public bool Forward;

    /// <summary>Moving backward (S key).</summary>
    public bool Backward;

    /// <summary>Strafing left (A key).</summary>
    public bool Left;

    /// <summary>Strafing right (D key).</summary>
    public bool Right;

    /// <summary>Jumping (Space key).</summary>
    public bool Jump;

    /// <summary>Turning left (Q key).</summary>
    public bool TurnLeft;

    /// <summary>Turning right (E key).</summary>
    public bool TurnRight;
}

/// <summary>
/// Applies gravity and handles voxel collision.
/// </summary>
[System(Phase = SystemPhase.FixedUpdate, Order = 0)]
public partial class VoxelPhysicsSystem : SystemBase
{
    /// <summary>Reference to chunk loader for block queries.</summary>
    public ChunkLoaderSystem ChunkLoader { get; init; } = null!;

    /// <summary>Reference to block registry.</summary>
    public BlockRegistry Blocks { get; init; } = null!;

    /// <summary>Gravity acceleration.</summary>
    public float Gravity { get; set; } = 25f;

    /// <summary>Maximum fall speed.</summary>
    public float TerminalVelocity { get; set; } = 50f;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position3D, Velocity3D, VoxelCollider>())
        {
            ref var pos = ref World.Get<Position3D>(entity);
            ref var vel = ref World.Get<Velocity3D>(entity);
            ref var collider = ref World.Get<VoxelCollider>(entity);

            // Apply gravity
            vel.Y -= Gravity * deltaTime;
            vel.Y = MathF.Max(vel.Y, -TerminalVelocity);

            // Calculate movement
            float dx = vel.X * deltaTime;
            float dy = vel.Y * deltaTime;
            float dz = vel.Z * deltaTime;

            // Sweep collision (simplified axis-aligned)
            collider.OnGround = false;

            // X axis
            if (MathF.Abs(dx) > 0.001f && CheckCollision(pos.X + dx, pos.Y, pos.Z, collider))
            {
                dx = 0;
                vel.X = 0;
            }

            // Z axis
            if (MathF.Abs(dz) > 0.001f && CheckCollision(pos.X + dx, pos.Y, pos.Z + dz, collider))
            {
                dz = 0;
                vel.Z = 0;
            }

            // Y axis
            if (MathF.Abs(dy) > 0.001f && CheckCollision(pos.X + dx, pos.Y + dy, pos.Z + dz, collider))
            {
                if (dy < 0)
                {
                    collider.OnGround = true;
                }

                dy = 0;
                vel.Y = 0;
            }

            // Apply movement
            pos.X += dx;
            pos.Y += dy;
            pos.Z += dz;
        }
    }

    private bool CheckCollision(float x, float y, float z, VoxelCollider collider)
    {
        float halfWidth = collider.Width * 0.5f;
        float halfDepth = collider.Depth * 0.5f;

        // Check all blocks the AABB overlaps
        int minX = (int)MathF.Floor(x - halfWidth);
        int maxX = (int)MathF.Floor(x + halfWidth);
        int minY = (int)MathF.Floor(y);
        int maxY = (int)MathF.Floor(y + collider.Height);
        int minZ = (int)MathF.Floor(z - halfDepth);
        int maxZ = (int)MathF.Floor(z + halfDepth);

        for (int by = minY; by <= maxY; by++)
        {
            for (int bz = minZ; bz <= maxZ; bz++)
            {
                for (int bx = minX; bx <= maxX; bx++)
                {
                    byte block = ChunkLoader.GetBlock(bx, by, bz);
                    if (Blocks.IsSolid(block))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

/// <summary>
/// Updates visible chunk list based on player position.
/// </summary>
[System(Phase = SystemPhase.LateUpdate, Order = 0)]
public partial class ChunkVisibilitySystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Find player position
        float playerX = 0, playerY = 0, playerZ = 0;
        int viewHorizontal = 4, viewVertical = 2;

        foreach (var entity in World.Query<Position3D, ViewDistance>().With<LocalPlayer>())
        {
            ref readonly var pos = ref World.Get<Position3D>(entity);
            ref readonly var view = ref World.Get<ViewDistance>(entity);
            playerX = pos.X;
            playerY = pos.Y;
            playerZ = pos.Z;
            viewHorizontal = view.Horizontal;
            viewVertical = view.Vertical;
            break;
        }

        int playerChunkX = (int)MathF.Floor(playerX / VoxelData.Size);
        int playerChunkY = (int)MathF.Floor(playerY / VoxelData.Size);
        int playerChunkZ = (int)MathF.Floor(playerZ / VoxelData.Size);

        // Update visibility for all chunks
        foreach (var entity in World.Query<ChunkCoord>().With<ChunkLoaded>())
        {
            ref readonly var coord = ref World.Get<ChunkCoord>(entity);

            int dx = Math.Abs(coord.X - playerChunkX);
            int dy = Math.Abs(coord.Y - playerChunkY);
            int dz = Math.Abs(coord.Z - playerChunkZ);

            bool shouldBeVisible = dx <= viewHorizontal && dy <= viewVertical && dz <= viewHorizontal;

            bool isVisible = World.Has<ChunkVisible>(entity);

            if (shouldBeVisible && !isVisible)
            {
                World.Add(entity, default(ChunkVisible));
            }
            else if (!shouldBeVisible && isVisible)
            {
                World.Remove<ChunkVisible>(entity);
            }
        }
    }
}
