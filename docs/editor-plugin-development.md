# Editor Plugin Development

This is a reference for authoring editor plugins against `KeenEyes.Editor.Abstractions`. It assumes you've read the [Editor](editor.md) overview (launching, panels, install flow); this page goes one level deeper into the lifecycle interfaces, capabilities, and extension points a plugin actually codes against.

Types that make up the surface area:

- **Lifecycle**: `IEditorPlugin`, `EditorPluginBase`, `IEditorContext`, `IStatefulPlugin`, `IEditorCapability`, `PluginPermission`, `PermissionDeniedException`.
- **Capabilities** (obtained from `IEditorContext`): `IPanelCapability` / `IExtendedPanelCapability`, `IInspectorCapability`, `IMenuCapability`, `IShortcutCapability`, `IToolCapability`, `IViewportCapability`, `IAssetCapability`, `INotificationCapability`.
- **Extension points** (types you implement): `IEditorPanel`, `EditorToolBase` / `IEditorTool`, `IComponentInspector`, `PropertyDrawer` (via `IPropertyDrawerRegistry`), `IGizmoRenderer` / `IGizmoDrawer`, `IEditorCommand`, and classes marked `[EditorExtension]`.

All of these live under the `KeenEyes.Editor.Abstractions` namespace, with the capability interfaces specifically in `KeenEyes.Editor.Abstractions.Capabilities` and drawer/inspector types in `KeenEyes.Editor.Abstractions.Inspector`.

## Plugin Lifecycle

### IEditorPlugin

`IEditorPlugin` is the contract every editor plugin implements: `Name`, `Version`, `Description` (nullable), `Initialize(IEditorContext context)`, and `Shutdown()`. Per its documented lifecycle, a plugin is instantiated by the editor, `Initialize` is called with the editor context, the plugin registers its extensions through capabilities obtained from that context, it stays active until editor shutdown, and `Shutdown` is called to release resources. Resources registered through capabilities during `Initialize` are tracked and cleaned up automatically when the plugin is uninstalled — `Shutdown` is for any *additional* custom cleanup.

### EditorPluginBase

`EditorPluginBase` is an abstract base implementing `IEditorPlugin` with sensible defaults: `Version` defaults to `"1.0.0"` and `Description` defaults to `null` (both `virtual`, override to change). It exposes a protected `Context` property (`IEditorContext?`) that's set when `Initialize` runs and cleared when `Shutdown` runs — so it's `null` outside that window. `Initialize`/`Shutdown` themselves are plain (non-virtual) methods that set/clear `Context` and delegate — you don't override them directly. Instead, override the protected `OnInitialize(IEditorContext context)` (abstract) and `OnShutdown()` (virtual, no-op by default).

### IEditorContext

`IEditorContext` is what `Initialize`/`OnInitialize` receives, and it groups into four areas:

