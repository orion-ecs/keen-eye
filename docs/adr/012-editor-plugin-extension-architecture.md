# ADR-012: Editor Plugin Extension Architecture

**Status:** Accepted
**Revision:** v2
**Implementation:** Partial
**First accepted:** 2026-01-01 · **Last amended:** 2026-07-26
**Relates to:** [ADR-007](007-capability-based-plugin-architecture.md) (plugin capabilities) · [#600](https://github.com/orion-ecs/keen-eye/issues/600)

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
    // Core services (read-only access, interface-typed)
    IEditorWorldManager Worlds { get; }
    ISelectionManager Selection { get; }
    IUndoRedoManager UndoRedo { get; }
    IAssetDatabase Assets { get; }
    IWorld EditorWorld { get; }
    ILogQueryable? Log { get; }  // null when no queryable log provider is configured

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
    EventSubscription OnSceneOpened(Action<IWorld> handler);
    EventSubscription OnSceneClosed(Action handler);
    EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler);
    EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler);
}
```

Core services are exposed through interfaces (`IEditorWorldManager`, `ISelectionManager`, `IUndoRedoManager`, `IAssetDatabase`) rather than the concrete manager types originally sketched, and an `ILogQueryable? Log` service was added. Not yet implemented: the originally proposed `EditorProject Project` service — no `EditorProject` type exists.

### Editor Capabilities

Following ADR-007's pattern, editor features are defined as capability interfaces. Nine ship: the seven originally proposed plus two added during implementation:

| Capability | Purpose |
|------------|---------|
| `IInspectorCapability` | Register property drawers, component inspectors |
| `IViewportCapability` | Add gizmo renderers, overlays, pick handlers |
| `IMenuCapability` | Add menu items, context menus, toolbar buttons |
| `IPanelCapability` | Register dockable panels |
| `IAssetCapability` | Custom asset importers, thumbnails |
| `IShortcutCapability` | Register keyboard shortcuts |
| `IToolCapability` | Register viewport tools (select, move, etc.) |
| `INotificationCapability` | Editor notifications/toasts (added post-proposal) |
| `IExtendedPanelCapability` | Extended panel management (added post-proposal) |

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
    void RemovePickHandler(IPickHandler handler);
}

public interface IGizmoRenderer
{
    int Order { get; }
    bool IsVisible { get; }
    void Render(GizmoRenderContext context);
}
```

`IGizmoRenderer.Render` receives the current selection through `GizmoRenderContext` rather than as a separate parameter. Not yet implemented: camera-mode registration (`RegisterCameraMode(string id, ICameraMode mode)`) from the original proposal — no `ICameraMode` type exists.

#### IMenuCapability

```csharp
public interface IMenuCapability : IEditorCapability
{
    void AddMenuItem(MenuPath path, EditorCommand command);
    void AddContextMenuItem<T>(MenuPath path, EditorCommand<T> command);
    void AddToolbarButton(ToolbarSection section, EditorCommand command);
    bool RemoveMenuItem(MenuPath path);  // true if the item existed
}

// MenuPath is a readonly struct of path segments,
// constructed via new MenuPath(params string[]) or parsed from a string:
public readonly struct MenuPath
{
    public MenuPath(params string[] segments) { ... }
    public static MenuPath Parse(string path) { ... }  // e.g. "File/Export/Scene"

    public MenuPath Parent { get; }
    public string Name { get; }
    public bool IsRoot { get; }
}
```

The originally sketched per-menu static factories (`MenuPath.File(...)`, `MenuPath.Edit(...)`, etc.) were not implemented; segment construction and `Parse` cover those cases.

#### IPanelCapability

```csharp
public interface IPanelCapability : IEditorCapability
{
    void RegisterPanel<T>(PanelDescriptor descriptor) where T : IEditorPanel, new();
    void RegisterPanel(PanelDescriptor descriptor, Func<IEditorPanel> factory);
    void OpenPanel(string id);
    void ClosePanel(string id);
    bool IsPanelOpen(string id);
    void FocusPanel(string id);
}

public interface IEditorPanel : IDisposable
{
    void Initialize(PanelContext context);
    void Update(float deltaTime);
    void Shutdown();
}

public class PanelDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Icon { get; init; }
    public PanelDockLocation DefaultLocation { get; init; }
    public bool OpenByDefault { get; init; }
    public float MinWidth { get; init; }
    public float MinHeight { get; init; }
    public float DefaultWidth { get; init; }
    public float DefaultHeight { get; init; }
    public string? Category { get; init; }
    public ShortcutBinding? ToggleShortcut { get; init; }
}
```

As shipped, `PanelDescriptor` is an init-property class rather than the positional record originally sketched, and `IEditorPanel` follows an `Initialize(PanelContext)` / `Update` / `Shutdown` lifecycle instead of building UI directly via a `CreateUI(IWorld, Entity, FontHandle)` method. A factory-based `RegisterPanel(descriptor, Func<IEditorPanel>)` overload and `FocusPanel(id)` were added.

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

