namespace KeenEyes.Graphics;

/// <summary>
/// Component that marks an entity as renderable with mesh and material references.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MeshId"/> and <see cref="MaterialId"/> are handles to resources
/// managed by <see cref="GraphicsContext"/>. Use the graphics API to create meshes
/// and materials before assigning their IDs to this component.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var meshId = graphics.CreateMesh(vertices, indices);
/// var materialId = graphics.CreateMaterial(shaderId, textureId);
///
/// world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new Renderable { MeshId = meshId, MaterialId = materialId })
///     .Build();
/// </code>
/// </example>
/// <param name="meshId">The mesh resource handle.</param>
/// <param name="materialId">The material resource handle.</param>
public struct Renderable(int meshId, int materialId) : IComponent
{
    /// <summary>
    /// The handle to the mesh resource.
    /// </summary>
    public int MeshId = meshId;

    /// <summary>
    /// The handle to the material resource.
    /// </summary>
    public int MaterialId = materialId;

    /// <summary>
    /// The render layer for sorting and culling.
    /// Lower values render first (background), higher values render last (foreground).
    /// </summary>
    public int Layer = 0;

    /// <summary>
    /// Whether this renderable casts shadows.
    /// </summary>
    public bool CastShadows = true;

    /// <summary>
    /// Whether this renderable receives shadows.
    /// </summary>
    public bool ReceiveShadows = true;
}
