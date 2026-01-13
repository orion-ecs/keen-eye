namespace KeenEyes.Shaders;

/// <summary>
/// Interface for shader pipelines that bundle vertex, geometry, and fragment shaders.
/// </summary>
public interface IShaderPipeline
{
    /// <summary>
    /// Gets the pipeline name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the vertex shader name, or null if not defined.
    /// </summary>
    string? VertexShaderName { get; }

    /// <summary>
    /// Gets the geometry shader name, or null if not defined.
    /// </summary>
    string? GeometryShaderName { get; }

    /// <summary>
    /// Gets the fragment shader name, or null if not defined.
    /// </summary>
    string? FragmentShaderName { get; }

    /// <summary>
    /// Gets whether this pipeline has a vertex stage.
    /// </summary>
    bool HasVertexStage { get; }

    /// <summary>
    /// Gets whether this pipeline has a geometry stage.
    /// </summary>
    bool HasGeometryStage { get; }

    /// <summary>
    /// Gets whether this pipeline has a fragment stage.
    /// </summary>
    bool HasFragmentStage { get; }
}
