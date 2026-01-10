using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics;

/// <summary>
/// Key for grouping instanced entities into batches.
/// </summary>
/// <param name="MeshId">The mesh resource handle.</param>
/// <param name="MaterialId">The material resource handle.</param>
/// <param name="BatchId">The user-defined batch identifier.</param>
internal readonly record struct BatchKey(int MeshId, int MaterialId, int BatchId);

/// <summary>
/// Represents a prepared batch of instances ready for rendering.
/// </summary>
public sealed class PreparedBatch
{
    /// <summary>
    /// The mesh handle for this batch.
    /// </summary>
    public MeshHandle Mesh { get; init; }

    /// <summary>
    /// The material for this batch (if any entities have materials).
    /// </summary>
    public Material Material { get; set; }

    /// <summary>
    /// Whether this batch has a material component.
    /// </summary>
    public bool HasMaterial { get; set; }

    /// <summary>
    /// The instance buffer handle containing per-instance data.
    /// </summary>
    public InstanceBufferHandle InstanceBuffer { get; set; }

    /// <summary>
    /// The number of instances in this batch.
    /// </summary>
    public int InstanceCount { get; set; }

    /// <summary>
    /// The render layer for sorting.
    /// </summary>
    public int Layer { get; set; }
}

/// <summary>
/// System that prepares instance batches for GPU instanced rendering.
/// </summary>
/// <remarks>
/// <para>
/// The InstanceBatchingSystem queries for entities with <see cref="Transform3D"/>,
/// <see cref="Renderable"/>, and <see cref="InstanceBatch"/> components. It groups
/// them by mesh, material, and batch ID, then uploads the instance data to GPU buffers.
/// </para>
/// <para>
/// This system should run in the PreRender phase, before the <see cref="RenderSystem"/>.
/// The RenderSystem should skip entities with InstanceBatch components and instead
/// render the prepared batches from this system.
/// </para>
/// <para>
/// For optimal performance, instance buffers are reused when possible and only
/// resized when the batch grows beyond capacity.
/// </para>
/// </remarks>
public sealed class InstanceBatchingSystem : ISystem
{
    private const int InitialBufferCapacity = 128;
    private const float BufferGrowthFactor = 1.5f;

    private IWorld? world;
    private IGraphicsContext? graphics;

    // Batch building state
    private readonly Dictionary<BatchKey, List<InstanceData>> batchData = [];
    private readonly Dictionary<BatchKey, PreparedBatch> preparedBatches = [];
    private readonly Dictionary<BatchKey, int> batchLayers = [];
    private readonly Dictionary<BatchKey, Material> batchMaterials = [];
    private readonly Dictionary<BatchKey, bool> batchHasMaterials = [];

    // Reusable array for uploading
    private InstanceData[] uploadBuffer = new InstanceData[InitialBufferCapacity];

    /// <summary>
    /// Gets all prepared batches for rendering.
    /// </summary>
    /// <remarks>
    /// This collection is updated each frame by the system. The RenderSystem
    /// should iterate over these batches and render them using instanced draw calls.
    /// </remarks>
    public IEnumerable<PreparedBatch> Batches => preparedBatches.Values;

    /// <summary>
    /// Gets the number of active batches.
    /// </summary>
    public int BatchCount => preparedBatches.Count;

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;

