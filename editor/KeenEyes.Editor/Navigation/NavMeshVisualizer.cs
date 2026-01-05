// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Renders navigation mesh debug visualization in the editor viewport.
/// </summary>
/// <remarks>
/// <para>
/// The visualizer draws the navmesh polygons with color-coded area types,
/// helping developers verify navigation mesh coverage and area assignments.
/// </para>
/// <para>
/// Visualization can be toggled on/off and supports different display modes:
/// surfaces, edges, vertices, or combinations.
/// </para>
/// </remarks>
public sealed class NavMeshVisualizer : IGizmoRenderer
{
    private NavMeshData? navMesh;
    private readonly Dictionary<NavAreaType, Vector4> areaColors;

    /// <summary>
    /// Creates a new NavMeshVisualizer.
    /// </summary>
    public NavMeshVisualizer()
    {
        areaColors = CreateDefaultAreaColors();
    }

    /// <inheritdoc/>
    public string Id => "navmesh-visualizer";

    /// <inheritdoc/>
    public string DisplayName => "NavMesh";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public int Order => 100;

    /// <summary>
    /// Gets or sets the current display mode.
    /// </summary>
    public NavMeshDisplayMode DisplayMode { get; set; } = NavMeshDisplayMode.SurfacesAndEdges;

    /// <summary>
    /// Gets or sets the surface transparency (0-1).
    /// </summary>
    public float SurfaceAlpha { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the edge color.
    /// </summary>
    public Vector4 EdgeColor { get; set; } = new(0.1f, 0.1f, 0.1f, 1.0f);

    /// <summary>
    /// Gets or sets the edge width.
    /// </summary>
    public float EdgeWidth { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the vertex color.
    /// </summary>
    public Vector4 VertexColor { get; set; } = new(1.0f, 1.0f, 0.0f, 1.0f);

    /// <summary>
    /// Gets or sets the vertex size.
    /// </summary>
    public float VertexSize { get; set; } = 5.0f;

    /// <summary>
    /// Gets or sets the height offset to prevent z-fighting with scene geometry.
    /// </summary>
    public float HeightOffset { get; set; } = 0.02f;

    /// <summary>
    /// Gets or sets whether to show area type labels.
    /// </summary>
    public bool ShowAreaLabels { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to show polygon indices.
    /// </summary>
    public bool ShowPolygonIndices { get; set; } = false;

    /// <summary>
    /// Sets the navmesh to visualize.
    /// </summary>
    /// <param name="mesh">The navmesh data, or null to clear.</param>
    public void SetNavMesh(NavMeshData? mesh)
    {
        navMesh = mesh;
    }

    /// <summary>
    /// Gets the current navmesh being visualized.
    /// </summary>
    public NavMeshData? GetNavMesh() => navMesh;

    /// <summary>
    /// Sets the color for a specific area type.
    /// </summary>
    /// <param name="areaType">The area type.</param>
    /// <param name="color">The color (RGBA, 0-1).</param>
    public void SetAreaColor(NavAreaType areaType, Vector4 color)
    {
        areaColors[areaType] = color;
    }

    /// <summary>
    /// Gets the color for a specific area type.
    /// </summary>
    /// <param name="areaType">The area type.</param>
    /// <returns>The color for the area type.</returns>
    public Vector4 GetAreaColor(NavAreaType areaType)
    {
        return areaColors.TryGetValue(areaType, out var color)
            ? color
            : new Vector4(0.5f, 0.5f, 0.5f, SurfaceAlpha);
    }

    /// <inheritdoc/>
    public void Render(GizmoRenderContext context)
    {
        if (!IsEnabled || navMesh == null)
        {
            return;
        }

        // Collect all polygon data for rendering
        var polygons = CollectPolygons();

        if (polygons.Count == 0)
        {
            return;
        }

        // Render based on display mode
        if (DisplayMode.HasFlag(NavMeshDisplayMode.Surfaces))
        {
            RenderSurfaces(context, polygons);
        }

        if (DisplayMode.HasFlag(NavMeshDisplayMode.Edges))
        {
            RenderEdges(context, polygons);
        }

        if (DisplayMode.HasFlag(NavMeshDisplayMode.Vertices))
        {
            RenderVertices(context, polygons);
        }
    }

    /// <inheritdoc/>
    public bool ShouldRender(Entity entity, IWorld sceneWorld)
    {
        // This gizmo renders the global navmesh, not per-entity
        // Return true for the first entity to trigger one render
        return navMesh != null;
    }

    private List<NavMeshPolygon> CollectPolygons()
    {
        var polygons = new List<NavMeshPolygon>();

        if (navMesh == null)
        {
            return polygons;
        }

        // Iterate through all polygons in the navmesh
        int polygonCount = navMesh.PolygonCount;

        // This is a simplified implementation - actual implementation would
        // iterate through DtNavMesh tiles and polygons
        // For now, we'll use the public API to get polygon vertices
        for (uint polyId = 1; polyId <= (uint)polygonCount; polyId++)
        {
            try
            {
                var vertices = navMesh.GetPolygonVertices(polyId);
                if (vertices.Length >= 3)
                {
                    // Get area type from polygon
                    var centroid = CalculateCentroid(vertices);
                    var areaType = navMesh.GetAreaType(centroid);

                    polygons.Add(new NavMeshPolygon
                    {
                        PolygonId = polyId,
                        Vertices = vertices.ToArray(),
                        AreaType = areaType
                    });
                }
            }
            catch
            {
                // Skip invalid polygon IDs
            }
        }

        return polygons;
    }

    private void RenderSurfaces(GizmoRenderContext context, List<NavMeshPolygon> polygons)
    {
        // This would integrate with the actual rendering system
        // For now, we prepare the data that would be sent to the GPU

        foreach (var polygon in polygons)
        {
            var baseColor = GetAreaColor(polygon.AreaType);
            var color = baseColor with { W = SurfaceAlpha };

            // Apply height offset
            var offsetVertices = new Vector3[polygon.Vertices.Length];
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                offsetVertices[i] = polygon.Vertices[i] + new Vector3(0, HeightOffset, 0);
            }

            // Triangulate the polygon (fan triangulation for convex polygons)
            for (int i = 1; i < offsetVertices.Length - 1; i++)
            {
                // In actual implementation, these would be added to a render batch
                // TODO: Integrate with actual gizmo rendering API when available
                context.DrawTriangle(offsetVertices[0], offsetVertices[i], offsetVertices[i + 1], color);
            }
        }
    }

    private void RenderEdges(GizmoRenderContext context, List<NavMeshPolygon> polygons)
    {
        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                var v0 = polygon.Vertices[i] + new Vector3(0, HeightOffset * 2, 0);
                var v1 = polygon.Vertices[(i + 1) % polygon.Vertices.Length] + new Vector3(0, HeightOffset * 2, 0);

                // TODO: Integrate with actual gizmo rendering API when available
                context.DrawLine(v0, v1, EdgeColor, EdgeWidth);
            }
        }
    }

