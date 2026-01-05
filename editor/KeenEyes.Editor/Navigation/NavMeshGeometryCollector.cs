// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Collects geometry from a scene for navigation mesh baking.
/// </summary>
/// <remarks>
/// <para>
/// The geometry collector traverses the scene hierarchy and gathers mesh data
/// from entities with <see cref="NavMeshSurface"/> components or matching
/// layer settings.
/// </para>
/// <para>
/// Collected geometry is returned as vertex and index arrays suitable for
/// the DotRecast mesh builder.
/// </para>
/// </remarks>
public sealed class NavMeshGeometryCollector
{
    private readonly List<float> vertices = [];
    private readonly List<int> indices = [];
    private readonly List<int> areaIds = [];

    /// <summary>
    /// Gets the collected vertices as XYZ triplets.
    /// </summary>
    public ReadOnlySpan<float> Vertices => vertices.ToArray();

    /// <summary>
    /// Gets the collected triangle indices.
    /// </summary>
    public ReadOnlySpan<int> Indices => indices.ToArray();

    /// <summary>
    /// Gets the per-triangle area IDs.
    /// </summary>
    public int[] AreaIds => areaIds.ToArray();

    /// <summary>
    /// Gets the number of triangles collected.
    /// </summary>
    public int TriangleCount => indices.Count / 3;

    /// <summary>
    /// Gets the number of vertices collected.
    /// </summary>
    public int VertexCount => vertices.Count / 3;

    /// <summary>
    /// Gets whether any geometry was collected.
    /// </summary>
    public bool HasGeometry => vertices.Count > 0 && indices.Count > 0;

    /// <summary>
    /// Collects geometry from a world based on the bake configuration.
    /// </summary>
    /// <param name="world">The world to collect geometry from.</param>
    /// <param name="config">The bake configuration.</param>
    /// <returns>A result containing collected geometry data or an error.</returns>
    public NavMeshGeometryResult Collect(IWorld world, NavMeshBakeConfig config)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(config);

        Clear();

        int surfaceCount = 0;

        // Query for entities with NavMeshSurface
        foreach (var entity in world.Query<NavMeshSurface>())
        {
            if (!world.IsAlive(entity))
            {
                continue;
            }

            ref readonly var surface = ref world.Get<NavMeshSurface>(entity);

            // Check layer mask
            if (config.LayerMask != -1 && (config.LayerMask & (1 << surface.Layer)) == 0)
            {
                continue;
            }

            // Check if surface is walkable
            if (!surface.IsWalkable && surface.AreaType != NavAreaType.NotWalkable)
            {
                continue;
            }

            // Collect geometry from this entity
            CollectEntityGeometry(world, entity, surface, config);
            surfaceCount++;
        }

        // Also collect any static geometry without explicit surface (if enabled)
        if (!config.StaticOnly || surfaceCount == 0)
        {
            CollectDefaultGeometry(world, config);
        }

        if (!HasGeometry)
        {
            return NavMeshGeometryResult.Failure("No geometry found. Add NavMeshSurface components to walkable surfaces.");
        }

