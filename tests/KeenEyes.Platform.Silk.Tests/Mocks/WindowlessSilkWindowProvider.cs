using Silk.NET.Input;
using Silk.NET.Windowing;

namespace KeenEyes.Platform.Silk.Tests.Mocks;

/// <summary>
/// An <see cref="ISilkWindowProvider"/> that owns no OS window and reports a sentinel exception
/// the moment anything reaches for one.
/// </summary>
/// <remarks>
/// This makes "the caller got as far as touching the window" observable in a headless test: a
/// <see cref="NotSupportedException"/> carrying <see cref="WindowAccessMessage"/> means execution
/// passed every check that runs before window creation.
/// </remarks>
internal sealed class WindowlessSilkWindowProvider : ISilkWindowProvider
{
    /// <summary>
    /// The message carried by the sentinel exception thrown from <see cref="Window"/>.
    /// </summary>
    internal const string WindowAccessMessage = "The test window provider owns no OS window.";

    public IWindow Window => throw new NotSupportedException(WindowAccessMessage);

    public IInputContext InputContext => throw new NotSupportedException(WindowAccessMessage);

    public int Width => 0;

    public int Height => 0;

    public int FramebufferWidth => 0;

    public int FramebufferHeight => 0;

#pragma warning disable CS0067 // Events exist to satisfy the interface; this provider raises none.
    public event Action? OnLoad;

    public event Action<double>? OnUpdate;

    public event Action<double>? OnRender;

    public event Action<int, int>? OnResize;

    public event Action<int, int>? OnFramebufferResize;

    public event Action? OnClosing;
#pragma warning restore CS0067

    public void Dispose()
    {
    }
}
