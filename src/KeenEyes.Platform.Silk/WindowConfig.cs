namespace KeenEyes.Platform.Silk;

/// <summary>
/// Configuration options for creating a Silk.NET window.
/// </summary>
public sealed class WindowConfig
{
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title { get; set; } = "KeenEyes";

    /// <summary>
    /// Gets or sets the initial window width in pixels.
    /// </summary>
    public int Width { get; set; } = 1280;

    /// <summary>
    /// Gets or sets the initial window height in pixels.
    /// </summary>
    public int Height { get; set; } = 720;

    /// <summary>
    /// Gets or sets whether vertical sync is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the window is resizable.
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Gets or sets the target frames per second. Set to 0 for uncapped.
    /// </summary>
    public double TargetFramerate { get; set; } = 60.0;

    /// <summary>
    /// Gets or sets the target updates per second for fixed timestep.
    /// </summary>
    public double TargetUpdateFrequency { get; set; } = 60.0;
}
