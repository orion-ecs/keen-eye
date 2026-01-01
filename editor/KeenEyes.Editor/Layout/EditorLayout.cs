using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Layout;

/// <summary>
/// Represents a complete editor layout configuration that can be serialized.
/// </summary>
public sealed class EditorLayout
{
    /// <summary>
    /// Current layout file format version.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Gets or sets the layout format version.
    /// </summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Gets or sets the name of this layout.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the main window state.
    /// </summary>
    public WindowState Window { get; set; } = new();

    /// <summary>
    /// Gets or sets the dock layout configuration.
    /// </summary>
    public DockLayoutNode? DockLayout { get; set; }

    /// <summary>
    /// Gets or sets the panel states.
    /// </summary>
    public Dictionary<string, PanelState> Panels { get; set; } = [];

    /// <summary>
    /// Creates a default layout.
    /// </summary>
    public static EditorLayout CreateDefault()
    {
        return new EditorLayout
        {
            Name = "Default",
            Window = new WindowState
            {
                X = 100,
                Y = 100,
                Width = 1600,
                Height = 900,
                Maximized = false,
                Monitor = 0
            },
            DockLayout = new DockLayoutNode
            {
                Type = DockNodeType.Split,
                Direction = SplitDirection.Horizontal,
                Ratio = 0.2f,
                Children =
                [
                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "hierarchy" },
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Horizontal,
                        Ratio = 0.75f,
                        Children =
                        [
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "viewport" },
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "inspector" }
                        ]
                    }
                ]
            },
            Panels = new Dictionary<string, PanelState>
            {
                ["hierarchy"] = new() { Visible = true, Collapsed = false },
                ["inspector"] = new() { Visible = true, Collapsed = false },
                ["console"] = new() { Visible = true, Collapsed = true },
                ["project"] = new() { Visible = true, Collapsed = false },
                ["viewport"] = new() { Visible = true, Collapsed = false }
            }
        };
    }

    /// <summary>
    /// Creates a tall layout with vertical hierarchy.
    /// </summary>
    public static EditorLayout CreateTall()
    {
        return new EditorLayout
        {
            Name = "Tall",
            Window = new WindowState
            {
                X = 100,
                Y = 100,
                Width = 1200,
                Height = 1000,
                Maximized = false,
                Monitor = 0
            },
            DockLayout = new DockLayoutNode
            {
                Type = DockNodeType.Split,
                Direction = SplitDirection.Vertical,
                Ratio = 0.65f,
                Children =
                [
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Horizontal,
                        Ratio = 0.25f,
                        Children =
                        [
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "hierarchy" },
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "viewport" }
                        ]
                    },
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Horizontal,
                        Ratio = 0.5f,
                        Children =
                        [
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "inspector" },
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "console" }
                        ]
                    }
                ]
            },
            Panels = new Dictionary<string, PanelState>
            {
                ["hierarchy"] = new() { Visible = true, Collapsed = false },
                ["inspector"] = new() { Visible = true, Collapsed = false },
                ["console"] = new() { Visible = true, Collapsed = false },
                ["project"] = new() { Visible = true, Collapsed = false },
                ["viewport"] = new() { Visible = true, Collapsed = false }
            }
        };
    }

    /// <summary>
    /// Creates a wide layout with horizontal arrangement.
    /// </summary>
    public static EditorLayout CreateWide()
    {
        return new EditorLayout
        {
            Name = "Wide",
            Window = new WindowState
            {
                X = 50,
                Y = 50,
                Width = 1920,
                Height = 800,
                Maximized = false,
                Monitor = 0
            },
            DockLayout = new DockLayoutNode
            {
                Type = DockNodeType.Split,
                Direction = SplitDirection.Horizontal,
                Ratio = 0.15f,
                Children =
                [
                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "hierarchy" },
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Horizontal,
                        Ratio = 0.6f,
                        Children =
                        [
                            new DockLayoutNode
                            {
                                Type = DockNodeType.Split,
                                Direction = SplitDirection.Vertical,
                                Ratio = 0.8f,
                                Children =
                                [
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "viewport" },
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "console" }
                                ]
                            },
                            new DockLayoutNode
                            {
                                Type = DockNodeType.Split,
                                Direction = SplitDirection.Vertical,
                                Ratio = 0.5f,
                                Children =
                                [
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "inspector" },
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "project" }
                                ]
                            }
                        ]
                    }
                ]
            },
            Panels = new Dictionary<string, PanelState>
            {
                ["hierarchy"] = new() { Visible = true, Collapsed = false },
                ["inspector"] = new() { Visible = true, Collapsed = false },
                ["console"] = new() { Visible = true, Collapsed = false },
                ["project"] = new() { Visible = true, Collapsed = false },
                ["viewport"] = new() { Visible = true, Collapsed = false }
            }
        };
    }

    /// <summary>
    /// Creates a 2-column layout.
    /// </summary>
    public static EditorLayout Create2Column()
    {
        return new EditorLayout
        {
            Name = "2-Column",
            Window = new WindowState
            {
                X = 100,
                Y = 100,
                Width = 1600,
                Height = 900,
                Maximized = false,
                Monitor = 0
            },
            DockLayout = new DockLayoutNode
            {
                Type = DockNodeType.Split,
                Direction = SplitDirection.Horizontal,
                Ratio = 0.5f,
                Children =
                [
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Vertical,
                        Ratio = 0.7f,
                        Children =
                        [
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "hierarchy" },
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "console" }
                        ]
                    },
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Vertical,
                        Ratio = 0.6f,
                        Children =
                        [
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "viewport" },
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "inspector" }
                        ]
                    }
                ]
            },
            Panels = new Dictionary<string, PanelState>
            {
                ["hierarchy"] = new() { Visible = true, Collapsed = false },
                ["inspector"] = new() { Visible = true, Collapsed = false },
                ["console"] = new() { Visible = true, Collapsed = false },
                ["project"] = new() { Visible = false, Collapsed = false },
                ["viewport"] = new() { Visible = true, Collapsed = false }
            }
        };
    }

    /// <summary>
    /// Creates a 3-column layout.
    /// </summary>
    public static EditorLayout Create3Column()
    {
        return new EditorLayout
        {
            Name = "3-Column",
            Window = new WindowState
            {
                X = 50,
                Y = 50,
                Width = 1800,
                Height = 900,
                Maximized = false,
                Monitor = 0
            },
            DockLayout = new DockLayoutNode
            {
                Type = DockNodeType.Split,
                Direction = SplitDirection.Horizontal,
                Ratio = 0.2f,
                Children =
                [
                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "hierarchy" },
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Horizontal,
                        Ratio = 0.75f,
                        Children =
                        [
                            new DockLayoutNode
                            {
                                Type = DockNodeType.Split,
                                Direction = SplitDirection.Vertical,
                                Ratio = 0.8f,
                                Children =
                                [
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "viewport" },
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "console" }
                                ]
                            },
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "inspector" }
                        ]
                    }
                ]
            },
            Panels = new Dictionary<string, PanelState>
            {
                ["hierarchy"] = new() { Visible = true, Collapsed = false },
                ["inspector"] = new() { Visible = true, Collapsed = false },
                ["console"] = new() { Visible = true, Collapsed = false },
                ["project"] = new() { Visible = false, Collapsed = false },
                ["viewport"] = new() { Visible = true, Collapsed = false }
            }
        };
    }

    /// <summary>
    /// Creates a 4-column layout.
    /// </summary>
    public static EditorLayout Create4Column()
    {
        return new EditorLayout
        {
            Name = "4-Column",
            Window = new WindowState
            {
                X = 0,
                Y = 0,
                Width = 1920,
                Height = 1080,
                Maximized = true,
                Monitor = 0
            },
            DockLayout = new DockLayoutNode
            {
                Type = DockNodeType.Split,
                Direction = SplitDirection.Horizontal,
                Ratio = 0.2f,
                Children =
                [
                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "hierarchy" },
                    new DockLayoutNode
                    {
                        Type = DockNodeType.Split,
                        Direction = SplitDirection.Horizontal,
                        Ratio = 0.3f,
                        Children =
                        [
                            new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "project" },
                            new DockLayoutNode
                            {
                                Type = DockNodeType.Split,
                                Direction = SplitDirection.Horizontal,
                                Ratio = 0.7f,
                                Children =
                                [
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "viewport" },
                                    new DockLayoutNode { Type = DockNodeType.Panel, PanelId = "inspector" }
                                ]
                            }
                        ]
                    }
                ]
            },
            Panels = new Dictionary<string, PanelState>
            {
                ["hierarchy"] = new() { Visible = true, Collapsed = false },
                ["inspector"] = new() { Visible = true, Collapsed = false },
                ["console"] = new() { Visible = false, Collapsed = false },
                ["project"] = new() { Visible = true, Collapsed = false },
                ["viewport"] = new() { Visible = true, Collapsed = false }
            }
        };
    }

    /// <summary>
    /// Serializes this layout to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, GetJsonOptions());
    }

    /// <summary>
    /// Deserializes a layout from JSON.
    /// </summary>
    public static EditorLayout? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EditorLayout>(json, GetJsonOptions());
        }
        catch
        {
            return null;
        }
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

