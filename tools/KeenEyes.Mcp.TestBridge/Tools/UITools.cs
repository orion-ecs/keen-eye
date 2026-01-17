using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.UI;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for UI debugging: element inspection, focus management, hit testing, and interaction.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose the UIPlugin debugging infrastructure via MCP, allowing inspection
/// and manipulation of UI elements in running games.
/// </para>
/// <para>
/// Note: These tools require the UIPlugin to be installed in the target world.
/// Entities must have UIElement component for the operations to work.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class UITools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "ui_get_statistics")]
    [Description("Get overall UI statistics including counts of total elements, visible elements, and interactable elements.")]
    public async Task<UIStatisticsResult> GetStatistics()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.UI.GetStatisticsAsync();
        return UIStatisticsResult.FromSnapshot(stats);
    }

    #endregion

    #region Focus Management

    [McpServerTool(Name = "ui_get_focused")]
    [Description("Get the currently focused UI element.")]
    public async Task<FocusedElementResult> GetFocusedElement()
    {
        var bridge = connection.GetBridge();
        var focusedId = await bridge.UI.GetFocusedElementAsync();

        if (focusedId == null)
        {
            return new FocusedElementResult
            {
                Success = true,
                EntityId = null
            };
        }

        return new FocusedElementResult
        {
            Success = true,
            EntityId = focusedId
        };
    }

    [McpServerTool(Name = "ui_set_focus")]
    [Description("Set keyboard focus to a specific UI element.")]
    public async Task<OperationResult> SetFocus(
        [Description("The entity ID to focus")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.UI.SetFocusAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set focus on entity {entityId}. Entity may not exist or may not be focusable."
        };
    }

    [McpServerTool(Name = "ui_clear_focus")]
    [Description("Clear focus from the currently focused UI element.")]
    public async Task<OperationResult> ClearFocus()
    {
        var bridge = connection.GetBridge();
        var success = await bridge.UI.ClearFocusAsync();
        return new OperationResult
        {
            Success = success,
            Error = success ? null : "No element is currently focused."
        };
    }

    #endregion

    #region Element Inspection

    [McpServerTool(Name = "ui_get_element")]
    [Description("Get detailed information about a specific UI element.")]
    public async Task<UIElementResult> GetElement(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var element = await bridge.UI.GetElementAsync(entityId);

        if (element == null)
        {
            return new UIElementResult
            {
                Success = false,
                Error = $"No UI element found on entity {entityId}"
            };
        }

        return UIElementResult.FromSnapshot(element);
    }

    [McpServerTool(Name = "ui_get_element_tree")]
    [Description("Get the UI element hierarchy starting from a root (or all roots if not specified).")]
    public async Task<UIElementTreeResult> GetElementTree(
        [Description("The root entity ID, or null for all roots")]
        int? rootEntityId = null)
    {
        var bridge = connection.GetBridge();
        var elements = await bridge.UI.GetElementTreeAsync(rootEntityId);
        return new UIElementTreeResult
        {
            Success = true,
            Elements = elements,
            Count = elements.Count
        };
    }

    [McpServerTool(Name = "ui_get_roots")]
    [Description("Get all root UI elements (canvases).")]
    public async Task<EntityListResult> GetRootElements()
    {
        var bridge = connection.GetBridge();
        var roots = await bridge.UI.GetRootElementsAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = roots,
            Count = roots.Count
        };
    }

    [McpServerTool(Name = "ui_get_bounds")]
    [Description("Get the screen-space bounds of a UI element.")]
    public async Task<UIBoundsResult> GetElementBounds(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var bounds = await bridge.UI.GetElementBoundsAsync(entityId);

        if (bounds == null)
        {
            return new UIBoundsResult
            {
                Success = false,
                Error = $"No UIRect component found on entity {entityId}"
            };
        }

        return UIBoundsResult.FromSnapshot(bounds);
    }

    [McpServerTool(Name = "ui_get_style")]
    [Description("Get the visual style of a UI element (colors, borders, padding).")]
    public async Task<UIStyleResult> GetElementStyle(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var style = await bridge.UI.GetElementStyleAsync(entityId);

        if (style == null)
        {
            return new UIStyleResult
            {
                Success = false,
                Error = $"No UIStyle component found on entity {entityId}"
            };
        }

        return UIStyleResult.FromSnapshot(style);
    }

    [McpServerTool(Name = "ui_get_interaction")]
    [Description("Get the interaction state of a UI element (hover, pressed, focused, dragging).")]
    public async Task<UIInteractionResult> GetInteractionState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var interaction = await bridge.UI.GetInteractionStateAsync(entityId);

        if (interaction == null)
        {
            return new UIInteractionResult
            {
                Success = false,
                Error = $"No UIInteractable component found on entity {entityId}"
            };
        }

        return UIInteractionResult.FromSnapshot(interaction);
    }

    #endregion

    #region Hit Testing

    [McpServerTool(Name = "ui_hit_test")]
    [Description("Find the topmost UI element at a screen position.")]
    public async Task<HitTestResult> HitTest(
        [Description("The X screen coordinate")]
        float x,
        [Description("The Y screen coordinate")]
        float y)
    {
        var bridge = connection.GetBridge();
        var entityId = await bridge.UI.HitTestAsync(x, y);

        return new HitTestResult
        {
            Success = true,
            X = x,
            Y = y,
            EntityId = entityId
        };
    }

    [McpServerTool(Name = "ui_hit_test_all")]
    [Description("Find all UI elements at a screen position, sorted topmost first.")]
    public async Task<HitTestAllResult> HitTestAll(
        [Description("The X screen coordinate")]
        float x,
        [Description("The Y screen coordinate")]
        float y)
    {
        var bridge = connection.GetBridge();
        var entityIds = await bridge.UI.HitTestAllAsync(x, y);

        return new HitTestAllResult
        {
            Success = true,
            X = x,
            Y = y,
            EntityIds = entityIds,
            Count = entityIds.Count
        };
    }

    #endregion

    #region Element Search

    [McpServerTool(Name = "ui_find_by_name")]
    [Description("Find a UI element by its name.")]
    public async Task<FindElementResult> FindElementByName(
        [Description("The element name to search for")]
        string name)
    {
        var bridge = connection.GetBridge();
        var entityId = await bridge.UI.FindElementByNameAsync(name);

        if (entityId == null)
        {
            return new FindElementResult
            {
                Success = false,
                Error = $"No UI element found with name '{name}'"
            };
        }

        return new FindElementResult
        {
            Success = true,
            Name = name,
            EntityId = entityId.Value
        };
    }

    #endregion

    #region Interaction Simulation

    [McpServerTool(Name = "ui_simulate_click")]
    [Description("Simulate a click on a UI element.")]
    public async Task<OperationResult> SimulateClick(
        [Description("The entity ID to click")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.UI.SimulateClickAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to simulate click on entity {entityId}. Entity may not exist or may not be clickable."
        };
    }

    #endregion
}

