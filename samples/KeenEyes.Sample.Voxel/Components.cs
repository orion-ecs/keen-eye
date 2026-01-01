using KeenEyes;

namespace KeenEyes.Sample.Voxel;

// =============================================================================
// VOXEL GAME COMPONENTS
// =============================================================================
// Components for a voxel-based game demonstrating:
// - Chunk-based world storage
// - Block types and biomes
// - Player movement with voxel collision
// - ASCII visualization of voxel terrain
// =============================================================================

// =============================================================================
// CHUNK COMPONENTS
// =============================================================================

/// <summary>
/// Chunk position in chunk coordinates (not world coordinates).
/// </summary>
[Component]
public partial struct ChunkCoord
{
    /// <summary>Chunk X coordinate.</summary>
    public int X;

    /// <summary>Chunk Y coordinate (vertical).</summary>
    public int Y;

    /// <summary>Chunk Z coordinate.</summary>
    public int Z;
}

/// <summary>
/// Voxel data storage for a chunk (pure data, no logic).
/// Uses IComponent directly without generator to avoid array complexity.
/// </summary>
/// <remarks>
/// This is a pure data component following ECS principles.
/// Use <see cref="VoxelDataHelper"/> for block access operations.
/// </remarks>
public struct VoxelData : IComponent
{
    /// <summary>Chunk size in blocks per axis.</summary>
    public const int Size = 16;

    /// <summary>Total blocks per chunk.</summary>
    public const int Volume = Size * Size * Size;

    /// <summary>Block type IDs (0 = air).</summary>
    public byte[] Blocks;
}

/// <summary>
/// Biome identifier for a chunk.
/// </summary>
[Component]
public partial struct ChunkBiome
{
    /// <summary>Biome type for this chunk.</summary>
    public BiomeType Biome;
}

/// <summary>
/// Height map cache for faster terrain queries (pure data, no logic).
/// Uses IComponent directly without generator to avoid array complexity.
/// </summary>
/// <remarks>
/// This is a pure data component following ECS principles.
/// Use <see cref="VoxelDataHelper"/> for height access operations.
/// </remarks>
public struct HeightMap : IComponent
{
    /// <summary>Cached height values for each XZ column.</summary>
    public byte[] Heights;
}

// =============================================================================
// VOXEL DATA HELPER (STATIC UTILITY CLASS)
// =============================================================================

/// <summary>
/// Static utility methods for voxel data operations.
/// </summary>
/// <remarks>
/// Following ECS principles, components are pure data.
/// Logic for manipulating voxel data is provided through this helper class.
/// </remarks>
public static class VoxelDataHelper
{
    /// <summary>
    /// Gets the block at local coordinates.
    /// </summary>
    /// <param name="data">The voxel data component.</param>
    /// <param name="x">Local X coordinate (0 to Size-1).</param>
    /// <param name="y">Local Y coordinate (0 to Size-1).</param>
    /// <param name="z">Local Z coordinate (0 to Size-1).</param>
    /// <returns>The block ID at the specified coordinates, or 0 if out of bounds.</returns>
    public static byte GetBlock(ref readonly VoxelData data, int x, int y, int z)
    {
        if (x < 0 || x >= VoxelData.Size || y < 0 || y >= VoxelData.Size || z < 0 || z >= VoxelData.Size)
        {
            return 0;
        }

        return data.Blocks[x + y * VoxelData.Size + z * VoxelData.Size * VoxelData.Size];
    }

    /// <summary>
    /// Sets the block at local coordinates.
    /// </summary>
    /// <param name="data">The voxel data component.</param>
    /// <param name="x">Local X coordinate (0 to Size-1).</param>
    /// <param name="y">Local Y coordinate (0 to Size-1).</param>
    /// <param name="z">Local Z coordinate (0 to Size-1).</param>
    /// <param name="blockId">The block ID to set.</param>
    public static void SetBlock(ref VoxelData data, int x, int y, int z, byte blockId)
    {
        if (x < 0 || x >= VoxelData.Size || y < 0 || y >= VoxelData.Size || z < 0 || z >= VoxelData.Size)
        {
            return;
        }

        data.Blocks[x + y * VoxelData.Size + z * VoxelData.Size * VoxelData.Size] = blockId;
    }

