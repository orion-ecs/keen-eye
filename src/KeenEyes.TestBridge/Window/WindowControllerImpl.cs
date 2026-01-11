using KeenEyes.Graphics.Abstractions;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.WindowImpl;

/// <summary>
/// In-process implementation of <see cref="IWindowController"/>.
/// </summary>
/// <remarks>
/// This implementation accesses the <see cref="IWindow"/> directly for
/// in-process testing scenarios.
/// </remarks>
/// <param name="window">The window to query, or null if unavailable.</param>
internal sealed class WindowControllerImpl(IWindow? window) : IWindowController
{
    /// <inheritdoc />
    public bool IsAvailable => window != null;

    /// <inheritdoc />
    public Task<WindowStateSnapshot> GetStateAsync()
    {
        if (window == null)
        {
            return Task.FromResult(CreateUnavailableSnapshot());
        }

        return Task.FromResult(new WindowStateSnapshot
        {
            Width = window.Width,
            Height = window.Height,
            Title = window.Title,
            IsClosing = window.IsClosing,
            IsFocused = window.IsFocused,
            AspectRatio = window.AspectRatio
        });
    }

    /// <inheritdoc />
    public Task<(int Width, int Height)> GetSizeAsync()
    {
        if (window == null)
        {
            return Task.FromResult((0, 0));
        }

        return Task.FromResult((window.Width, window.Height));
    }

    /// <inheritdoc />
    public Task<string> GetTitleAsync()
    {
        return Task.FromResult(window?.Title ?? string.Empty);
    }

    /// <inheritdoc />
    public Task<bool> IsClosingAsync()
    {
        return Task.FromResult(window?.IsClosing ?? false);
    }

    /// <inheritdoc />
    public Task<bool> IsFocusedAsync()
    {
        return Task.FromResult(window?.IsFocused ?? false);
    }

    /// <inheritdoc />
    public Task<float> GetAspectRatioAsync()
    {
        return Task.FromResult(window?.AspectRatio ?? 0f);
    }

    private static WindowStateSnapshot CreateUnavailableSnapshot()
    {
        return new WindowStateSnapshot
        {
            Width = 0,
            Height = 0,
            Title = string.Empty,
            IsClosing = false,
            IsFocused = false,
            AspectRatio = 0f
        };
    }
}
