using KeenEyes.Graph.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that processes context menu interactions for graph editing.
/// </summary>
/// <remarks>
/// <para>
/// Handles keyboard navigation (arrow keys, Enter, Escape), search filtering,
/// and menu item execution for context menus on graph canvases.
/// </para>
/// </remarks>
public sealed class GraphContextMenuSystem : SystemBase
{
    private IInputContext? inputContext;
    private GraphContext? graphContext;
    private PortRegistry? portRegistry;
    private NodeTypeRegistry? nodeTypeRegistry;

    // Key state for debouncing
    private readonly HashSet<Key> keysDownLastFrame = [];

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization
        if (inputContext is null && !World.TryGetExtension(out inputContext))
        {
            return;
        }

        if (graphContext is null && !World.TryGetExtension(out graphContext))
        {
            return;
        }

        if (portRegistry is null && !World.TryGetExtension(out portRegistry))
        {
            return;
        }

        if (nodeTypeRegistry is null)
        {
            World.TryGetExtension(out nodeTypeRegistry);
        }

        var keyboard = inputContext!.Keyboard;

        // Process each canvas with an active context menu
        foreach (var canvas in World.Query<GraphCanvas, GraphContextMenu, GraphCanvasTag>())
        {
            ProcessContextMenu(canvas, keyboard);
        }