- **Core services** — properties for the pieces of editor state a plugin most commonly needs: `Worlds` (`IEditorWorldManager`), `Selection` (`ISelectionManager`), `UndoRedo` (`IUndoRedoManager`), `Assets` (`IAssetDatabase`), `EditorWorld` (`IWorld`, the world backing the editor's own UI, distinct from the scene being edited), and `Log` (`ILogQueryable?`, null if no queryable log provider is configured — see [Logging](logging.md#querying-log-history)).
- **Extension storage** — `SetExtension<T>(T extension)`, `T GetExtension<T>()` (throws `InvalidOperationException` if unregistered), `bool TryGetExtension<T>(out T? extension)`, and `bool RemoveExtension<T>()`. This is the same pattern used for `IWorld` extensions, applied to the editor itself, and lets one plugin expose an API surface that other plugins can consume.
- **Capability access** — `T GetCapability<T>()` (throws `InvalidOperationException` if unavailable), `bool TryGetCapability<T>(out T? capability)`, and `bool HasCapability<T>()`, all constrained to `T : class, IEditorCapability`.
- **Event subscriptions** — `OnSceneOpened(Action<IWorld>)`, `OnSceneClosed(Action)`, `OnSelectionChanged(Action<IReadOnlyList<Entity>>)`, and `OnPlayModeChanged(Action<EditorPlayState>)`. Each returns an `EventSubscription`; dispose it to unsubscribe. `EditorPlayState` is an enum: `Editing`, `Playing`, `Paused`.

### Capabilities and permissions

`IEditorCapability` is an empty marker interface every capability interface implements, so `GetCapability<T>`/`TryGetCapability<T>`/`HasCapability<T>` can be constrained generically. Beyond the "is it available at all" check (`InvalidOperationException` from `GetCapability<T>`), capability access is also gated by `PluginPermission` — a `[Flags] enum` (file system, network, process/reflection/assembly-loading, and editor-specific bits like `MenuAccess`, `PanelAccess`, `InspectorAccess`, `ViewportAccess`, `ShortcutAccess`, `UndoAccess`, `SelectionAccess`, `AssetDatabaseAccess`) plus composites like `StandardEditor`, `EditorUI`, and `FullTrust`. Per its own remarks, `PermissionDeniedException` (carrying `PluginId`, `RequiredPermission`, and an optional `CapabilityType`) is thrown by `IEditorContext` implementations when a plugin attempts to use a capability it wasn't granted permission for.

### Stateful plugins and hot reload

Implement `IStatefulPlugin` (extends `IEditorPlugin`) to survive a hot reload: `object? SaveState()` is called before the plugin is unloaded, and `void RestoreState(object? state)` is called after the new instance's `Initialize` runs. The returned state object should avoid referencing types defined in the plugin's own assembly (dictionaries of primitives or host-defined record types are recommended) since that assembly is being unloaded and reloaded. See [Editor Plugin Hot Reload](editor-plugin-hot-reload.md) for the full reload sequence and constraints.

### EditorExtensionAttribute

`[EditorExtension(string propertyName)]`, applied to a class, tells the source generator to create a C# 13 extension member on `IEditorContext` that wraps `GetExtension<T>()`/`SetExtension<T>()` in a named property (`context.MyExtension` instead of `context.GetExtension<MyExtensionClass>()`). It has one constructor argument, `PropertyName`, and one settable property, `Nullable` (`bool`, default `false`) — when `true` the generated property returns `null` if the extension isn't registered instead of throwing.

## Panels

`IPanelCapability` registers and manages dockable panels:

- `RegisterPanel<T>(PanelDescriptor descriptor)` where `T : IEditorPanel, new()`, or `RegisterPanel(PanelDescriptor descriptor, Func<IEditorPanel> factory)` when construction needs arguments.
- `OpenPanel(string id)`, `ClosePanel(string id)`, `IsPanelOpen(string id)`, `FocusPanel(string id)`.
- `GetPanelDescriptors()`, `GetOpenPanels()`, and the `PanelOpened` / `PanelClosed` events (`Action<string>?`).

`PanelDescriptor` fields: `Id`, `Title`, `Icon` (nullable), `DefaultLocation` (`PanelDockLocation`: `Left`, `Right`, `Bottom`, `Top`, `Center`, `Floating`), `OpenByDefault`, `MinWidth`/`MinHeight`/`DefaultWidth`/`DefaultHeight`, `AllowMultiple`, `Category`, and `ToggleShortcut`.

`IExtendedPanelCapability` extends `IPanelCapability` with `GetPanelState(string panelId)` / `SetPanelState(string panelId, ExtendedPanelState state)`, where `ExtendedPanelState` carries `X`, `Y`, `Width`, `Height`, and `DockLocation` — used to capture and restore panel layout across a hot reload.

The extension point is `IEditorPanel`: `Initialize(PanelContext context)`, `Update(float deltaTime)` (called every frame), `Shutdown()`, and a `RootEntity` property (the UI entity the panel built). `PanelContext` supplies `EditorContext`, `EditorWorld`, the `Parent` entity to attach content under, the `Descriptor`, and a default `Font` (`FontHandle`).

```csharp
public sealed class MyPanel : IEditorPanel
{
    public Entity RootEntity { get; private set; }

    public void Initialize(PanelContext context)
    {
        // Build UI entities under context.Parent using context.EditorWorld
        RootEntity = context.Parent;
    }

    public void Update(float deltaTime) { }

    public void Shutdown() { }
}
```

## Inspector customization

`IInspectorCapability` is how a plugin changes what the Inspector panel shows for a field or component type:

- `RegisterPropertyDrawer(Type fieldType, PropertyDrawer drawer)` / `RegisterPropertyDrawer<T>(PropertyDrawer drawer)` — drawer for a field *type*.
- `RegisterDrawerForAttribute<TAttribute>(PropertyDrawer drawer)` where `TAttribute : Attribute` — drawer for fields carrying a specific attribute, regardless of field type.
- `RegisterComponentInspector<TComponent>(IComponentInspector inspector)` where `TComponent : struct, IComponent` — replaces the entire default per-field display for a component with custom UI.
- `RegisterComponentActions<TComponent>(IComponentActionProvider provider)` — adds context-menu actions for a component.
- `PropertyDrawers` — direct access to the underlying `IPropertyDrawerRegistry`.

**Property drawers.** `PropertyDrawer` is an abstract class: `TargetType` (the type it handles), `GetHeight(FieldInfo field, object? value)` (defaults to `20f`), the abstract `CreateUI(PropertyDrawerContext context, FieldInfo field, object? value) : Entity`, and a virtual `UpdateUI(PropertyDrawerContext context, Entity uiEntity, object? value)` (no-op by default; override for reactive updates when the underlying value changes externally). `PropertyDrawerContext` supplies `EditorWorld`, `Parent`, `Font`, field `Metadata` (`FieldMetadata`), and an `OnValueChanged` callback to invoke when the user edits the value. `FieldMetadata` carries `DisplayName`, `Tooltip`, `Header`, `SpaceHeight`, a `Range` tuple (`Min`, `Max`) for numeric fields, `IsReadOnly`, `FoldoutGroup`, and a `TextArea` tuple (`MinLines`, `MaxLines`) for strings.

`IPropertyDrawerRegistry` is the lookup table drawers are stored in: `Register<T>(PropertyDrawer)` / `Register(Type, PropertyDrawer)`, `GetDrawer(Type)` / `GetDrawer<T>()` (falls back to a default drawer if nothing is registered for the type), and `HasDrawer(Type)`.

**Component inspectors.** `IComponentInspector` replaces the default field-by-field rendering for a component: `Entity CreateUI(ComponentInspectorContext context, object componentValue)` and `void UpdateUI(ComponentInspectorContext context, Entity rootEntity, object componentValue)`. `ComponentInspectorContext` supplies `EditorWorld`, `Parent`, the `InspectedEntity`, `ComponentType`, and `OnValueChanged`.

**Component actions.** `IComponentActionProvider.GetActions(ComponentActionContext context)` returns `ComponentAction` entries (`Name`, `Execute`, `IsEnabled`, `Icon`) that appear as context-menu items next to a component in the Inspector.

## Menus & toolbar

`IMenuCapability` adds menu items, context-menu items, and toolbar buttons:

- `AddMenuItem(MenuPath path, EditorCommand command)` — top-level menu bar item, e.g. `"File/Export/Scene"`.
- `AddContextMenuItem<T>(MenuPath path, EditorCommand<T> command)` — item shown when right-clicking a `T`.
- `AddToolbarButton(ToolbarSection section, EditorCommand command)` — `ToolbarSection` is `File`, `Edit`, `PlayMode`, `Tools`, `View`, or `Custom`.
- `RemoveMenuItem(MenuPath path)`, `RemoveToolbarButton(string commandId)`, `GetMenuItems(MenuPath parentPath)`.

`MenuPath` is a struct built from segments (`new MenuPath("File", "Export", "Scene")`) or parsed from a slash-delimited string via `MenuPath.Parse` — and strings convert to it implicitly, so `AddMenuItem("File/Export/Scene", command)` works directly.

`EditorCommand` (used for menu items and toolbar buttons) has `Id`, `DisplayName`, `Execute` (`Action`), an optional `CanExecute` (`Func<bool>`), `Shortcut`, `Icon`, and `Tooltip`. `EditorCommand<T>` is the context-menu variant: same shape, but `Execute` is `Action<T>` and `CanExecute` is `Func<T, bool>`, so the command receives the right-clicked target.

> `EditorCommand` (menu/toolbar action descriptor) is unrelated to `IEditorCommand` (the undo/redo primitive, covered under [Undo/redo commands](#undoredo-commands) below) — they share "command" in the name but serve different capabilities.

## Shortcuts

`IShortcutCapability` registers keyboard shortcuts with conflict detection and rebinding:

- `RegisterShortcut(string actionId, string displayName, string category, string defaultShortcut, Action action)` and an overload taking an additional `Func<bool> canExecute` — both return a `ShortcutBinding`.
- `UnregisterShortcut(string actionId)`, `GetShortcut(string actionId)`, `GetShortcutsInCategory(string category)`, `GetAllShortcuts()`, `GetCategories()`.
- `RebindShortcut(string actionId, string newShortcut)`, `ResetShortcut(string actionId)`, `ResetAllShortcuts()`.
- `FindConflict(string shortcut, string? excludeActionId = null)` — returns the conflicting `ShortcutBinding`, if any.
- `ProcessKeyEvent(KeyEvent keyEvent)` — dispatches a key event to matching shortcuts, returning whether one fired.
- Events: `ShortcutRegistered`, `ShortcutUnregistered`, `ShortcutRebound`.

`ShortcutBinding` carries `ActionId`, `DisplayName`, `Category`, `DefaultShortcut`, a mutable `CurrentShortcut`, `Execute`, `CanExecute`, a derived `IsModified` (current vs. default), `IsEnabled`, and a parsed `ParsedShortcut` (`KeyCombination?`). Shortcut strings like `"Ctrl+Shift+S"` parse into a `KeyCombination` (`Key` + `KeyModifiers` flags: `Control`, `Alt`, `Shift`, `Meta`) via `KeyCombination.Parse`. `ShortcutCategories` supplies standard category name constants (`File`, `Edit`, `View`, `Selection`, `Transform`, `PlayMode`, `Navigation`, `Tools`, `Custom`).

## Tools

`IToolCapability` manages viewport interaction tools: `ActiveTool` / `ActiveToolId`, `RegisterTool(string id, IEditorTool tool)`, `UnregisterTool(string id)`, `ActivateTool(string id)`, `DeactivateTool()`, `GetTool(string id)`, `GetTools()`, `GetToolsInCategory(string category)`, and events `ActiveToolChanged` (`ToolChangedEventArgs`: `PreviousToolId`/`PreviousTool`/`NewToolId`/`NewTool`), `ToolRegistered`, `ToolUnregistered`.

The extension point is `IEditorTool`: `DisplayName`, `Icon`, `Category`, `Tooltip`, `Shortcut`, `IsEnabled`, `OnActivate(ToolContext)` / `OnDeactivate(ToolContext)`, `Update(ToolContext, float deltaTime)`, `OnMouseDown`/`OnMouseUp(ToolContext, MouseButton, Vector2 position)` and `OnMouseMove(ToolContext, Vector2 position, Vector2 delta)` (each returns `bool` — whether the tool consumed the input), and `OnRender(GizmoRenderContext)` for drawing tool-specific overlays. `EditorToolBase` is an abstract class supplying default (mostly no-op / `false` / `"General"`) implementations of every member, so a concrete tool typically derives from it and overrides only what it needs. `ToolContext` supplies `EditorContext`, the nullable `SceneWorld`, `SelectedEntities`, `ViewportBounds`, `ViewMatrix`, `ProjectionMatrix`, `CameraPosition`, and `CameraForward`. `ToolCategories` supplies constants (`Selection`, `Transform`, `Creation`, `Terrain`, `Physics`, `Custom`); `MouseButton` is `Left`, `Right`, `Middle`, `Button4`, `Button5`.

```csharp
public sealed class MeasureTool : EditorToolBase
{
    public override string DisplayName => "Measure";
    public override string Category => ToolCategories.Custom;

    public override bool OnMouseDown(ToolContext context, MouseButton button, Vector2 position)
    {
        // Start a measurement at the clicked viewport position
        return true;
    }
}
```

## Viewport & gizmos

`IViewportCapability` customizes viewport rendering and picking:

- `AddGizmoRenderer(IGizmoRenderer renderer)` / `RemoveGizmoRenderer` / `GetGizmoRenderers()`, plus `GizmoRendererAdded` / `GizmoRendererRemoved` events.
- `AddOverlay(string id, IViewportOverlay overlay)` / `RemoveOverlay(string id)` / `SetOverlayVisible(string id, bool visible)` / `IsOverlayVisible(string id)` / `GetOverlayIds()`.
- `AddPickHandler(IPickHandler handler)` / `RemovePickHandler` / `GetPickHandlers()`.

`IGizmoRenderer` is what a plugin implements and registers: `Id`, `DisplayName`, a mutable `IsEnabled`, `Order` (render order — lower values render first, i.e. behind), `Render(GizmoRenderContext context)`, and `ShouldRender(Entity entity, IWorld sceneWorld)` (a filter deciding whether this renderer draws for a given entity). `GizmoRenderContext` gives the renderer `SceneWorld`, `SelectedEntities`, `ViewMatrix`, `ProjectionMatrix`, `CameraPosition`, `Bounds`, `DeltaTime`, and a `Drawer` (`IGizmoDrawer`) — plus `DrawTriangle`/`DrawLine`/`DrawPoint` convenience methods on the context itself that simply forward to `Drawer`. `IGizmoDrawer` is the actual primitive-drawing surface: `DrawTriangle`, `DrawLine` (with `width`), `DrawPoint` (with `size`), `DrawWireBox(min, max, color, lineWidth)`, `DrawWireSphere(center, radius, color, segments)`, and `DrawText(position, text, color)`.

`IViewportOverlay` is for 2D content drawn on top of the viewport rather than 3D gizmos in scene space: `IsVisible`, `Order`, and `Render(OverlayRenderContext context)` (`EditorWorld`, nullable `SceneWorld`, `Bounds`, `DeltaTime`).

`IPickHandler` intercepts entity picking: `Priority` (higher checked first) and `TryPick(PickContext context) : PickResult?`. `PickContext` supplies the normalized screen coordinates, ray origin/direction, view/projection matrices, and bounds; `PickResult` returns the picked `Entity`, `WorldPosition`, `Distance`, and optional `UserData`.

## Assets

`IAssetCapability` extends how the Project panel and asset pipeline handle files:

- `RegisterImporter(string[] extensions, IAssetImporter importer)` / `UnregisterImporter(string importerId)` / `GetImporter(string extension)` / `GetImporters()`, with `ImporterRegistered` / `ImporterUnregistered` events. `IAssetImporter` exposes `Id`, `DisplayName`, `Priority`, `SupportedExtensions`, `CanImport(string filePath)`, `Task<AssetImportResult> ImportAsync(AssetImportContext context)`, and `GetSettingsUI() : IImportSettingsUI?`.
- `RegisterThumbnailGenerator<TAsset>(IThumbnailGenerator generator)` / the `Type`-based overload / `GetThumbnailGenerator(Type)`. `IThumbnailGenerator` exposes `Id`, `Priority`, `CanGenerate(string assetPath)`, and `Task<ThumbnailResult> GenerateAsync(ThumbnailContext context)`.
- `RegisterDragDropHandler<TAsset>(IAssetDragDropHandler handler)` / `Type` overload / `GetDragDropHandler(Type)`. `IAssetDragDropHandler` exposes `Id`, `Priority`, `CanDrag(AssetDragContext) : DragDropEffect`, `CanDrop(AssetDropContext) : DragDropEffect`, and `OnDrop(AssetDropContext) : bool`. `DragDropEffect` is `None`, `Copy`, `Move`, or `Link`.
- `RegisterDoubleClickAction<TAsset>(Action<TAsset> action)` / `Type` overload / `GetDoubleClickAction(Type)`.
- `RegisterContextMenuProvider<TAsset>(IAssetContextMenuProvider provider)` / `Type` overload / `GetContextMenuProviders(Type)`. `IAssetContextMenuProvider.GetMenuItems(AssetContextMenuContext)` returns `AssetMenuItem` entries (`Name`, `Execute`, `IsEnabled`, `Icon`, `Shortcut`, `SeparatorBefore`, nested `Children`).

Assets themselves are represented as `AssetEntry` records (`RelativePath`, `FullPath`, `Name`, `Type`, `LastModified`) with `AssetType` values `Scene`, `Prefab`, `WorldConfig`, `Shader`, `Texture`, `Audio`, `Script`, `Data`, or `Unknown`.

## Notifications

`INotificationCapability` shows toast-style feedback: `Show(NotificationOptions options) : NotificationHandle`, plus shorthand `ShowSuccess`/`ShowError`/`ShowWarning`/`ShowInfo(string message, string? title = null)`. `Dismiss(NotificationHandle handle)` and `DismissAll()` remove notifications; `ActiveCount` reports how many are showing; `NotificationShown`/`NotificationDismissed` events fire on each transition.

`NotificationOptions` (a record) has `Message`, `Title`, `Severity` (`NotificationSeverity`: `Info`, `Success`, `Warning`, `Error`), `Duration` (default 4 seconds; `TimeSpan.Zero` or negative means it stays until manually dismissed), `Icon`, `Dismissible`, an `OnClick` action, and optional `Actions` — a list of `NotificationAction` (`Label`, `Execute`, `DismissOnClick`).

## Undo/redo commands

Edits that should participate in undo/redo go through `IUndoRedoManager` (reached via `context.UndoRedo`, not a capability): `CanUndo`/`CanRedo`, `NextUndoDescription`/`NextRedoDescription`, `MaxHistorySize`, a `StateChanged` event, `Execute(IEditorCommand command)`, `Undo()`, `Redo()`, and batching via `BeginBatch(string description)` / `EndBatch()` / `CancelBatch()` (groups several commands into one undo step) plus `Clear()`.

The extension point is `IEditorCommand`: `Description` (shown in undo/redo UI), `Execute()`, `Undo()`, and `TryMerge(IEditorCommand other)` — return `true` from `TryMerge` to coalesce a subsequent command into this one (e.g. successive drag-move commands collapsing into a single undo step) instead of pushing a separate history entry.

## Next Steps

- [Editor](editor.md) — launching the editor, default panels, and the plugin marketplace install flow
- [Editor Plugin Dependencies](editor-plugin-dependencies.md) — version constraints and dependency resolution between plugins
- [Editor Plugin Hot Reload](editor-plugin-hot-reload.md) — the reload sequence, `IStatefulPlugin` state preservation, and limitations
- [SDK](sdk.md) — the MSBuild SDKs used to build plugin projects
