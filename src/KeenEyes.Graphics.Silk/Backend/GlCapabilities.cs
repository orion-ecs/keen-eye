using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Backend;

/// <summary>
/// The OpenGL feature level this backend requires, and the check that enforces it.
/// </summary>
internal static class GlCapabilities
{
    /// <summary>
    /// The minimum OpenGL major version this backend runs on.
    /// </summary>
    /// <remarks>
    /// Every built-in shader declares <c>#version 330 core</c> - see
    /// <see cref="Rendering2D.Shaders2D"/> and <see cref="Shaders.DefaultShaders"/> - so OpenGL
    /// 3.3 is a hard requirement, not a preference. Raising the shaders raises this constant.
    /// </remarks>
    internal const int MinimumMajorVersion = 3;

    /// <summary>
    /// The minimum OpenGL minor version this backend runs on.
    /// </summary>
    /// <remarks>See <see cref="MinimumMajorVersion"/>.</remarks>
    internal const int MinimumMinorVersion = 3;

    /// <summary>
    /// Throws when the device's OpenGL version is below the engine's minimum.
    /// </summary>
    /// <param name="deviceInfo">The driver identity and version reported by the device.</param>
    /// <exception cref="UnsupportedGraphicsDeviceException">
    /// Thrown when the driver reports a version below
    /// <see cref="MinimumMajorVersion"/>.<see cref="MinimumMinorVersion"/>.
    /// </exception>
    internal static void EnsureMinimumVersion(GraphicsDeviceInfo deviceInfo)
    {
        if (deviceInfo.IsAtLeast(MinimumMajorVersion, MinimumMinorVersion))
        {
            return;
        }

        throw new UnsupportedGraphicsDeviceException(
            $"This machine's OpenGL driver is too old to run KeenEyes: detected " +
            $"{deviceInfo.MajorVersion}.{deviceInfo.MinorVersion}, but " +
            $"{MinimumMajorVersion}.{MinimumMinorVersion} or newer is required " +
            $"(every built-in shader is '#version 330 core'). " +
            $"Driver reports {deviceInfo}. " +
            "Install the graphics driver from your GPU vendor (NVIDIA, AMD, or Intel): the " +
            "Microsoft Basic Display Adapter software fallback, used when no vendor driver is " +
            "installed, reports OpenGL 1.1 - as do remote-desktop sessions.",
            deviceInfo);
    }
}
