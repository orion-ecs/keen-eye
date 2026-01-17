using System.Text.Json;
using KeenEyes.TestBridge.UI;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles UI debugging commands for element inspection, focus management, and interaction.
/// </summary>
internal sealed class UICommandHandler(IUIController uiController) : ICommandHandler
{
    public string Prefix => "ui";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Statistics
            "getStatistics" => await uiController.GetStatisticsAsync(cancellationToken),

            // Focus Management
            "getFocused" => await uiController.GetFocusedElementAsync(cancellationToken),
            "setFocus" => await HandleSetFocusAsync(args, cancellationToken),
            "clearFocus" => await uiController.ClearFocusAsync(cancellationToken),

            // Element Inspection
            "getElement" => await HandleGetElementAsync(args, cancellationToken),
            "getTree" => await HandleGetTreeAsync(args, cancellationToken),
            "getRoots" => await uiController.GetRootElementsAsync(cancellationToken),
            "getBounds" => await HandleGetBoundsAsync(args, cancellationToken),
            "getStyle" => await HandleGetStyleAsync(args, cancellationToken),
            "getInteraction" => await HandleGetInteractionAsync(args, cancellationToken),

            // Hit Testing
            "hitTest" => await HandleHitTestAsync(args, cancellationToken),
            "hitTestAll" => await HandleHitTestAllAsync(args, cancellationToken),

            // Element Search
            "findByName" => await HandleFindByNameAsync(args, cancellationToken),

            // Interaction Simulation
            "simulateClick" => await HandleSimulateClickAsync(args, cancellationToken),

            _ => throw new InvalidOperationException($"Unknown UI command: {command}")
        };
    }

    #region Focus Management Handlers

    private async Task<bool> HandleSetFocusAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await uiController.SetFocusAsync(entityId, cancellationToken);
    }

    #endregion

    #region Element Inspection Handlers

    private async Task<UIElementSnapshot?> HandleGetElementAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await uiController.GetElementAsync(entityId, cancellationToken);
    }

    private async Task<IReadOnlyList<UIElementSnapshot>> HandleGetTreeAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var rootEntityId = GetOptionalInt(args, "rootEntityId");
        return await uiController.GetElementTreeAsync(rootEntityId, cancellationToken);
    }

    private async Task<UIBoundsSnapshot?> HandleGetBoundsAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await uiController.GetElementBoundsAsync(entityId, cancellationToken);
    }

    private async Task<UIStyleSnapshot?> HandleGetStyleAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await uiController.GetElementStyleAsync(entityId, cancellationToken);
    }

    private async Task<UIInteractionSnapshot?> HandleGetInteractionAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await uiController.GetInteractionStateAsync(entityId, cancellationToken);
    }

    #endregion

    #region Hit Testing Handlers

    private async Task<int?> HandleHitTestAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        return await uiController.HitTestAsync(x, y, cancellationToken);
    }

    private async Task<IReadOnlyList<int>> HandleHitTestAllAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        return await uiController.HitTestAllAsync(x, y, cancellationToken);
    }

    #endregion

    #region Element Search Handlers

    private async Task<int?> HandleFindByNameAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var name = GetRequiredString(args, "name");
        return await uiController.FindElementByNameAsync(name, cancellationToken);
    }

    #endregion

    #region Interaction Simulation Handlers

    private async Task<bool> HandleSimulateClickAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await uiController.SimulateClickAsync(entityId, cancellationToken);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static int? GetOptionalInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetInt32();
    }

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetSingle();
    }

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    #endregion
}