    private void RenderVertices(GizmoRenderContext context, List<NavMeshPolygon> polygons)
    {
        var renderedVertices = new HashSet<Vector3>();

        foreach (var polygon in polygons)
        {
            foreach (var vertex in polygon.Vertices)
            {
                var offsetVertex = vertex + new Vector3(0, HeightOffset * 3, 0);

                // Avoid rendering the same vertex multiple times
                if (renderedVertices.Add(offsetVertex))
                {
                    // TODO: Integrate with actual gizmo rendering API when available
                    context.DrawPoint(offsetVertex, VertexColor, VertexSize);
                }
            }
        }
    }

    private static Vector3 CalculateCentroid(ReadOnlySpan<Vector3> vertices)
    {
        var sum = Vector3.Zero;
        foreach (var vertex in vertices)
        {
            sum += vertex;
        }

        return sum / vertices.Length;
    }

    private static Dictionary<NavAreaType, Vector4> CreateDefaultAreaColors()
    {
        return new Dictionary<NavAreaType, Vector4>
        {
            [NavAreaType.Walkable] = new Vector4(0.0f, 0.75f, 1.0f, 0.5f),    // Cyan
            [NavAreaType.Road] = new Vector4(0.6f, 0.6f, 0.6f, 0.5f),         // Gray
            [NavAreaType.Grass] = new Vector4(0.0f, 0.8f, 0.2f, 0.5f),        // Green
            [NavAreaType.Water] = new Vector4(0.0f, 0.3f, 1.0f, 0.5f),        // Blue
            [NavAreaType.Sand] = new Vector4(1.0f, 0.9f, 0.5f, 0.5f),         // Yellow
            [NavAreaType.Mud] = new Vector4(0.5f, 0.3f, 0.1f, 0.5f),          // Brown
            [NavAreaType.Ice] = new Vector4(0.8f, 0.9f, 1.0f, 0.5f),          // Light blue
            [NavAreaType.Hazard] = new Vector4(1.0f, 0.0f, 0.0f, 0.5f),       // Red
            [NavAreaType.Door] = new Vector4(0.8f, 0.5f, 0.0f, 0.5f),         // Orange
            [NavAreaType.Jump] = new Vector4(1.0f, 1.0f, 0.0f, 0.5f),         // Yellow
            [NavAreaType.Climb] = new Vector4(1.0f, 0.5f, 1.0f, 0.5f),        // Magenta
            [NavAreaType.OffMeshLink] = new Vector4(0.5f, 0.0f, 1.0f, 0.5f),  // Purple
            [NavAreaType.NotWalkable] = new Vector4(1.0f, 0.0f, 0.0f, 0.3f)   // Transparent red
        };
    }

    /// <summary>
    /// Internal polygon data for rendering.
    /// </summary>
    private readonly struct NavMeshPolygon
    {
        public uint PolygonId { get; init; }
        public Vector3[] Vertices { get; init; }
        public NavAreaType AreaType { get; init; }
    }
}

/// <summary>
/// Display modes for navmesh visualization.
/// </summary>
[Flags]
public enum NavMeshDisplayMode
{
    /// <summary>
    /// Display nothing.
    /// </summary>
    None = 0,

    /// <summary>
    /// Display filled polygon surfaces.
    /// </summary>
    Surfaces = 1,

    /// <summary>
    /// Display polygon edges.
    /// </summary>
    Edges = 2,

    /// <summary>
    /// Display polygon vertices.
    /// </summary>
    Vertices = 4,

    /// <summary>
    /// Display surfaces and edges.
    /// </summary>
    SurfacesAndEdges = Surfaces | Edges,

    /// <summary>
    /// Display all elements.
    /// </summary>
    All = Surfaces | Edges | Vertices
}