The shipped lifecycle matches this flow and extends it beyond the original proposal:

- **Dynamic discovery and loading** in collectible, isolated load contexts (`PluginLoader`, `PluginLoadContext`, `PluginRepository`)
- **Permission-based security** — `PluginPermission` flags gate capability access via `PermissionManager` and `SecurePluginContext`, with signature verification (`PluginSignatureVerifier`) and `PermissionDeniedException` on violations
- **Hot reload with state preservation** — plugins implementing `IStatefulPlugin` save and restore state across reloads
- **`EditorPluginBase`** convenience base class for plugin authors

See [Editor Plugin Development](../editor-plugin-development.md) for the full authoring guide.

### Built-in Plugins

Core editor features are implemented as internal plugins:

| Plugin | Provides | Status |
|--------|----------|--------|
| `CoreEditorPlugin` | Selection, undo/redo, basic commands | ✅ Implemented |
| `InspectorPlugin` | Component inspector, built-in property drawers | ✅ Implemented, unit-tested |
| `HierarchyPlugin` | Scene tree panel | ✅ Implemented, unit-tested |
| `ViewportPlugin` | 3D/2D viewport, transform gizmos, grid | ✅ Implemented |
| `ConsolePlugin` | Log panel | ✅ Implemented |
| `ProfilerPlugin` | System timing panel | Not implemented — frame inspection lives in the static `FrameInspectorPanel` |
| `ProjectPlugin` | Asset browser panel | ✅ Implemented |
| `PluginManagerPlugin` | Plugin management panel (added post-proposal) | ✅ Implemented |

These serve as reference implementations for third-party plugins. Note, however, that `EditorApplication` has not yet migrated to them: the shipping editor still constructs its panels through the static panel classes (`HierarchyPanel.Create`, `ViewportPanel.Create`, `InspectorPanel.Create`, etc.), and the only plugins installed at startup via `EditorPluginManager` are the gizmo plugins (`NavigationEditorPlugin`, `AnimationEditorPlugin`). Completing the panel migration remains open work.

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

### Phase 1: Core Abstractions ✅
- [x] Create `IEditorPlugin`, `IEditorContext` interfaces
- [x] Create `IEditorCapability` marker interface
- [x] Create `EditorPluginManager` for lifecycle management (wired into `EditorApplication`)

### Phase 2: Capability Interfaces ✅
- [x] `IInspectorCapability` with PropertyDrawer registration
- [x] `IMenuCapability` with menu/toolbar registration
- [x] `IPanelCapability` with panel registration

### Phase 3: Viewport Capabilities ✅ (except camera modes)
- [x] `IViewportCapability` for gizmos and overlays
- [x] `IToolCapability` for viewport tools
- [x] `IShortcutCapability` for keybindings
- Not yet implemented: camera-mode registration (`RegisterCameraMode`/`ICameraMode`)

### Phase 4: Asset Capabilities ✅
- [x] `IAssetCapability` for importers
- [x] Thumbnail generators
- [x] Drag-drop handlers

### Phase 5: Built-in Plugin Refactoring (partial)
- [x] Convert InspectorPanel to InspectorPlugin
- [x] Convert HierarchyPanel to HierarchyPlugin
- [x] Convert ViewportPanel to ViewportPlugin
- Not yet implemented: installing these plugins in `EditorApplication` — the live editor still builds panels via the static panel classes, so the plugin versions are written and tested but not yet the shipping code path

## Related

- [ADR-007: Capability-Based Plugin Architecture](007-capability-based-plugin-architecture.md)
- [Scene Editor Architecture](../research/scene-editor-architecture.md)
- [Editor Plugin Development](../editor-plugin-development.md) — authoring guide for this architecture
- [Editor Documentation](../editor.md)
- [Epic #600: Scene/World Editor](https://github.com/orion-ecs/keen-eye/issues/600)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Accepted — the architecture is implemented and documented as the editor's supported extension surface. Implementation marked Partial: `RegisterCameraMode`/`ICameraMode`, `ProfilerPlugin`, and the `EditorProject` `Project` context service were never built, and the built-in panel plugins, though implemented and unit-tested, are not yet installed by `EditorApplication` (the live editor still uses static panel classes). Body amended to as-built API shapes (interface-typed `IEditorContext` services plus `Log`, `MenuPath` as a segment struct, `PanelDescriptor`/`IEditorPanel` Initialize/Update/Shutdown lifecycle, `IGizmoRenderer.Render(GizmoRenderContext)`), the two added capabilities (`INotificationCapability`, `IExtendedPanelCapability`), and the shipped lifecycle extensions (dynamic loading, permission-based security, hot reload).
- **v1 — 2026-01-01 (#600):** Proposed — IEditorPlugin/IEditorContext capability-based editor plugin architecture mirroring the runtime plugin pattern (ADR-007), with capability interfaces, source-generated extensions, and built-in plugin refactoring plan.