        return NavMeshGeometryResult.Success(
            vertices.ToArray(),
            indices.ToArray(),
            areaIds.ToArray(),
            surfaceCount);
    }

    /// <summary>
    /// Clears all collected geometry.
    /// </summary>
    public void Clear()
    {
        vertices.Clear();
        indices.Clear();
        areaIds.Clear();
    }

    /// <summary>
    /// Adds geometry manually with transformation.
    /// </summary>
    /// <param name="meshVertices">The mesh vertices.</param>
    /// <param name="meshIndices">The triangle indices.</param>
    /// <param name="transform">The world transform matrix.</param>
    /// <param name="areaType">The navigation area type.</param>
    public void AddGeometry(
        ReadOnlySpan<Vector3> meshVertices,
        ReadOnlySpan<int> meshIndices,
        Matrix4x4 transform,
        NavAreaType areaType = NavAreaType.Walkable)
    {
        int baseVertex = vertices.Count / 3;

        // Transform and add vertices
        foreach (var vertex in meshVertices)
        {
            var transformed = Vector3.Transform(vertex, transform);
            vertices.Add(transformed.X);
            vertices.Add(transformed.Y);
            vertices.Add(transformed.Z);
        }

        // Add indices with offset
        int triangleCount = meshIndices.Length / 3;
        for (int i = 0; i < meshIndices.Length; i++)
        {
            indices.Add(meshIndices[i] + baseVertex);
        }

        // Add area IDs for each triangle
        for (int i = 0; i < triangleCount; i++)
        {
            areaIds.Add((int)areaType);
        }
    }

    /// <summary>
    /// Adds geometry from float arrays (XYZ triplets).
    /// </summary>
    /// <param name="meshVertices">The mesh vertices as XYZ triplets.</param>
    /// <param name="meshIndices">The triangle indices.</param>
    /// <param name="transform">The world transform matrix.</param>
    /// <param name="areaType">The navigation area type.</param>
    public void AddGeometry(
        ReadOnlySpan<float> meshVertices,
        ReadOnlySpan<int> meshIndices,
        Matrix4x4 transform,
        NavAreaType areaType = NavAreaType.Walkable)
    {
        int baseVertex = vertices.Count / 3;

        // Transform and add vertices
        for (int i = 0; i < meshVertices.Length; i += 3)
        {
            var vertex = new Vector3(meshVertices[i], meshVertices[i + 1], meshVertices[i + 2]);
            var transformed = Vector3.Transform(vertex, transform);
            vertices.Add(transformed.X);
            vertices.Add(transformed.Y);
            vertices.Add(transformed.Z);
        }

        // Add indices with offset
        int triangleCount = meshIndices.Length / 3;
        for (int i = 0; i < meshIndices.Length; i++)
        {
            indices.Add(meshIndices[i] + baseVertex);
        }

        // Add area IDs for each triangle
        for (int i = 0; i < triangleCount; i++)
        {
            areaIds.Add((int)areaType);
        }
    }

    /// <summary>
    /// Adds a box as geometry (useful for floor/ground planes).
    /// </summary>
    /// <param name="min">The minimum corner of the box.</param>
    /// <param name="max">The maximum corner of the box.</param>
    /// <param name="areaType">The navigation area type.</param>
    public void AddBox(Vector3 min, Vector3 max, NavAreaType areaType = NavAreaType.Walkable)
    {
        int baseVertex = vertices.Count / 3;

        // Create a complete box with 6 faces
        float bottomY = min.Y;
        float topY = max.Y;

        // Bottom face (4 vertices)
        vertices.Add(min.X); vertices.Add(bottomY); vertices.Add(min.Z);  // 0
        vertices.Add(max.X); vertices.Add(bottomY); vertices.Add(min.Z);  // 1
        vertices.Add(max.X); vertices.Add(bottomY); vertices.Add(max.Z);  // 2
        vertices.Add(min.X); vertices.Add(bottomY); vertices.Add(max.Z);  // 3

        // Top face (4 vertices)
        vertices.Add(min.X); vertices.Add(topY); vertices.Add(min.Z);     // 4
        vertices.Add(max.X); vertices.Add(topY); vertices.Add(min.Z);     // 5
        vertices.Add(max.X); vertices.Add(topY); vertices.Add(max.Z);     // 6
        vertices.Add(min.X); vertices.Add(topY); vertices.Add(max.Z);     // 7

        // Add all 12 triangles (6 faces x 2 triangles each)
        int[] boxIndices =
        [
            // Bottom face
            0, 2, 1, 0, 3, 2,
            // Top face (walkable surface)
            4, 5, 6, 4, 6, 7,
            // Front face (-Z)
            0, 1, 5, 0, 5, 4,
            // Back face (+Z)
            2, 3, 7, 2, 7, 6,
            // Left face (-X)
            0, 4, 7, 0, 7, 3,
            // Right face (+X)
            1, 2, 6, 1, 6, 5
        ];

        for (int i = 0; i < boxIndices.Length; i++)
        {
            indices.Add(boxIndices[i] + baseVertex);
        }

        // 12 triangles
        for (int i = 0; i < 12; i++)
        {
            areaIds.Add((int)areaType);
        }
    }

    private void CollectEntityGeometry(
        IWorld world,
        Entity entity,
        in NavMeshSurface surface,
        NavMeshBakeConfig config)
    {
        // Get world transform
        var transform = GetWorldTransform(world, entity);

        // Check modifiers
        var areaType = surface.AreaType;
        bool ignore = false;

        if (world.Has<NavMeshModifier>(entity))
        {
            ref readonly var modifier = ref world.Get<NavMeshModifier>(entity);
            if (modifier.IgnoreFromBuild)
            {
                ignore = true;
            }
            else if (modifier.OverrideAreaType)
            {
                areaType = modifier.AreaType;
            }
        }

        if (ignore)
        {
            return;
        }

        // Check bounds if configured
        if (config.BakeBounds.HasValue)
        {
            var position = GetPosition(world, entity);
            var bounds = config.BakeBounds.Value;

            if (position.X < bounds.Min.X || position.X > bounds.Max.X ||
                position.Y < bounds.Min.Y || position.Y > bounds.Max.Y ||
                position.Z < bounds.Min.Z || position.Z > bounds.Max.Z)
            {
                return;
            }
        }

        // Collect based on geometry type
        switch (surface.CollectGeometry)
        {
            case NavMeshCollectGeometry.RenderMeshes:
                CollectRenderMesh(world, entity, transform, areaType);
                break;
            case NavMeshCollectGeometry.PhysicsColliders:
                CollectPhysicsCollider(world, entity, transform, areaType);
                break;
            case NavMeshCollectGeometry.Both:
                CollectRenderMesh(world, entity, transform, areaType);
                CollectPhysicsCollider(world, entity, transform, areaType);
                break;
        }

        // Collect children if enabled
        if (surface.IncludeChildren)
        {
            CollectChildGeometry(world, entity, config, areaType);
        }
    }

    private void CollectDefaultGeometry(IWorld world, NavMeshBakeConfig config)
    {
        // Collect geometry from entities with mesh but no explicit surface
        // This is a fallback for simple scenes
        foreach (var entity in world.Query<Transform3D>())
        {
            if (!world.IsAlive(entity))
            {
                continue;
            }

            // Skip if already processed via NavMeshSurface
            if (world.Has<NavMeshSurface>(entity))
            {
                continue;
            }

            // Check if entity should be excluded
            if (world.Has<NavMeshModifier>(entity))
            {
                ref readonly var modifier = ref world.Get<NavMeshModifier>(entity);
                if (modifier.IgnoreFromBuild)
                {
                    continue;
                }
            }

            // Collect render mesh if present
            var transform = GetWorldTransform(world, entity);
            CollectRenderMesh(world, entity, transform, NavAreaType.Walkable);
        }
    }

    private void CollectRenderMesh(IWorld world, Entity entity, Matrix4x4 transform, NavAreaType areaType)
    {
        // Check for mesh data component
        // This would integrate with the actual mesh system
        // For now, we look for a hypothetical MeshData component
        if (!world.Has<MeshData>(entity))
        {
            return;
        }

        ref readonly var meshData = ref world.Get<MeshData>(entity);

        if (meshData.Vertices.Length == 0 || meshData.Indices.Length == 0)
        {
            return;
        }

        AddGeometry(meshData.Vertices.Span, meshData.Indices.Span, transform, areaType);
    }

    private void CollectPhysicsCollider(IWorld world, Entity entity, Matrix4x4 transform, NavAreaType areaType)
    {
        // Check for collider component
        // This would integrate with the physics system
        if (!world.Has<ColliderShape>(entity))
        {
            return;
        }

        ref readonly var collider = ref world.Get<ColliderShape>(entity);

        // Generate geometry based on collider type
        switch (collider.Type)
        {
            case ColliderType.Box:
                AddBox(
                    Vector3.Transform(collider.BoxMin, transform),
                    Vector3.Transform(collider.BoxMax, transform),
                    areaType);
                break;

            case ColliderType.Mesh:
                if (collider.MeshVertices.Length > 0 && collider.MeshIndices.Length > 0)
                {
                    AddGeometry(collider.MeshVertices.Span, collider.MeshIndices.Span, transform, areaType);
                }
                break;
        }
    }

    private void CollectChildGeometry(
        IWorld world,
        Entity parent,
        NavMeshBakeConfig config,
        NavAreaType parentAreaType)
    {
        var children = world.GetChildren(parent);

        foreach (var child in children)
        {
            if (!world.IsAlive(child))
            {
                continue;
            }

            var areaType = parentAreaType;
            bool ignore = false;

            // Check for modifier on child
            if (world.Has<NavMeshModifier>(child))
            {
                ref readonly var modifier = ref world.Get<NavMeshModifier>(child);
                if (modifier.IgnoreFromBuild)
                {
                    ignore = true;
                }
                else if (modifier.OverrideAreaType)
                {
                    areaType = modifier.AreaType;
                }
            }

            if (!ignore)
            {
                var transform = GetWorldTransform(world, child);
                CollectRenderMesh(world, child, transform, areaType);
                CollectPhysicsCollider(world, child, transform, areaType);

                // Recursively collect from grandchildren
                CollectChildGeometry(world, child, config, areaType);
            }
        }
    }

    private static Matrix4x4 GetWorldTransform(IWorld world, Entity entity)
    {
        if (!world.Has<Transform3D>(entity))
        {
            return Matrix4x4.Identity;
        }

        ref readonly var transform = ref world.Get<Transform3D>(entity);
        return transform.Matrix();
    }

    private static Vector3 GetPosition(IWorld world, Entity entity)
    {
        if (!world.Has<Transform3D>(entity))
        {
            return Vector3.Zero;
        }

        ref readonly var transform = ref world.Get<Transform3D>(entity);
        return transform.Position;
    }
}

