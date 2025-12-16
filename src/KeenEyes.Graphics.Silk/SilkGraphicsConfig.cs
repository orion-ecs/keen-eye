using System.Numerics;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Configuration options for the Silk.NET graphics plugin.
/// </summary>
/// <remarks>
/// This configuration includes both window and rendering settings.
/// The graphics plugin creates and manages its own window.
/// </remarks>
public sealed class SilkGraphicsConfig
{
    #region Rendering Settings

    /// <summary>
    /// Gets or sets the default clear color.
    /// </summary>
    public Vector4 ClearColor { get; set; } = new(0.1f, 0.1f, 0.1f, 1f);

    /// <summary>
    /// Gets or sets whether to enable depth testing by default.
    /// </summary>
    public bool EnableDepthTest { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable backface culling by default.
    /// </summary>
    public bool EnableCulling { get; set; } = true;

    #endregion
}
