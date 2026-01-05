// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Editor command that bakes a navigation mesh from scene geometry.
/// </summary>
/// <remarks>
/// <para>
/// This command collects geometry from the scene, builds a navigation mesh
/// using DotRecast, and saves it to a .kenavmesh file. The baking process
/// is performed synchronously but can be cancelled.
/// </para>
/// <para>
/// The command supports undo/redo by keeping track of the previous navmesh
/// file and restoring it on undo.
/// </para>
/// </remarks>
public sealed class NavMeshBakeCommand : IEditorCommand
{
    private readonly World world;
    private readonly NavMeshBakeConfig config;
    private readonly Action<NavMeshBakeProgress>? progressCallback;

    private string? outputPath;
    private byte[]? previousData;
    private NavMeshData? bakedNavMesh;
    private NavMeshBakeResult? result;

    /// <summary>
    /// Creates a new bake command.
    /// </summary>
    /// <param name="world">The world containing the scene geometry.</param>
    /// <param name="config">The bake configuration.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    public NavMeshBakeCommand(
        World world,
        NavMeshBakeConfig config,
        Action<NavMeshBakeProgress>? progressCallback = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(config);

        this.world = world;
        this.config = config.Clone();
        this.progressCallback = progressCallback;
    }

    /// <inheritdoc/>
    public string Description => $"Bake NavMesh ({config.AgentTypeName})";

    /// <summary>
    /// Gets the baked navmesh data after successful execution.
    /// </summary>
    public NavMeshData? BakedNavMesh => bakedNavMesh;

    /// <summary>
    /// Gets the bake result.
    /// </summary>
    public NavMeshBakeResult? Result => result;

    /// <inheritdoc/>
    public void Execute()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate configuration
            ReportProgress(NavMeshBakePhase.Validating, 0, "Validating configuration...");

            var validationError = config.Validate();
            if (validationError != null)
            {
                result = NavMeshBakeResult.Failure(validationError);
                return;
            }

            // Determine output path
            outputPath = config.OutputPath ?? GetDefaultOutputPath();

            // Backup existing navmesh if present
            if (File.Exists(outputPath))
            {
                previousData = File.ReadAllBytes(outputPath);
            }

            // Collect geometry
            ReportProgress(NavMeshBakePhase.CollectingGeometry, 10, "Collecting geometry...");

            var collector = new NavMeshGeometryCollector();
            var geometryResult = collector.Collect(world, config);

            if (!geometryResult.IsSuccess)
            {
                result = NavMeshBakeResult.Failure(geometryResult.ErrorMessage!);
                return;
            }

            ReportProgress(
                NavMeshBakePhase.CollectingGeometry,
                20,
                $"Collected {collector.TriangleCount} triangles from {geometryResult.SurfaceCount} surfaces");

            // Build navmesh
            ReportProgress(NavMeshBakePhase.Building, 30, "Building navmesh...");

            var runtimeConfig = config.ToRuntimeConfig();
            var builder = new DotRecastMeshBuilder(runtimeConfig);

            bakedNavMesh = builder.Build(
                geometryResult.Vertices,
                geometryResult.Indices,
                geometryResult.AreaIds);

            ReportProgress(
                NavMeshBakePhase.Building,
                80,
                $"Built navmesh with {bakedNavMesh.PolygonCount} polygons");

            // Save to file
            ReportProgress(NavMeshBakePhase.Saving, 90, "Saving navmesh...");

            NavMeshSerializer.Save(outputPath, bakedNavMesh, config);

            stopwatch.Stop();

            // Report success
            ReportProgress(NavMeshBakePhase.Complete, 100, "Bake complete!");

            result = NavMeshBakeResult.Success(
                outputPath,
                bakedNavMesh.PolygonCount,
                bakedNavMesh.VertexCount,
                geometryResult.SurfaceCount,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result = NavMeshBakeResult.Failure($"Bake failed: {ex.Message}");
            ReportProgress(NavMeshBakePhase.Failed, 0, result.ErrorMessage!);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (outputPath == null)
        {
            return;
        }

        if (previousData != null)
        {
            // Restore previous navmesh
            File.WriteAllBytes(outputPath, previousData);
        }
        else if (File.Exists(outputPath))
        {
            // Remove the new navmesh
            File.Delete(outputPath);
        }

        bakedNavMesh = null;
        result = null;
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;

    private void ReportProgress(NavMeshBakePhase phase, int percentage, string message)
    {
        progressCallback?.Invoke(new NavMeshBakeProgress
        {
            Phase = phase,
            Percentage = percentage,
            Message = message
        });
    }

    private string GetDefaultOutputPath()
    {
        // Use scene name or default
        var sceneName = "Scene";
        var agentName = config.AgentTypeName.Replace(" ", "");
        return Path.Combine(
            Environment.CurrentDirectory,
            "Assets",
            "Navigation",
            $"{sceneName}_{agentName}.kenavmesh");
    }
}

/// <summary>
/// Progress information during navmesh baking.
/// </summary>
public readonly struct NavMeshBakeProgress
{
    /// <summary>
    /// Gets the current baking phase.
    /// </summary>
    public NavMeshBakePhase Phase { get; init; }

    /// <summary>
    /// Gets the overall progress percentage (0-100).
    /// </summary>
    public int Percentage { get; init; }

    /// <summary>
    /// Gets the status message.
    /// </summary>
    public string Message { get; init; }
}

/// <summary>
/// Phases of the navmesh baking process.
/// </summary>
public enum NavMeshBakePhase
{
    /// <summary>
    /// Validating configuration.
    /// </summary>
    Validating,

    /// <summary>
    /// Collecting geometry from the scene.
    /// </summary>
    CollectingGeometry,

    /// <summary>
    /// Building the navmesh.
    /// </summary>
    Building,

    /// <summary>
    /// Saving the navmesh to disk.
    /// </summary>
    Saving,

    /// <summary>
    /// Baking completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// Baking failed.
    /// </summary>
    Failed
}

/// <summary>
/// Result of a navmesh baking operation.
/// </summary>
public sealed class NavMeshBakeResult
{
    /// <summary>
    /// Gets whether the bake was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets the error message if the bake failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets the output file path.
    /// </summary>
    public string? OutputPath { get; private init; }

    /// <summary>
    /// Gets the number of polygons in the baked navmesh.
    /// </summary>
    public int PolygonCount { get; private init; }

    /// <summary>
    /// Gets the number of vertices in the baked navmesh.
    /// </summary>
    public int VertexCount { get; private init; }

    /// <summary>
    /// Gets the number of surfaces processed.
    /// </summary>
    public int SurfaceCount { get; private init; }

    /// <summary>
    /// Gets the time taken to bake.
    /// </summary>
    public TimeSpan ElapsedTime { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static NavMeshBakeResult Success(
        string outputPath,
        int polygonCount,
        int vertexCount,
        int surfaceCount,
        TimeSpan elapsedTime)
        => new()
        {
            IsSuccess = true,
            OutputPath = outputPath,
            PolygonCount = polygonCount,
            VertexCount = vertexCount,
            SurfaceCount = surfaceCount,
            ElapsedTime = elapsedTime
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static NavMeshBakeResult Failure(string message)
        => new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };

    /// <summary>
    /// Gets a summary string of the result.
    /// </summary>
    public override string ToString()
    {
        if (IsSuccess)
        {
            return $"NavMesh baked: {PolygonCount} polygons, {VertexCount} vertices from {SurfaceCount} surfaces in {ElapsedTime.TotalSeconds:F2}s";
        }

        return $"NavMesh bake failed: {ErrorMessage}";
    }
}
