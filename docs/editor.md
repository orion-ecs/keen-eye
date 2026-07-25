# Editor

The KeenEyes Editor (`KeenEyes.Editor`) is a desktop application for authoring KeenEyes games — a scene view, entity hierarchy, component inspector, console, and asset browser built on the same ECS, UI, graphics, and input libraries the runtime uses. It also defines a plugin model (`IEditorPlugin`, below); today the built-in panels are wired directly into the editor application, and the two built-in gizmo plugins (navigation, animation) are the only plugins installed at startup.

> The editor is distinct from the ECS plugin system in [Plugins](plugins.md). `IWorldPlugin` extends a `World`; `IEditorPlugin` (below) extends the *editor application*.

## Launching

```bash
dotnet run --project editor/KeenEyes.Editor -c Release
```

On start the editor opens its window, initializes graphics/input/UI, and builds the default panel layout. It also starts a [TestBridge](testbridge.md) IPC server (`KeenEyes.Editor.TestBridge`) so external tools can inspect and drive the editor world (a second server, `KeenEyes.Editor.Scene.TestBridge`, starts for the scene world once a scene is opened).

There is no project system yet: the editor treats its **current working directory** as the project root, building an `AssetDatabase` that recursively indexes `.kescene`, `.keprefab`, and `.keworld` files under it. Launch the editor from your game's directory to work on that game's assets. The **File ▸ New Project** and **File ▸ Open Project** menu items are declared but not yet implemented — clicking them does nothing.

## Opening scenes

**File ▸ Open Scene** (Ctrl+O) does not show a file dialog. If the asset database contains exactly one `.kescene`, it opens that scene directly; with none it logs "No scene files found"; with several it logs the candidates and defers to the Project panel, where double-clicking a `.kescene` opens it. **Save Scene As** prompts for a file name (resolved against the working directory, appending `.kescene` if missing) rather than opening a file browser.

Opening (or creating) a scene is also what activates the scene-dependent features: play mode, hot reload, replay playback, and the scene TestBridge server are initialized when a scene is opened, not at launch.

## Panels & tools

The editor's default layout is composed of dockable panels:

| Panel | Purpose |
|-------|---------|
| Viewport | Renders the scene; hosts transform gizmos and editor tools |
| Hierarchy | The entity tree — select, rename, reparent, create, and delete entities |
| Inspector | Edit the components of the selected entity via per-type property drawers |
| Console | Editor and game log output (backed by the logging query layer) |
| Project | Browse the working directory's `.kescene`/`.keprefab`/`.keworld` assets (the `AssetDatabase`); double-click a scene to open it |
| Frame Inspector | Step through recorded frames during replay playback |

Editing actions run through an `UndoRedoManager` (every mutation is an `IEditorCommand`, e.g. create/delete/reparent/rename/set-component), a `SelectionManager` tracks the active entity, and an `EntityClipboard` supports copy/cut/paste. Layouts are saved and restored by the `LayoutManager`.

## Play mode & hot reload

Both features become available once a scene is opened (see [Opening scenes](#opening-scenes) above).

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
        // Probe for the capabilities this plugin needs
        if (context.TryGetCapability<IPanelCapability>(out var panels) && panels is not null)
        {
            // ...register panels, menu items, tools, inspectors, etc.
        }
    }

    public override void Shutdown()
    {
        // Release anything registered in Initialize
    }
}
```

The capability interfaces defined in `KeenEyes.Editor.Abstractions` are `IPanelCapability`, `IInspectorCapability`, `IMenuCapability`, `IShortcutCapability`, `IToolCapability`, `IViewportCapability`, `IAssetCapability`, and `INotificationCapability`. Note that the shipped editor currently registers only `IViewportCapability` with the plugin manager — `GetCapability<T>()` throws `InvalidOperationException` for the others, so use `TryGetCapability<T>(out var cap)` to probe for what is available. Common extension points:

- **Panels** — implement `IEditorPanel` and register via `IPanelCapability`.
- **Tools** — derive from `EditorToolBase` (viewport tools with activate/update hooks).
- **Inspectors & property drawers** — implement `IComponentInspector` or a `PropertyDrawer` (registered through `IPropertyDrawerRegistry`) to customize how a component or field type is edited.
- **Gizmos** — implement `IGizmoDrawer` to draw in the viewport.

## Installing plugins (marketplace)

Game and third-party editor plugins are distributed as NuGet packages and installed with the CLI, which shares its configuration with the editor. Note that the running editor does not yet discover and load CLI-installed plugins at startup — the install/search tooling below works, but the loading step is not wired up yet:

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
