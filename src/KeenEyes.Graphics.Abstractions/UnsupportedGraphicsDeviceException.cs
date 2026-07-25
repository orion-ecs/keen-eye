namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Thrown when the graphics driver on the machine cannot support the engine's rendering
/// requirements, most commonly because its API version is too old.
/// </summary>
/// <remarks>
/// <para>
/// The backend raises this during graphics initialization, before it creates any shader or
/// renderer, so the failure names the real cause instead of surfacing later as an opaque shader
/// compilation error. <see cref="DeviceInfo"/> carries the driver's reported identity for
/// callers that want to present it themselves; the message already contains it.
/// </para>
/// </remarks>
/// <param name="message">A message stating what was detected, what is required, and how to fix it.</param>
/// <param name="deviceInfo">The driver identity and version that failed the check.</param>
public sealed class UnsupportedGraphicsDeviceException(string message, GraphicsDeviceInfo deviceInfo)
    : InvalidOperationException(message)
{
    /// <summary>
    /// Gets the driver identity and version that failed the check.
    /// </summary>
    public GraphicsDeviceInfo DeviceInfo { get; } = deviceInfo;
}