    /// <summary>
    /// Gets the terrain height at local XZ coordinates from a height map.
    /// </summary>
    /// <param name="heightMap">The height map component.</param>
    /// <param name="x">Local X coordinate (0 to Size-1).</param>
    /// <param name="z">Local Z coordinate (0 to Size-1).</param>
    /// <returns>The height at the specified coordinates, or 0 if out of bounds.</returns>
    public static int GetHeight(ref readonly HeightMap heightMap, int x, int z)
    {
        if (x < 0 || x >= VoxelData.Size || z < 0 || z >= VoxelData.Size)
        {
            return 0;
        }

        return heightMap.Heights[x + z * VoxelData.Size];
    }
}

// =============================================================================
// PLAYER COMPONENTS
// =============================================================================

/// <summary>
/// 3D position in world space.
/// </summary>
[Component]
public partial struct Position3D
{
    /// <summary>X coordinate in world units.</summary>
    public float X;

    /// <summary>Y coordinate (vertical) in world units.</summary>
    public float Y;

    /// <summary>Z coordinate in world units.</summary>
    public float Z;
}

/// <summary>
/// 3D velocity for movement.
/// </summary>
[Component]
public partial struct Velocity3D
{
    /// <summary>X velocity component.</summary>
    public float X;

    /// <summary>Y velocity component.</summary>
    public float Y;

    /// <summary>Z velocity component.</summary>
    public float Z;
}

/// <summary>
/// Axis-aligned bounding box for collision.
/// </summary>
[Component]
public partial struct VoxelCollider
{
    /// <summary>Width (X axis).</summary>
    public float Width;

    /// <summary>Height (Y axis).</summary>
    public float Height;

    /// <summary>Depth (Z axis).</summary>
    public float Depth;

    /// <summary>Whether the entity is standing on ground.</summary>
    public bool OnGround;
}

/// <summary>
/// Camera rotation for viewing direction.
/// </summary>
[Component]
public partial struct CameraRotation
{
    /// <summary>Yaw angle in radians (horizontal rotation).</summary>
    public float Yaw;

    /// <summary>Pitch angle in radians (vertical rotation).</summary>
    public float Pitch;
}

/// <summary>
/// View range for chunk loading.
/// </summary>
[Component]
public partial struct ViewDistance
{
    /// <summary>Horizontal view distance in chunks.</summary>
    [DefaultValue(4)]
    public int Horizontal;

    /// <summary>Vertical view distance in chunks.</summary>
    [DefaultValue(2)]
    public int Vertical;
}

// =============================================================================
// TAG COMPONENTS
// =============================================================================

/// <summary>Marks an entity as the local player.</summary>
[TagComponent]
public partial struct LocalPlayer;

/// <summary>Marks a chunk as loaded and ready.</summary>
[TagComponent]
public partial struct ChunkLoaded;

/// <summary>Marks a chunk as needing mesh/display update.</summary>
[TagComponent]
public partial struct ChunkDirty;

/// <summary>Marks a chunk as visible to the player.</summary>
[TagComponent]
public partial struct ChunkVisible;

/// <summary>Marks a chunk as modified (needs saving).</summary>
[TagComponent]
public partial struct ChunkModified;

// =============================================================================
// BIOME TYPES
// =============================================================================

/// <summary>
/// Available biome types.
/// </summary>
public enum BiomeType : byte
{
    /// <summary>Flat grassland with occasional trees.</summary>
    Plains,

    /// <summary>Dense tree coverage.</summary>
    Forest,

    /// <summary>Sandy terrain near water.</summary>
    Desert,

    /// <summary>Elevated rocky terrain.</summary>
    Mountains,

    /// <summary>Cold snowy terrain.</summary>
    Tundra,

    /// <summary>Water body.</summary>
    Ocean
}