#region Result Types

/// <summary>
/// Result for UI statistics.
/// </summary>
public sealed record UIStatisticsResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the total number of UI elements.
    /// </summary>
    public int TotalElementCount { get; init; }

    /// <summary>
    /// Gets the number of visible UI elements.
    /// </summary>
    public int VisibleElementCount { get; init; }

    /// <summary>
    /// Gets the number of interactable UI elements.
    /// </summary>
    public int InteractableCount { get; init; }

    /// <summary>
    /// Gets the entity ID of the currently focused element, or null if none.
    /// </summary>
    public int? FocusedElementId { get; init; }

    internal static UIStatisticsResult FromSnapshot(UIStatisticsSnapshot snapshot)
    {
        return new UIStatisticsResult
        {
            TotalElementCount = snapshot.TotalElementCount,
            VisibleElementCount = snapshot.VisibleElementCount,
            InteractableCount = snapshot.InteractableCount,
            FocusedElementId = snapshot.FocusedElementId
        };
    }
}

/// <summary>
/// Result for focused element query.
/// </summary>
public sealed record FocusedElementResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID of the focused element, or null if none.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for UI element query.
/// </summary>
public sealed record UIElementResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the element name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets whether the element is visible.
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Gets whether the element can receive pointer events.
    /// </summary>
    public bool IsRaycastTarget { get; init; }

    /// <summary>
    /// Gets the parent entity ID, or null if root.
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// Gets the child entity IDs.
    /// </summary>
    public IReadOnlyList<int>? ChildIds { get; init; }

    /// <summary>
    /// Gets whether the element has a UIInteractable component.
    /// </summary>
    public bool HasInteractable { get; init; }

    /// <summary>
    /// Gets whether the element has a UIText component.
    /// </summary>
    public bool HasText { get; init; }

    /// <summary>
    /// Gets whether the element has a UIStyle component.
    /// </summary>
    public bool HasStyle { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static UIElementResult FromSnapshot(UIElementSnapshot snapshot)
    {
        return new UIElementResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            Name = snapshot.Name,
            IsVisible = snapshot.IsVisible,
            IsRaycastTarget = snapshot.IsRaycastTarget,
            ParentId = snapshot.ParentId,
            ChildIds = snapshot.ChildIds,
            HasInteractable = snapshot.HasInteractable,
            HasText = snapshot.HasText,
            HasStyle = snapshot.HasStyle
        };
    }
}

/// <summary>
/// Result for UI element tree query.
/// </summary>
public sealed record UIElementTreeResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the UI elements in hierarchical order.
    /// </summary>
    public IReadOnlyList<UIElementSnapshot>? Elements { get; init; }

    /// <summary>
    /// Gets the count of elements.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for UI bounds query.
