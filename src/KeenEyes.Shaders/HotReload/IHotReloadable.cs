namespace KeenEyes.Shaders.HotReload;

/// <summary>
/// Interface for shaders that support hot-reload during development.
/// </summary>
public interface IHotReloadable
{
    /// <summary>
    /// Gets the unique name of this shader for hot-reload identification.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the source file path if available.
    /// </summary>
    string? SourcePath { get; }

    /// <summary>
    /// Called when the shader source has been recompiled and the shader should update.
    /// </summary>
    /// <param name="newSource">The new shader source code.</param>
    /// <param name="backend">The shader backend to use.</param>
    void Reload(string newSource, ShaderBackend backend);
}
