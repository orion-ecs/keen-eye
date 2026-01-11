using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IWindowController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteWindowController(TestBridgeClient client) : IWindowController
{
    /// <inheritdoc />
    public bool IsAvailable => client.SendRequestAsync<bool>("window.isAvailable", null, CancellationToken.None)
        .GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<WindowStateSnapshot> GetStateAsync()
    {
        var result = await client.SendRequestAsync<WindowStateSnapshot>("window.getState", null, CancellationToken.None);
        return result ?? throw new InvalidOperationException("Failed to get window state");
    }

    /// <inheritdoc />
    public async Task<(int Width, int Height)> GetSizeAsync()
    {
        var result = await client.SendRequestAsync<WindowSizeResult>("window.getSize", null, CancellationToken.None);
        if (result == null)
        {
            return (0, 0);
        }

        return (result.Width, result.Height);
    }

    /// <inheritdoc />
    public async Task<string> GetTitleAsync()
    {
        var result = await client.SendRequestAsync<string>("window.getTitle", null, CancellationToken.None);
        return result ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<bool> IsClosingAsync()
    {
        return await client.SendRequestAsync<bool>("window.isClosing", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<bool> IsFocusedAsync()
    {
        return await client.SendRequestAsync<bool>("window.isFocused", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<float> GetAspectRatioAsync()
    {
        return await client.SendRequestAsync<float>("window.getAspectRatio", null, CancellationToken.None);
    }
}
