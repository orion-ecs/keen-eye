using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Component that marks an entity for GPU instanced rendering.
/// </summary>
/// <remarks>
/// <para>
/// Entities with this component are grouped by their <see cref="BatchId"/> and rendered
/// together using a single instanced draw call. This dramatically reduces CPU overhead
/// when rendering many similar objects (trees, rocks, particles, etc.).
/// </para>
/// <para>
/// All entities in the same batch must share the same mesh and material. The batching
/// system automatically handles grouping and instance buffer management.
/// </para>
/// <para>
/// The <see cref="ColorTint"/> is multiplied with the base material color to provide
/// per-instance color variation without requiring separate materials.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create 1000 trees in one batch - rendered with a single draw call
/// for (int i = 0; i &lt; 1000; i++)
/// {
///     world.Spawn()
///         .With(new Transform3D(positions[i], Quaternion.Identity, Vector3.One))
///         .With(new Renderable(treeMeshId, treeMaterialId))
///         .With(new InstanceBatch(batchId: 1))
///         .Build();
/// }
/// </code>
/// </example>
/// <param name="batchId">The batch identifier for grouping instances.</param>
/// <param name="colorTint">The color tint multiplier.</param>
public struct InstanceBatch(int batchId, Vector4 colorTint) : IComponent
{
    /// <summary>
    /// The batch identifier used to group entities for instanced rendering.
    /// </summary>
    /// <remarks>
    /// Entities with the same BatchId, mesh, and material will be rendered together
    /// in a single instanced draw call. Use different BatchIds to create separate
    /// batches for different logical groups of entities.
    /// </remarks>
    public int BatchId = batchId;

    /// <summary>
    /// The color tint multiplier applied to this instance.
    /// </summary>
    /// <remarks>
    /// This color is multiplied with the base material color to provide per-instance
    /// color variation. A value of (1, 1, 1, 1) represents no tint (original color).
    /// </remarks>
    public Vector4 ColorTint = colorTint;

    /// <summary>
    /// Creates a new instance batch component with the specified batch ID and no color tint.
    /// </summary>
    /// <param name="batchId">The batch identifier for grouping instances.</param>
    public InstanceBatch(int batchId) : this(batchId, Vector4.One)
    {
    }

    /// <summary>
    /// Creates an instance batch with no color tint.
    /// </summary>
    /// <param name="batchId">The batch identifier.</param>
    /// <returns>A new instance batch component.</returns>
    public static InstanceBatch WithBatch(int batchId) => new(batchId);

    /// <summary>
    /// Creates an instance batch with a color tint.
    /// </summary>
    /// <param name="batchId">The batch identifier.</param>
    /// <param name="colorTint">The color tint multiplier.</param>
    /// <returns>A new instance batch component.</returns>
    public static InstanceBatch WithTint(int batchId, Vector4 colorTint) => new(batchId, colorTint);
}
