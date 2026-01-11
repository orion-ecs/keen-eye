using KeenEyes.Common;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Specifies how LOD levels are selected.
/// </summary>
public enum LodSelectionMode
{
    /// <summary>
    /// Select LOD based on distance from camera.
    /// </summary>
    Distance,

    /// <summary>
    /// Select LOD based on projected screen size.
    /// </summary>
    ScreenSize
}

/// <summary>
/// Represents a single LOD level with a mesh and distance threshold.
/// </summary>
/// <param name="MeshId">The mesh handle for this LOD level.</param>
/// <param name="Threshold">
/// The distance or screen size threshold for this LOD level.
/// For <see cref="LodSelectionMode.Distance"/>: Distance in world units beyond which this LOD is used.
/// For <see cref="LodSelectionMode.ScreenSize"/>: Screen coverage ratio (0-1) below which this LOD is used.
/// </param>
public readonly record struct LodLevel(int MeshId, float Threshold);

/// <summary>
/// Component that groups multiple mesh LOD levels for automatic switching based on camera distance or screen coverage.
/// </summary>
/// <remarks>
/// <para>
/// The LOD system automatically switches between mesh detail levels based on the entity's distance
/// from the camera or its projected screen size. This improves performance by rendering simpler
/// meshes for distant objects.
/// </para>
/// <para>
/// LOD levels are stored inline (no array allocation) for cache efficiency. Use the factory methods
/// to create LOD groups with the appropriate number of levels.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create meshes at different detail levels
/// var highDetail = graphics.CreateMesh(highVertices, highIndices);
/// var mediumDetail = graphics.CreateMesh(mediumVertices, mediumIndices);
/// var lowDetail = graphics.CreateMesh(lowVertices, lowIndices);
///
/// // Create entity with LOD
/// world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new Renderable(highDetail.Id, materialId))
///     .With(LodGroup.Create(
///         new LodLevel(highDetail.Id, 0f),      // 0-20 units
///         new LodLevel(mediumDetail.Id, 20f),   // 20-50 units
///         new LodLevel(lowDetail.Id, 50f)))     // 50+ units
///     .Build();
/// </code>
/// </example>
public struct LodGroup : IComponent
{
    /// <summary>
    /// The highest detail LOD level (used when closest to camera).
    /// </summary>
    public LodLevel Level0;

    /// <summary>
    /// The second LOD level.
    /// </summary>
    public LodLevel Level1;

    /// <summary>
    /// The third LOD level.
    /// </summary>
    public LodLevel Level2;

    /// <summary>
    /// The lowest detail LOD level (used when farthest from camera).
    /// </summary>
    public LodLevel Level3;

    /// <summary>
    /// The number of valid LOD levels (1-4).
    /// </summary>
    public int LevelCount;

    /// <summary>
    /// How LOD selection is performed.
    /// </summary>
    public LodSelectionMode SelectionMode;

    /// <summary>
    /// The bounding sphere radius for screen-size calculations.
    /// Only used when <see cref="SelectionMode"/> is <see cref="LodSelectionMode.ScreenSize"/>.
    /// </summary>
    public float BoundingSphereRadius;

    /// <summary>
    /// Bias applied to LOD selection. Positive values prefer higher detail, negative values prefer lower detail.
    /// For distance mode: Subtracts from calculated distance.
    /// For screen-size mode: Multiplies the calculated screen size.
    /// </summary>
    public float Bias;

    /// <summary>
    /// The currently selected LOD level index (0 = highest detail).
    /// Updated by the LodSystem.
    /// </summary>
    public int CurrentLevel;

    /// <summary>
    /// Creates a LOD group with a single level (no LOD switching).
    /// </summary>
    /// <param name="level0">The only LOD level.</param>
    /// <returns>A new LOD group.</returns>
    public static LodGroup Create(LodLevel level0)
    {
        return new LodGroup
        {
            Level0 = level0,
            LevelCount = 1,
            SelectionMode = LodSelectionMode.Distance
        };
    }

    /// <summary>
    /// Creates a LOD group with two levels.
    /// </summary>
    /// <param name="level0">The highest detail level.</param>
    /// <param name="level1">The lowest detail level.</param>
    /// <returns>A new LOD group.</returns>
    public static LodGroup Create(LodLevel level0, LodLevel level1)
    {
        return new LodGroup
        {
            Level0 = level0,
            Level1 = level1,
            LevelCount = 2,
            SelectionMode = LodSelectionMode.Distance
        };
    }

    /// <summary>
    /// Creates a LOD group with three levels.
    /// </summary>
    /// <param name="level0">The highest detail level.</param>
    /// <param name="level1">The medium detail level.</param>
    /// <param name="level2">The lowest detail level.</param>
    /// <returns>A new LOD group.</returns>
    public static LodGroup Create(LodLevel level0, LodLevel level1, LodLevel level2)
    {
        return new LodGroup
        {
            Level0 = level0,
            Level1 = level1,
            Level2 = level2,
            LevelCount = 3,
            SelectionMode = LodSelectionMode.Distance
        };
    }

    /// <summary>
    /// Creates a LOD group with four levels.
    /// </summary>
    /// <param name="level0">The highest detail level.</param>
    /// <param name="level1">The high-medium detail level.</param>
    /// <param name="level2">The low-medium detail level.</param>
    /// <param name="level3">The lowest detail level.</param>
    /// <returns>A new LOD group.</returns>
    public static LodGroup Create(LodLevel level0, LodLevel level1, LodLevel level2, LodLevel level3)
    {
        return new LodGroup
        {
            Level0 = level0,
            Level1 = level1,
            Level2 = level2,
            Level3 = level3,
            LevelCount = 4,
            SelectionMode = LodSelectionMode.Distance
        };
    }

    /// <summary>
    /// Gets the LOD level at the specified index.
    /// </summary>
    /// <param name="index">The level index (0-3).</param>
    /// <returns>The LOD level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public readonly LodLevel GetLevel(int index)
    {
        return index switch
        {
            0 => Level0,
            1 => Level1,
            2 => Level2,
            3 => Level3,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, "LOD level index must be 0-3.")
        };
    }

    /// <summary>
    /// Sets the LOD level at the specified index.
    /// </summary>
    /// <param name="index">The level index (0-3).</param>
    /// <param name="level">The LOD level to set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public void SetLevel(int index, LodLevel level)
    {
        switch (index)
        {
            case 0: Level0 = level; break;
            case 1: Level1 = level; break;
            case 2: Level2 = level; break;
            case 3: Level3 = level; break;
            default: throw new ArgumentOutOfRangeException(nameof(index), index, "LOD level index must be 0-3.");
        }
    }
}
