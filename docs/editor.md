# Editor

The KeenEyes Editor (`KeenEyes.Editor`) is a desktop application for authoring KeenEyes games — a scene view, entity hierarchy, component inspector, console, and asset browser built on the same ECS, UI, graphics, and input libraries the runtime uses. It is itself extensible: editor features are delivered as plugins, and game-specific plugins are installed from NuGet via the [`keeneyes` CLI](cli.md).

> The editor is distinct from the ECS plugin system in [Plugins](plugins.md). `IWorldPlugin` extends a `World`; `IEditorPlugin` (below) extends the *editor application*.

## Launching

```bash
dotnet run --project editor/KeenEyes.Editor -c Release
```

On start the editor opens its window, initializes graphics/input/UI, and builds the default panel layout. It also starts a [TestBridge](testbridge.md) IPC server (`KeenEyes.Editor.TestBridge`) so external tools can inspect and drive the editor world.

## Panels & tools

The editor's default layout is composed of dockable panels:

| Panel | Purpose |
|-------|---------|
| Viewport | Renders the scene; hosts transform gizmos and editor tools |
| Hierarchy | The entity tree — select, rename, reparent, create, and delete entities |
| Inspector | Edit the components of the selected entity via per-type property drawers |
| Console | Editor and game log output (backed by the logging query layer) |
| Project | Browse project assets through the `AssetDatabase` |
| Frame Inspector | Step through recorded frames during replay playback |

Editing actions run through an `UndoRedoManager` (every mutation is an `IEditorCommand`, e.g. create/delete/reparent/rename/set-component), a `SelectionManager` tracks the active entity, and an `EntityClipboard` supports copy/cut/paste. Layouts are saved and restored by the `LayoutManager`.

## Play mode & hot reload

- **Play mode** (`PlayModeManager`) runs the game world inside the editor and returns to the authoring state when stopped.
- **Hot reload** (`HotReloadService`) rebuilds and swaps the game assembly while the editor stays open, so component and system changes take effect without a full restart. See the [Editor Plugin Hot Reload](editor-plugin-hot-reload.md) guide for details and constraints.

## Extending the editor

Editor plugins implement `IEditorPlugin` (or derive from `EditorPluginBase`, which supplies a default `Version` and `Description`). A plugin receives an `IEditorContext` on initialization and reaches editor functionality through **capabilities** requested from that context:

```csharp
using KeenEyes.Editor.Abstractions;

public sealed class MyEditorPlugin : EditorPluginBase
{
    public override string Name => "My Editor Plugin";

    public override void Initialize(IEditorContext context)
    {
        // Request the capabilities this plugin needs
        var panels = context.GetCapability<IPanelCapability>();
        var menus = context.GetCapability<IMenuCapability>();
        // ...register panels, menu items, tools, inspectors, etc.
    }

    public override void Shutdown()
    {
        // Release anything registered in Initialize
    }
}
```

Available capabilities (from `KeenEyes.Editor.Abstractions`) include `IPanelCapability`, `IInspectorCapability`, `IMenuCapability`, `IShortcutCapability`, `IToolCapability`, `IViewportCapability`, `IAssetCapability`, and `INotificationCapability`. Common extension points:

- **Panels** — implement `IEditorPanel` and register via `IPanelCapability`.
- **Tools** — derive from `EditorToolBase` (viewport tools with activate/update hooks).
- **Inspectors & property drawers** — implement `IComponentInspector` or a `PropertyDrawer` (registered through `IPropertyDrawerRegistry`) to customize how a component or field type is edited.
- **Gizmos** — implement `IGizmoDrawer` to draw in the viewport.

## Installing plugins (marketplace)

Game and third-party editor plugins are distributed as NuGet packages and installed with the CLI, which shares its configuration with the editor:

```bash
keeneyes sources add studio-feed https://nuget.example.com/v3/index.json --default
keeneyes plugin search inventory
keeneyes plugin install Acme.KeenEyes.InventoryEditor
```

Plugin dependency resolution is described in [Editor Plugin Dependencies](editor-plugin-dependencies.md).

## Next Steps

- [Command-Line Interface](cli.md) - Manage editor plugins and package sources
- [Editor Plugin Dependencies](editor-plugin-dependencies.md) - How plugin dependencies resolve
- [Editor Plugin Hot Reload](editor-plugin-hot-reload.md) - Live assembly reload while editing
- [TestBridge Architecture](testbridge.md) - Inspecting and driving the editor from external tools
- [SDK](sdk.md) - Authoring plugins the editor can load