/// </summary>
public sealed record UIBoundsResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the X position in screen coordinates.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y position in screen coordinates.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// Gets the local Z-index for render ordering.
    /// </summary>
    public short LocalZIndex { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static UIBoundsResult FromSnapshot(UIBoundsSnapshot snapshot)
    {
        return new UIBoundsResult
        {
            Success = true,
            X = snapshot.X,
            Y = snapshot.Y,
            Width = snapshot.Width,
            Height = snapshot.Height,
            LocalZIndex = snapshot.LocalZIndex
        };
    }
}

/// <summary>
/// Result for UI style query.
/// </summary>
public sealed record UIStyleResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the background color red component (0-1).
    /// </summary>
    public float BackgroundColorR { get; init; }

    /// <summary>
    /// Gets the background color green component (0-1).
    /// </summary>
    public float BackgroundColorG { get; init; }

    /// <summary>
    /// Gets the background color blue component (0-1).
    /// </summary>
    public float BackgroundColorB { get; init; }

    /// <summary>
    /// Gets the background color alpha component (0-1).
    /// </summary>
    public float BackgroundColorA { get; init; }

    /// <summary>
    /// Gets the border width in pixels.
    /// </summary>
    public float BorderWidth { get; init; }

    /// <summary>
    /// Gets the corner radius for rounded rectangles.
    /// </summary>
    public float CornerRadius { get; init; }

    /// <summary>
    /// Gets the left padding in pixels.
    /// </summary>
    public float PaddingLeft { get; init; }

    /// <summary>
    /// Gets the right padding in pixels.
    /// </summary>
    public float PaddingRight { get; init; }

    /// <summary>
    /// Gets the top padding in pixels.
    /// </summary>
    public float PaddingTop { get; init; }

    /// <summary>
    /// Gets the bottom padding in pixels.
    /// </summary>
    public float PaddingBottom { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static UIStyleResult FromSnapshot(UIStyleSnapshot snapshot)
    {
        return new UIStyleResult
        {
            Success = true,
            BackgroundColorR = snapshot.BackgroundColorR,
            BackgroundColorG = snapshot.BackgroundColorG,
            BackgroundColorB = snapshot.BackgroundColorB,
            BackgroundColorA = snapshot.BackgroundColorA,
            BorderWidth = snapshot.BorderWidth,
            CornerRadius = snapshot.CornerRadius,
            PaddingLeft = snapshot.PaddingLeft,
            PaddingRight = snapshot.PaddingRight,
            PaddingTop = snapshot.PaddingTop,
            PaddingBottom = snapshot.PaddingBottom
        };
    }
}

/// <summary>
/// Result for UI interaction state query.
/// </summary>
public sealed record UIInteractionResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets whether the element can receive keyboard focus.
    /// </summary>
    public bool CanFocus { get; init; }

    /// <summary>
    /// Gets whether the element responds to click/tap events.
    /// </summary>
    public bool CanClick { get; init; }

    /// <summary>
    /// Gets whether the element can be dragged.
    /// </summary>
    public bool CanDrag { get; init; }

    /// <summary>
    /// Gets whether the element is currently hovered.
    /// </summary>
    public bool IsHovered { get; init; }

    /// <summary>
    /// Gets whether the element is currently pressed.
    /// </summary>
    public bool IsPressed { get; init; }

    /// <summary>
    /// Gets whether the element currently has focus.
    /// </summary>
    public bool IsFocused { get; init; }

    /// <summary>
    /// Gets whether the element is being dragged.
    /// </summary>
    public bool IsDragging { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static UIInteractionResult FromSnapshot(UIInteractionSnapshot snapshot)
    {
        return new UIInteractionResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            CanFocus = snapshot.CanFocus,
            CanClick = snapshot.CanClick,
            CanDrag = snapshot.CanDrag,
            IsHovered = snapshot.IsHovered,
            IsPressed = snapshot.IsPressed,
            IsFocused = snapshot.IsFocused,
            IsDragging = snapshot.IsDragging
        };
    }
}

/// <summary>
/// Result for hit test query.
/// </summary>
public sealed record HitTestResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the X screen coordinate tested.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y screen coordinate tested.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Gets the entity ID at the position, or null if none.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for hit test all query.
/// </summary>
public sealed record HitTestAllResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the X screen coordinate tested.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y screen coordinate tested.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Gets the entity IDs at the position, sorted topmost first.
    /// </summary>
    public IReadOnlyList<int>? EntityIds { get; init; }

    /// <summary>
    /// Gets the count of entities found.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for element search by name.
/// </summary>
public sealed record FindElementResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the name that was searched for.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the entity ID of the found element.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
