using System.Text.Json;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles window state query commands.
/// </summary>
internal sealed class WindowCommandHandler(IWindowController windowController) : ICommandHandler
{
    public string Prefix => "window";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "isAvailable" => windowController.IsAvailable,
            "getState" => await windowController.GetStateAsync(),
            "getSize" => await HandleGetSizeAsync(),
            "getTitle" => await windowController.GetTitleAsync(),
            "isClosing" => await windowController.IsClosingAsync(),
            "isFocused" => await windowController.IsFocusedAsync(),
            "getAspectRatio" => await windowController.GetAspectRatioAsync(),
            _ => throw new InvalidOperationException($"Unknown window command: {command}")
        };
    }

    private async Task<WindowSizeResult> HandleGetSizeAsync()
    {
        var (width, height) = await windowController.GetSizeAsync();
        return new WindowSizeResult { Width = width, Height = height };
    }
}