/// <summary>
/// Represents the main window state.
/// </summary>
public sealed class WindowState
{
    /// <summary>
    /// Window X position.
    /// </summary>
    public int X { get; set; } = 100;

    /// <summary>
    /// Window Y position.
    /// </summary>
    public int Y { get; set; } = 100;

    /// <summary>
    /// Window width.
    /// </summary>
    public int Width { get; set; } = 1600;

    /// <summary>
    /// Window height.
    /// </summary>
    public int Height { get; set; } = 900;

    /// <summary>
    /// Whether the window is maximized.
    /// </summary>
    public bool Maximized { get; set; }

    /// <summary>
    /// Which monitor the window is on (0-based).
    /// </summary>
    public int Monitor { get; set; }
}

/// <summary>
/// Represents a node in the dock layout tree.
/// </summary>
public sealed class DockLayoutNode
{
    /// <summary>
    /// The type of this node.
    /// </summary>
    public DockNodeType Type { get; set; } = DockNodeType.Panel;

    /// <summary>
    /// The panel ID if this is a panel node.
    /// </summary>
    public string? PanelId { get; set; }

    /// <summary>
    /// Split direction if this is a split node.
    /// </summary>
    public SplitDirection Direction { get; set; } = SplitDirection.Horizontal;

    /// <summary>
    /// Split ratio (0-1) for the first child.
    /// </summary>
    public float Ratio { get; set; } = 0.5f;

