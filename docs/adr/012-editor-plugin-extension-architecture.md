# ADR-012: Editor Plugin Extension Architecture

**Status:** Proposed
**Date:** 2026-01-01

## Context

The KeenEyes Editor is being built as a modular, plugin-based application. Currently, editor features like the Inspector, Hierarchy, and Viewport panels are implemented as static classes tightly coupled to `EditorApplication`. This creates several problems:

### Limited Extensibility
Third-party developers cannot add custom panels, property drawers, gizmos, or menu items without modifying editor source code.

### Monolithic Structure
All editor features are bundled together. Users cannot disable unused features or load only what they need.

### Testing Difficulty
Editor components are hard to test in isolation because they depend on the full `EditorApplication` context.

### Inconsistent Patterns
The runtime `IWorldPlugin` system provides a clean capability-based pattern (ADR-007), but the editor has no equivalent architecture.

## Decision

Introduce `IEditorPlugin` and `IEditorContext` interfaces that mirror the runtime plugin architecture, extended with editor-specific capabilities.

### Core Interfaces

```csharp
public interface IEditorPlugin
{
    string Name { get; }
    string Version { get; }
    string? Description { get; }

    void Initialize(IEditorContext context);
    void Shutdown();
}

public interface IEditorContext
{
    // Core services (read-only access)
    EditorProject Project { get; }
    EditorWorldManager Worlds { get; }
    SelectionManager Selection { get; }
    UndoRedoManager UndoRedo { get; }
    AssetDatabase Assets { get; }
    IWorld EditorWorld { get; }

    // Extension storage (mirrors IPluginContext)
    void SetExtension<T>(T extension) where T : class;
    T GetExtension<T>() where T : class;
    bool TryGetExtension<T>(out T? extension) where T : class;
    bool RemoveExtension<T>() where T : class;

    // Capability access (mirrors ADR-007)
    T GetCapability<T>() where T : class, IEditorCapability;
    bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability;
    bool HasCapability<T>() where T : class, IEditorCapability;

    // Event subscriptions
    EventSubscription OnSceneOpened(Action<World> handler);
    EventSubscription OnSceneClosed(Action handler);
    EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler);
    EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler);
}
```

### Editor Capabilities

Following ADR-007's pattern, define editor features as capability interfaces:

| Capability | Purpose |
|------------|---------|
| `IInspectorCapability` | Register property drawers, component inspectors |
| `IViewportCapability` | Add gizmo renderers, overlays, pick handlers |
| `IMenuCapability` | Add menu items, context menus, toolbar buttons |
| `IPanelCapability` | Register dockable panels |
| `IAssetCapability` | Custom asset importers, thumbnails |
| `IShortcutCapability` | Register keyboard shortcuts |
| `IToolCapability` | Register viewport tools (select, move, etc.) |

### Capability Interface Definitions

#### IInspectorCapability

```csharp
public interface IInspectorCapability : IEditorCapability
{
    void RegisterPropertyDrawer(Type fieldType, PropertyDrawer drawer);
    void RegisterPropertyDrawer<T>(PropertyDrawer drawer);
    void RegisterDrawerForAttribute<TAttribute>(PropertyDrawer drawer)
        where TAttribute : Attribute;
    void RegisterComponentInspector<TComponent>(IComponentInspector inspector);
    void RegisterComponentActions<TComponent>(IComponentActionProvider provider);
}
```

#### IViewportCapability

```csharp
public interface IViewportCapability : IEditorCapability
{
    void AddGizmoRenderer(IGizmoRenderer renderer);
    void RemoveGizmoRenderer(IGizmoRenderer renderer);
    void AddOverlay(string id, IViewportOverlay overlay);
    void SetOverlayVisible(string id, bool visible);
    void AddPickHandler(IPickHandler handler);
    void RegisterCameraMode(string id, ICameraMode mode);
}

public interface IGizmoRenderer
{
    int Order { get; }
    bool IsVisible { get; }
    void Render(GizmoRenderContext context, IReadOnlyList<Entity> selection);
}
```

#### IMenuCapability

```csharp
public interface IMenuCapability : IEditorCapability
{
    void AddMenuItem(MenuPath path, EditorCommand command);
    void AddContextMenuItem<T>(MenuPath path, EditorCommand<T> command);
    void AddToolbarButton(ToolbarSection section, EditorCommand command);
    void RemoveMenuItem(MenuPath path);
}

public record MenuPath(string Path)
{
    public static MenuPath File(string item) => new($"File/{item}");
    public static MenuPath Edit(string item) => new($"Edit/{item}");
    public static MenuPath Entity(string item) => new($"Entity/{item}");
    public static MenuPath Window(string item) => new($"Window/{item}");
}
```

