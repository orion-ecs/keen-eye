using Silk.NET.Input;
using Silk.NET.Windowing;

namespace KeenEyes.Platform.Silk;

/// <summary>
/// Provides access to the shared Silk.NET window, input context, and lifecycle events.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables multiple Silk.NET-based plugins (graphics, input) to share
/// a single window instance without knowing about each other's implementation details.
/// </para>
/// <para>
/// Plugins can subscribe to lifecycle events (OnLoad, OnUpdate, etc.) to hook into
/// the window's event loop without needing direct access to the window.
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
/// // Plugins can subscribe to window lifecycle events
/// windowProvider.OnLoad += () => InitializeResources();
/// windowProvider.OnUpdate += deltaTime => UpdateLogic(deltaTime);
/// </code>
/// </example>
public interface ISilkWindowProvider : IDisposable
{
    /// <summary>
    /// Gets the Silk.NET window for rendering operations.
    /// </summary>
    /// <remarks>
    /// Use this to create OpenGL/Vulkan contexts and get window dimensions.
    /// For lifecycle events, prefer using the event properties on this interface.
    /// </remarks>
    IWindow Window { get; }

    /// <summary>
    /// Gets the Silk.NET input context for keyboard, mouse, and gamepad access.
    /// </summary>
    /// <remarks>
    /// The input context is created from the window and provides access to all
    /// input devices. This is shared between all plugins that need input handling.
    /// Available after the <see cref="OnLoad"/> event fires.
    /// </remarks>
    IInputContext InputContext { get; }

    /// <summary>
    /// Raised once when the window has loaded and is ready for use.
    /// </summary>
    /// <remarks>
    /// This is the appropriate time to create GPU resources, initialize contexts,
    /// and perform other setup that requires the window to be ready.
    /// </remarks>
    event Action? OnLoad;

    /// <summary>
    /// Raised each frame for update logic.
    /// </summary>
    /// <remarks>
    /// The parameter is the delta time in seconds since the last update.
    /// </remarks>
    event Action<double>? OnUpdate;

    /// <summary>
    /// Raised each frame for rendering.
    /// </summary>
    /// <remarks>
    /// The parameter is the delta time in seconds since the last render.
    /// </remarks>
    event Action<double>? OnRender;

    /// <summary>
    /// Raised when the window is resized.
    /// </summary>
    /// <remarks>
    /// Parameters are the new width and height in pixels.
    /// </remarks>
    event Action<int, int>? OnResize;

    /// <summary>
    /// Raised when the window is closing.
    /// </summary>
    /// <remarks>
    /// This is the appropriate time to dispose GPU resources while the
    /// graphics context is still valid.
    /// </remarks>
    event Action? OnClosing;
}