    /// <summary>
    /// Child nodes for split/tab nodes.
    /// </summary>
    public List<DockLayoutNode>? Children { get; set; }

    /// <summary>
    /// Tab index if this node is in a tab group.
    /// </summary>
    public int TabIndex { get; set; }
}

/// <summary>
/// Type of dock layout node.
/// </summary>
public enum DockNodeType
{
    /// <summary>
    /// A panel container.
    /// </summary>
    Panel,

    /// <summary>
    /// A split container with two children.
    /// </summary>
    Split,

    /// <summary>
    /// A tab container with multiple tabbed panels.
    /// </summary>
    Tabs
}

/// <summary>
/// Direction of a split.
/// </summary>
public enum SplitDirection
{
    /// <summary>
    /// Split horizontally (left/right).
    /// </summary>
    Horizontal,

    /// <summary>
    /// Split vertically (top/bottom).
    /// </summary>
    Vertical
}

/// <summary>
/// State of an individual panel.
/// </summary>
public sealed class PanelState
{
    /// <summary>
    /// Whether the panel is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether the panel is collapsed.
    /// </summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// Optional fixed width.
    /// </summary>
    public float? Width { get; set; }

    /// <summary>
    /// Optional fixed height.
    /// </summary>
    public float? Height { get; set; }
}