#### IPanelCapability

```csharp
public interface IPanelCapability : IEditorCapability
{
    void RegisterPanel<T>(PanelDescriptor descriptor) where T : IEditorPanel, new();
    void OpenPanel(string id);
    void ClosePanel(string id);
    bool IsPanelOpen(string id);
}

public interface IEditorPanel : IDisposable
{
    string Title { get; }
    Entity CreateUI(IWorld editorWorld, Entity parent, FontHandle font);
    void Update(float deltaTime);
}

public record PanelDescriptor(
    string Id,
    string Title,
    DockPosition DefaultPosition = DockPosition.Right,
    bool ShowByDefault = false,
    MenuPath? WindowMenuItem = null
);
```

### Source-Generated Extensions

Following the `PluginExtensionAttribute` pattern, provide typed access to editor extensions:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class EditorExtensionAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; } = propertyName;
}

// Usage:
[EditorExtension("Physics")]
public sealed class PhysicsEditorExtension
{
    public void ShowColliderBounds(bool visible) { ... }
}

// Generated extension member:
extension(IEditorContext context)
{
    public PhysicsEditorExtension Physics
        => context.GetExtension<PhysicsEditorExtension>();
}
```

### Plugin Lifecycle

```
EditorApplication starts
    ↓
Load plugin assemblies
    ↓
For each IEditorPlugin:
    ├── Create IEditorContext
    ├── Call plugin.Initialize(context)
    └── Track registered resources
    ↓
Editor runs
    ↓
On shutdown:
    ├── For each plugin (reverse order):
    │   └── Call plugin.Shutdown()
    └── Dispose tracked resources
```

### Built-in Plugins

Core editor features are refactored as internal plugins:

| Plugin | Provides |
|--------|----------|
| `CoreEditorPlugin` | Selection, undo/redo, basic commands |
| `InspectorPlugin` | Component inspector, built-in property drawers |
| `HierarchyPlugin` | Scene tree panel |
| `ViewportPlugin` | 3D/2D viewport, transform gizmos, grid |
| `ConsolePlugin` | Log panel |
| `ProfilerPlugin` | System timing panel |
| `ProjectPlugin` | Asset browser panel |

This serves as reference implementations for third-party plugins.

## Consequences

### Positive

1. **Extensibility** - Third parties can add panels, drawers, gizmos, menu items
2. **Modularity** - Editor features are isolated and independently testable
3. **Consistency** - Same capability pattern as runtime plugins (ADR-007)
4. **Discoverability** - Capability interfaces document available extension points
5. **Clean shutdown** - Plugin resources are tracked and disposed properly

### Negative

1. **Migration effort** - Existing editor code needs refactoring to plugin pattern
2. **Indirection** - Accessing features requires capability lookup
3. **Learning curve** - Plugin authors must understand capability system

### Neutral

1. **Performance** - Interface dispatch overhead is negligible for editor code
2. **Gradual adoption** - Can migrate panels one at a time

## Implementation Phases

### Phase 1: Core Abstractions
- Create `IEditorPlugin`, `IEditorContext` interfaces
- Create `IEditorCapability` marker interface
- Create `EditorPluginManager` for lifecycle management

### Phase 2: Capability Interfaces
- `IInspectorCapability` with PropertyDrawer registration
- `IMenuCapability` with menu/toolbar registration
- `IPanelCapability` with panel registration

### Phase 3: Viewport Capabilities
- `IViewportCapability` for gizmos and overlays
- `IToolCapability` for viewport tools
- `IShortcutCapability` for keybindings

### Phase 4: Asset Capabilities
- `IAssetCapability` for importers
- Thumbnail generators
- Drag-drop handlers

### Phase 5: Built-in Plugin Refactoring
- Convert InspectorPanel to InspectorPlugin
- Convert HierarchyPanel to HierarchyPlugin
- Convert ViewportPanel to ViewportPlugin

## Related

- [ADR-007: Capability-Based Plugin Architecture](007-capability-based-plugin-architecture.md)
- [Scene Editor Architecture](../research/scene-editor-architecture.md)
- [Epic #600: Scene/World Editor](https://github.com/orion-ecs/keen-eye/issues/600)