        // Update key state for next frame
        UpdateKeyState(keyboard);
    }

    private void ProcessContextMenu(Entity canvas, IKeyboard keyboard)
    {
        ref var canvasData = ref World.Get<GraphCanvas>(canvas);
        ref var menu = ref World.Get<GraphContextMenu>(canvas);

        // Handle Escape - close menu
        if (WasKeyJustPressed(keyboard, Key.Escape))
        {
            CloseMenu(canvas, ref canvasData);
            return;
        }

        // Handle menu type-specific logic
        switch (menu.MenuType)
        {
            case ContextMenuType.Canvas:
                ProcessCanvasMenu(canvas, ref canvasData, ref menu, keyboard);
                break;

            case ContextMenuType.Node:
                ProcessNodeMenu(canvas, ref canvasData, ref menu, keyboard);
                break;

            case ContextMenuType.Connection:
                ProcessConnectionMenu(canvas, ref canvasData, ref menu, keyboard);
                break;
        }
    }

    private void ProcessCanvasMenu(Entity canvas, ref GraphCanvas canvasData, ref GraphContextMenu menu, IKeyboard keyboard)
    {
        // Get filtered node types
        var nodeTypes = GetFilteredNodeTypes(menu.SearchFilter);

        if (nodeTypes.Count == 0)
        {
            return;
        }

        // Handle arrow key navigation
        if (WasKeyJustPressed(keyboard, Key.Down))
        {
            menu.SelectedIndex = (menu.SelectedIndex + 1) % nodeTypes.Count;
        }
        else if (WasKeyJustPressed(keyboard, Key.Up))
        {
            menu.SelectedIndex = (menu.SelectedIndex - 1 + nodeTypes.Count) % nodeTypes.Count;
        }

        // Handle Enter - create selected node
        if (WasKeyJustPressed(keyboard, Key.Enter))
        {
            var selectedType = nodeTypes[menu.SelectedIndex];
            graphContext!.CreateNodeUndoable(canvas, selectedType.TypeId, menu.CanvasPosition);
            CloseMenu(canvas, ref canvasData);
        }

        // Handle alphanumeric input for search filtering
        UpdateSearchFilter(ref menu, keyboard);
    }

    private void ProcessNodeMenu(Entity canvas, ref GraphCanvas canvasData, ref GraphContextMenu menu, IKeyboard keyboard)
    {
        // Node menu options: Delete, Duplicate, Copy, Cut
        var options = new[] { "Delete", "Duplicate" };

        // Handle arrow key navigation
        if (WasKeyJustPressed(keyboard, Key.Down))
        {
            menu.SelectedIndex = (menu.SelectedIndex + 1) % options.Length;
        }
        else if (WasKeyJustPressed(keyboard, Key.Up))
        {
            menu.SelectedIndex = (menu.SelectedIndex - 1 + options.Length) % options.Length;
        }

        // Handle Enter - execute selected option
        if (WasKeyJustPressed(keyboard, Key.Enter))
        {
            var selectedOption = options[menu.SelectedIndex];

            switch (selectedOption)
            {
                case "Delete":
                    if (menu.TargetEntity.IsValid)
                    {
                        graphContext!.DeleteNodesUndoable([menu.TargetEntity]);
                    }
                    break;

                case "Duplicate":
                    if (menu.TargetEntity.IsValid)
                    {
                        graphContext!.DuplicateSelectionUndoable();
                    }
                    break;
            }

            CloseMenu(canvas, ref canvasData);
        }
    }

    private void ProcessConnectionMenu(Entity canvas, ref GraphCanvas canvasData, ref GraphContextMenu menu, IKeyboard keyboard)
    {
        // Handle Enter - delete connection
        if (WasKeyJustPressed(keyboard, Key.Enter))
        {
            if (menu.TargetEntity.IsValid)
            {
                graphContext!.DeleteConnectionUndoable(menu.TargetEntity);
            }

            CloseMenu(canvas, ref canvasData);
        }
    }

    private void CloseMenu(Entity canvas, ref GraphCanvas canvasData)
    {
        World.Remove<GraphContextMenu>(canvas);
        canvasData.Mode = GraphInteractionMode.None;
    }

    private List<PortRegistry.NodeTypeInfo> GetFilteredNodeTypes(string filter)
    {
        var allTypes = portRegistry!.GetAllNodeTypes().ToList();

        if (string.IsNullOrWhiteSpace(filter))
        {
            // When no filter, sort by category then by name for better organization
            return allTypes
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToList();
        }

        var lowerFilter = filter.ToLowerInvariant();
        return allTypes
            .Where(t => t.Name.ToLowerInvariant().Contains(lowerFilter) ||
                        t.Category.ToLowerInvariant().Contains(lowerFilter))
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// Gets node types organized by category for hierarchical display.
    /// </summary>
    /// <param name="filter">Optional search filter.</param>
    /// <returns>Dictionary of category name to list of node types in that category.</returns>
    internal Dictionary<string, List<PortRegistry.NodeTypeInfo>> GetNodeTypesByCategory(string filter)
    {
        var filtered = GetFilteredNodeTypes(filter);
        return filtered
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Gets all categories with registered node types.
    /// </summary>
    /// <returns>Sorted list of category names.</returns>
    internal IReadOnlyList<string> GetCategories()
    {
        if (nodeTypeRegistry is not null)
        {
            return nodeTypeRegistry.GetCategories().ToList();
        }

        return portRegistry?.GetCategories().ToList() ?? [];
    }

    private void UpdateSearchFilter(ref GraphContextMenu menu, IKeyboard keyboard)
    {
        // Handle backspace
        if (WasKeyJustPressed(keyboard, Key.Backspace) && menu.SearchFilter.Length > 0)
        {
            menu.SearchFilter = menu.SearchFilter[..^1];
            menu.SelectedIndex = 0; // Reset selection when filter changes
            return;
        }

        // Handle alphanumeric keys (simplified - in a real impl, use proper text input)
        for (var key = Key.A; key <= Key.Z; key++)
        {
            if (WasKeyJustPressed(keyboard, key))
            {
                var shift = (keyboard.Modifiers & KeyModifiers.Shift) != 0;
                var ch = shift ? (char)key : char.ToLowerInvariant((char)key);
                menu.SearchFilter += ch;
                menu.SelectedIndex = 0;
                return;
            }
        }

        // Handle number keys
        for (var key = Key.Number0; key <= Key.Number9; key++)
        {
            if (WasKeyJustPressed(keyboard, key))
            {
                var digit = (char)('0' + (key - Key.Number0));
                menu.SearchFilter += digit;
                menu.SelectedIndex = 0;
                return;
            }
        }

        // Handle space
        if (WasKeyJustPressed(keyboard, Key.Space))
        {
            menu.SearchFilter += ' ';
            menu.SelectedIndex = 0;
        }
    }

    private bool WasKeyJustPressed(IKeyboard keyboard, Key key)
    {
        var isDownNow = keyboard.IsKeyDown(key);
        var wasDownLastFrame = keysDownLastFrame.Contains(key);
        return isDownNow && !wasDownLastFrame;
    }

    // Keys that the context menu system cares about
    private static readonly Key[] trackedKeys =
    [
        Key.Escape,  // Close menu
        Key.Enter,   // Confirm selection
        Key.Up,      // Navigate up
        Key.Down,    // Navigate down
        Key.Left,    // Navigate left/collapse
        Key.Right,   // Navigate right/expand
    ];

    private void UpdateKeyState(IKeyboard keyboard)
    {
        keysDownLastFrame.Clear();

        // Only track keys relevant to context menu navigation
        foreach (var key in trackedKeys)
        {
            if (keyboard.IsKeyDown(key))
            {
                keysDownLastFrame.Add(key);
            }
        }
    }
}
