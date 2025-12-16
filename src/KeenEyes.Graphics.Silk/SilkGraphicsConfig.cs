using System.Numerics;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Configuration options for the Silk.NET graphics plugin.
/// </summary>
/// <remarks>
/// Unlike the standalone graphics plugin, this configuration does not include
/// window settings. Window configuration is handled by <c>SilkWindowPlugin</c>.
/// </remarks>
public sealed class SilkGraphicsConfig
{
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
}
