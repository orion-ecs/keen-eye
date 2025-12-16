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
    #region Window Settings

    /// <summary>
    /// Gets or sets the initial window width in pixels.
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Gets or sets the initial window height in pixels.
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string WindowTitle { get; set; } = "KeenEyes Application";

    /// <summary>
    /// Gets or sets whether to enable VSync.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the window is resizable.
    /// </summary>
    public bool Resizable { get; set; } = true;

    #endregion

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
