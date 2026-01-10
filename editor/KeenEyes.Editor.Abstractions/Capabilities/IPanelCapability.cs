using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for registering and managing editor panels.
/// Allows plugins to add custom panels to the editor layout.
/// </summary>
public interface IPanelCapability : IEditorCapability
{
    /// <summary>
    /// Registers a panel type with the editor.
    /// </summary>
    /// <typeparam name="T">The panel type.</typeparam>
    /// <param name="descriptor">The panel descriptor.</param>
    void RegisterPanel<T>(PanelDescriptor descriptor) where T : IEditorPanel, new();

    /// <summary>
    /// Registers a panel type with the editor using a factory.
    /// </summary>
    /// <param name="descriptor">The panel descriptor.</param>
    /// <param name="factory">Factory function to create the panel.</param>
    void RegisterPanel(PanelDescriptor descriptor, Func<IEditorPanel> factory);

    /// <summary>
    /// Opens a panel by its ID.
    /// </summary>
    /// <param name="id">The panel ID.</param>
    void OpenPanel(string id);

    /// <summary>
    /// Closes a panel by its ID.
    /// </summary>
    /// <param name="id">The panel ID.</param>
    void ClosePanel(string id);

    /// <summary>
    /// Checks if a panel is currently open.
    /// </summary>
    /// <param name="id">The panel ID.</param>
    /// <returns>True if the panel is open.</returns>
    bool IsPanelOpen(string id);

    /// <summary>
    /// Focuses a panel, bringing it to the front.
    /// </summary>
    /// <param name="id">The panel ID.</param>
    void FocusPanel(string id);

    /// <summary>
    /// Gets all registered panel descriptors.
    /// </summary>
    /// <returns>The panel descriptors.</returns>
    IEnumerable<PanelDescriptor> GetPanelDescriptors();

    /// <summary>
    /// Gets the IDs of all currently open panels.
    /// </summary>
    /// <returns>The open panel IDs.</returns>
    IEnumerable<string> GetOpenPanels();

    /// <summary>
    /// Event raised when a panel is opened.
    /// </summary>
    event Action<string>? PanelOpened;

    /// <summary>
    /// Event raised when a panel is closed.
    /// </summary>
    event Action<string>? PanelClosed;
}

/// <summary>
/// Describes a panel that can be registered with the editor.
/// </summary>
public sealed class PanelDescriptor
{
    /// <summary>
    /// Gets the unique identifier for this panel.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display title of the panel.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the optional icon identifier.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the default dock location for the panel.
    /// </summary>
    public PanelDockLocation DefaultLocation { get; init; } = PanelDockLocation.Right;

    /// <summary>
    /// Gets whether the panel should be open by default.
    /// </summary>
    public bool OpenByDefault { get; init; }

    /// <summary>
    /// Gets the minimum width of the panel.
    /// </summary>
    public float MinWidth { get; init; } = 200;

    /// <summary>
    /// Gets the minimum height of the panel.
    /// </summary>
    public float MinHeight { get; init; } = 100;

    /// <summary>
    /// Gets the default width of the panel.
    /// </summary>
    public float DefaultWidth { get; init; } = 300;

    /// <summary>
    /// Gets the default height of the panel.
    /// </summary>
    public float DefaultHeight { get; init; } = 400;

    /// <summary>
    /// Gets whether multiple instances of this panel can be opened.
    /// </summary>
    public bool AllowMultiple { get; init; }

    /// <summary>
    /// Gets the category for organizing panels in the View menu.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the optional keyboard shortcut to toggle the panel.
    /// </summary>
    public string? ToggleShortcut { get; init; }
}

/// <summary>
/// Default dock locations for panels.
/// </summary>
public enum PanelDockLocation
{
    /// <summary>
    /// Left side of the editor.
    /// </summary>
    Left,

    /// <summary>
    /// Right side of the editor.
    /// </summary>
    Right,

    /// <summary>
    /// Bottom of the editor.
    /// </summary>
    Bottom,

    /// <summary>
    /// Top of the editor.
    /// </summary>
    Top,

    /// <summary>
    /// Center area (main content).
    /// </summary>
    Center,

    /// <summary>
    /// Floating window.
    /// </summary>
    Floating
}

/// <summary>
/// Interface for editor panels.
/// </summary>
public interface IEditorPanel
{
    /// <summary>
    /// Called when the panel is created.
    /// </summary>
    /// <param name="context">The panel context.</param>
    void Initialize(PanelContext context);

    /// <summary>
    /// Called every frame to update the panel.
    /// </summary>
    /// <param name="deltaTime">Time since last update.</param>
    void Update(float deltaTime);

    /// <summary>
    /// Called when the panel is closed.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Gets the root UI entity of the panel.
    /// </summary>
    Entity RootEntity { get; }
}

/// <summary>
/// Context provided to panels during initialization.
/// </summary>
public sealed class PanelContext
{
    /// <summary>
    /// Gets the editor context for accessing editor services.
    /// </summary>
    public required IEditorContext EditorContext { get; init; }

    /// <summary>
    /// Gets the editor world for creating UI entities.
    /// </summary>
    public required IWorld EditorWorld { get; init; }

    /// <summary>
    /// Gets the parent entity to add panel content to.
    /// </summary>
    public required Entity Parent { get; init; }

    /// <summary>
    /// Gets the panel descriptor.
    /// </summary>
    public required PanelDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the default font handle for creating UI text.
    /// </summary>
    public required FontHandle Font { get; init; }
}