/// <summary>
/// Result of geometry collection for navmesh baking.
/// </summary>
public sealed class NavMeshGeometryResult
{
    /// <summary>
    /// Gets whether the collection was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets the error message if collection failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets the collected vertices as XYZ triplets.
    /// </summary>
    public float[] Vertices { get; private init; } = [];

    /// <summary>
    /// Gets the collected triangle indices.
    /// </summary>
    public int[] Indices { get; private init; } = [];

    /// <summary>
    /// Gets the per-triangle area IDs.
    /// </summary>
    public int[] AreaIds { get; private init; } = [];

    /// <summary>
    /// Gets the number of surfaces processed.
    /// </summary>
    public int SurfaceCount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static NavMeshGeometryResult Success(float[] vertices, int[] indices, int[] areaIds, int surfaceCount)
        => new()
        {
            IsSuccess = true,
            Vertices = vertices,
            Indices = indices,
            AreaIds = areaIds,
            SurfaceCount = surfaceCount
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static NavMeshGeometryResult Failure(string message)
        => new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };
}

/// <summary>
/// Temporary mesh data component for geometry collection.
/// </summary>
/// <remarks>
/// This component stores mesh vertex and index data for navmesh baking.
/// It should be populated from the actual graphics mesh system.
/// </remarks>
internal struct MeshData : IComponent
{
    public ReadOnlyMemory<Vector3> Vertices;
    public ReadOnlyMemory<int> Indices;
}

/// <summary>
/// Temporary collider shape component for geometry collection.
/// </summary>
internal struct ColliderShape : IComponent
{
    public ColliderType Type;
    public Vector3 BoxMin;
    public Vector3 BoxMax;
    public ReadOnlyMemory<Vector3> MeshVertices;
    public ReadOnlyMemory<int> MeshIndices;
}

/// <summary>
/// Types of collider shapes.
/// </summary>
internal enum ColliderType
{
    Box,
    Sphere,
    Capsule,
    Mesh
}
