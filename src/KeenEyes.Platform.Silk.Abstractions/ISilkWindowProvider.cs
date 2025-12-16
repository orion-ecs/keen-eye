using Silk.NET.Input;
using Silk.NET.Windowing;

namespace KeenEyes.Platform.Silk;

/// <summary>
/// Provides access to the shared Silk.NET window and input context.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables multiple Silk.NET-based plugins (graphics, input) to share
/// a single window instance without knowing about each other's implementation details.
/// </para>
/// <para>
/// Install <c>SilkWindowPlugin</c> to make this provider available. Both
/// <c>SilkGraphicsPlugin</c> and <c>SilkInputPlugin</c> require this to be installed first.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install window plugin first (required)
/// world.InstallPlugin(new SilkWindowPlugin(windowConfig));
///
/// // Then install graphics and/or input plugins
/// world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
/// world.InstallPlugin(new SilkInputPlugin(inputConfig));
///
/// // Both plugins share the same window via ISilkWindowProvider
/// </code>
/// </example>
public interface ISilkWindowProvider : IDisposable
{
    /// <summary>
    /// Gets the Silk.NET window for rendering operations.
    /// </summary>
    /// <remarks>
    /// Use this to create OpenGL/Vulkan contexts, get window dimensions,
    /// and hook into the window lifecycle events.
    /// </remarks>
    IWindow Window { get; }

    /// <summary>
    /// Gets the Silk.NET input context for keyboard, mouse, and gamepad access.
    /// </summary>
    /// <remarks>
    /// The input context is created from the window and provides access to all
    /// input devices. This is shared between all plugins that need input handling.
    /// </remarks>
    IInputContext InputContext { get; }
}
