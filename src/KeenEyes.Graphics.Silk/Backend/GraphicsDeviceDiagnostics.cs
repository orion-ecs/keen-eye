using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Backend;

/// <summary>
/// Diagnostic formatting shared by the backend's shader failure paths.
/// </summary>
internal static class GraphicsDeviceDiagnostics
{
    /// <summary>
    /// Combines a driver info log with the driver's identity and version strings.
    /// </summary>
    /// <param name="device">The device whose shader compilation or linking failed.</param>
    /// <param name="infoLog">The info log the driver returned for the failure.</param>
    /// <returns>The info log followed by the driver's version, renderer, and vendor.</returns>
    /// <remarks>
    /// A GLSL version mismatch surfaces as an ordinary compile error whose text varies by
    /// vendor, so the info log alone rarely identifies the cause. Reporting the driver's
    /// OpenGL and shading language versions alongside it makes that case self-evident.
    /// </remarks>
    internal static string DescribeShaderFailure(this IGraphicsDevice device, string infoLog)
        => $"{infoLog} (driver: OpenGL {device.GetDeviceInfo()})";
}