        if (!world.TryGetExtension<IGraphicsContext>(out graphics))
        {
            throw new InvalidOperationException("InstanceBatchingSystem requires IGraphicsContext extension");
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (world is null || graphics is null || !graphics.IsInitialized)
        {
            return;
        }

        // Clear batch data from previous frame
        foreach (var list in batchData.Values)
        {
            list.Clear();
        }
        batchLayers.Clear();
        batchMaterials.Clear();
        batchHasMaterials.Clear();

        // Collect all instanced entities and group by batch key
        foreach (var entity in world.Query<Transform3D, Renderable, InstanceBatch>())
        {
            ref readonly var transform = ref world.Get<Transform3D>(entity);
            ref readonly var renderable = ref world.Get<Renderable>(entity);
            ref readonly var batch = ref world.Get<InstanceBatch>(entity);

            // Skip if no mesh
            if (renderable.MeshId <= 0)
            {
                continue;
            }

            var key = new BatchKey(renderable.MeshId, renderable.MaterialId, batch.BatchId);

            // Get or create batch data list
            if (!batchData.TryGetValue(key, out var instances))
            {
                instances = [];
                batchData[key] = instances;
            }

            // Create instance data from transform and tint
            var instanceData = InstanceData.FromTransform(transform.Matrix(), batch.ColorTint);
            instances.Add(instanceData);

            // Track layer (use minimum layer in batch for sorting)
            if (!batchLayers.TryGetValue(key, out var existingLayer) || renderable.Layer < existingLayer)
            {
                batchLayers[key] = renderable.Layer;
            }

            // Track material (use first entity's material)
            if (!batchHasMaterials.ContainsKey(key))
            {
                if (world.Has<Material>(entity))
                {
                    batchMaterials[key] = world.Get<Material>(entity);
                    batchHasMaterials[key] = true;
                }
                else
                {
                    batchHasMaterials[key] = false;
                }
            }
        }

        // Update instance buffers for each batch
        foreach (var (key, instances) in batchData)
        {
            if (instances.Count == 0)
            {
                continue;
            }

            // Get or create prepared batch
            if (!preparedBatches.TryGetValue(key, out var prepared))
            {
                // Create new batch with initial buffer
                int capacity = Math.Max(InitialBufferCapacity, instances.Count);
                var buffer = graphics.CreateInstanceBuffer(capacity);

                prepared = new PreparedBatch
                {
                    Mesh = new MeshHandle(key.MeshId),
                    InstanceBuffer = buffer
                };
                preparedBatches[key] = prepared;
            }

            // Check if buffer needs to grow
            var currentBuffer = prepared.InstanceBuffer;
            // We need to track capacity - for now, recreate if count exceeds uploadBuffer
            if (instances.Count > uploadBuffer.Length)
            {
                // Grow upload buffer
                int newCapacity = (int)(instances.Count * BufferGrowthFactor);
                uploadBuffer = new InstanceData[newCapacity];

                // Recreate GPU buffer with new capacity
                graphics.DeleteInstanceBuffer(currentBuffer);
                currentBuffer = graphics.CreateInstanceBuffer(newCapacity);
                prepared.InstanceBuffer = currentBuffer;
            }

            // Copy to upload buffer
            for (int i = 0; i < instances.Count; i++)
            {
                uploadBuffer[i] = instances[i];
            }

            // Upload to GPU
            graphics.UpdateInstanceBuffer(currentBuffer, uploadBuffer.AsSpan(0, instances.Count));

            // Update batch metadata
            prepared.InstanceCount = instances.Count;
            prepared.Layer = batchLayers.GetValueOrDefault(key, 0);
            prepared.HasMaterial = batchHasMaterials.GetValueOrDefault(key, false);
            if (prepared.HasMaterial && batchMaterials.TryGetValue(key, out var material))
            {
                prepared.Material = material;
            }
        }

        // Clean up empty batches (batches that had no entities this frame)
        var keysToRemove = new List<BatchKey>();
        foreach (var (key, prepared) in preparedBatches)
        {
            if (!batchData.TryGetValue(key, out var instances) || instances.Count == 0)
            {
                // Delete GPU buffer
                graphics.DeleteInstanceBuffer(prepared.InstanceBuffer);
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            preparedBatches.Remove(key);
        }

        // Render all batches
        RenderBatches();
    }

    /// <summary>
    /// Renders all prepared batches using instanced draw calls.
    /// </summary>
    private void RenderBatches()
    {
        if (preparedBatches.Count == 0 || graphics is null || world is null)
        {
            return;
        }

        // Find active camera
        Camera camera = default;
        Transform3D cameraTransform = default;
        bool foundCamera = false;

        // Prefer main camera tag
        foreach (var entity in world.Query<Camera, Transform3D, MainCameraTag>())
        {
            camera = world.Get<Camera>(entity);
            cameraTransform = world.Get<Transform3D>(entity);
            foundCamera = true;
            break;
        }

        // Fall back to any camera
        if (!foundCamera)
        {
            foreach (var entity in world.Query<Camera, Transform3D>())
            {
                camera = world.Get<Camera>(entity);
                cameraTransform = world.Get<Transform3D>(entity);
                foundCamera = true;
                break;
            }
        }

        if (!foundCamera)
        {
            // No camera, nothing to render
            return;
        }

        // Calculate camera matrices
        Matrix4x4 viewMatrix = camera.ViewMatrix(cameraTransform);
        Matrix4x4 projectionMatrix = camera.ProjectionMatrix();

        // Sort batches by layer
        var sortedBatches = preparedBatches.Values.OrderBy(b => b.Layer).ToList();

        ShaderHandle currentShader = default;

        foreach (var batch in sortedBatches)
        {
            if (batch.InstanceCount == 0)
            {
                continue;
            }

            // Determine shader (use instanced versions)
            ShaderHandle shader;
            Material material = batch.HasMaterial ? batch.Material : Material.Default;

            if (batch.HasMaterial)
            {
                // Use instanced lit shader for materials
                shader = graphics.InstancedLitShader;
            }
            else
            {
                // No material - use instanced solid shader
                shader = graphics.InstancedSolidShader;
            }

            // Handle culling based on material double-sided flag
            if (material.DoubleSided)
            {
                graphics.SetCulling(false);
            }
            else
            {
                graphics.SetCulling(true, CullFaceMode.Back);
            }

            // Handle alpha blending
            if (material.AlphaMode == AlphaMode.Blend)
            {
                graphics.SetBlending(true);
            }
            else
            {
                graphics.SetBlending(false);
            }

            // Bind shader if changed
            if (currentShader.Id != shader.Id)
            {
                graphics.BindShader(shader);
                currentShader = shader;

                // Set per-frame uniforms (instanced shaders don't have uModel)
                graphics.SetUniform("uView", viewMatrix);
                graphics.SetUniform("uProjection", projectionMatrix);
                graphics.SetUniform("uCameraPosition", cameraTransform.Position);

                // Set default light uniforms
                graphics.SetUniform("uLightDirection", -Vector3.UnitY);
                graphics.SetUniform("uLightColor", Vector3.One);
                graphics.SetUniform("uLightIntensity", 1f);
            }

            // Bind texture and set material uniforms
            var texture = material.BaseColorTextureId > 0
                ? new TextureHandle(material.BaseColorTextureId)
                : graphics.WhiteTexture;
            graphics.BindTexture(texture, 0);
            graphics.SetUniform("uTexture", 0);
            graphics.SetUniform("uColor", material.BaseColorFactor);
            graphics.SetUniform("uEmissive", material.EmissiveFactor);

            // Draw instanced
            graphics.BindMesh(batch.Mesh);
            graphics.DrawMeshInstanced(batch.Mesh, batch.InstanceBuffer, batch.InstanceCount);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Clean up all GPU buffers
        if (graphics is not null)
        {
            foreach (var prepared in preparedBatches.Values)
            {
                graphics.DeleteInstanceBuffer(prepared.InstanceBuffer);
            }
        }

        preparedBatches.Clear();
        batchData.Clear();
        batchLayers.Clear();
        batchMaterials.Clear();
        batchHasMaterials.Clear();
    }
}
