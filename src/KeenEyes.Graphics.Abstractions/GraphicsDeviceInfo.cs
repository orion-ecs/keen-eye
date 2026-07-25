namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Identity and API version of the graphics driver behind an <see cref="IGraphicsDevice"/>.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IGraphicsDevice.GetDeviceInfo"/>. The strings are the driver's own,
/// reported verbatim so that a log line or bug report carries what the machine actually has;
/// the parsed <see cref="MajorVersion"/> and <see cref="MinorVersion"/> are what capability
/// checks compare.
/// </para>
/// <para>
/// A string is empty when the driver does not report it. The classic case is
/// <see cref="ShadingLanguageVersion"/> on an OpenGL 1.1 software fallback, which predates
/// shaders entirely.
/// </para>
/// </remarks>
/// <param name="Vendor">The driver vendor, for example <c>NVIDIA Corporation</c>.</param>
/// <param name="Renderer">The renderer, usually the GPU model, for example <c>GeForce RTX 4070</c>.</param>
/// <param name="Version">The driver's API version string, for example <c>4.6.0 NVIDIA 551.86</c>.</param>
/// <param name="ShadingLanguageVersion">The driver's shading language version string, for example <c>4.60</c>.</param>
/// <param name="MajorVersion">The major API version, or 0 when it could not be determined.</param>
/// <param name="MinorVersion">The minor API version, or 0 when it could not be determined.</param>
public readonly record struct GraphicsDeviceInfo(
    string Vendor,
    string Renderer,
    string Version,
    string ShadingLanguageVersion,
    int MajorVersion,
    int MinorVersion)
{
    /// <summary>
    /// Determines whether the device's API version is at least the specified version.
    /// </summary>
    /// <param name="major">The required major version.</param>
    /// <param name="minor">The required minor version.</param>
    /// <returns>True when the device reports <paramref name="major"/>.<paramref name="minor"/> or newer.</returns>
    public bool IsAtLeast(int major, int minor)
        => MajorVersion > major || (MajorVersion == major && MinorVersion >= minor);

    /// <summary>
    /// Returns a single-line summary suitable for logs and error messages.
    /// </summary>
    /// <returns>The driver's version, shading language version, renderer, and vendor.</returns>
    public override string ToString()
        => $"{Describe(Version)} | shading language {Describe(ShadingLanguageVersion)} " +
           $"| renderer: {Describe(Renderer)} | vendor: {Describe(Vendor)}";

    private static string Describe(string value)
        => string.IsNullOrWhiteSpace(value) ? "unreported" : value;
}
