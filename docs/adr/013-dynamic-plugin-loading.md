# ADR-013: Dynamic Plugin Loading

**Status:** Accepted
**Revision:** v2
**Implementation:** Partial
**First accepted:** 2026-01-02 · **Last amended:** 2026-07-26
**Relates to:** [ADR-012](012-editor-plugin-extension-architecture.md) (editor plugins) · [ADR-007](007-capability-based-plugin-architecture.md) (plugin capabilities)

## Context

KeenEyes Editor needs to support third-party plugins distributed as NuGet packages. Users should be able to:

1. **Install plugins** via NuGet package manager or CLI
2. **Load plugins** at editor startup (from installed packages)
3. **Enable/disable plugins** at runtime without editor restart
4. **Unload plugins** (optional hot reload) for development workflows
5. **Upgrade plugins** with minimal disruption

### Constraints

1. **Editor is JIT-compiled** - Reflection is acceptable (unlike runtime AOT)
2. **Plugins are NuGet packages** - Standard distribution mechanism
3. **Isolation required** - Plugin crashes shouldn't take down the editor
4. **Unloading is complex** - .NET's collectible AssemblyLoadContext has limitations

### Prior Art

| Editor | Plugin Loading | Hot Reload |
|--------|---------------|------------|
| Unity | Domain reload (full restart) | Yes (slow, ~2-5s) |
| Unreal | DLL replacement + restart | Limited |
| Godot | GDExtension (native) | No |
| VS Code | Extension host process | Yes (process restart) |
| Rider | Separate plugin process | Yes |

## Decision

Implement a **tiered plugin loading system** with three levels of dynamism:

### Tier 1: Static Plugins (Default)
- As built, Tier 1 is on-demand loading rather than a startup scan: built-in plugins are compiled into the editor and installed in `EditorApplication`, while installed third-party plugins are recorded in the persisted `PluginRegistry` and loaded via the Plugin Manager panel (`EditorPluginManager.LoadDynamicPlugin`)
- No automatic startup scan of plugin folders is wired (`DiscoverPlugins()` exists but has no production caller)
- Simplest, most stable approach
- All plugins work at this tier

### Tier 2: Enable/Disable at Runtime
- Plugins can be enabled/disabled without restart
- Plugin's `Initialize()` and `Shutdown()` called
- Assembly stays loaded (no unload)
- Requires plugin to properly clean up resources

### Tier 3: Full Hot Reload (Opt-in)
- Assembly can be unloaded and reloaded
- Uses collectible `AssemblyLoadContext`
- Plugin must declare `"supportsHotReload": true` in manifest
- Requires careful resource management

### Plugin Package Structure

```
MyPlugin.1.0.0.nupkg
├── lib/net10.0/
│   └── MyPlugin.dll
├── content/
│   └── keeneyes-plugin.json      # Plugin manifest (required)
└── MyPlugin.nuspec
```

### Plugin Manifest (keeneyes-plugin.json)

```json
{
  "name": "My Awesome Plugin",
  "id": "com.example.myawesomeplugin",
  "version": "1.0.0",
  "author": "Example Corp",
  "description": "Adds awesome features to the editor",

  "entryPoint": {
    "assembly": "MyPlugin.dll",
    "type": "MyPlugin.MyEditorPlugin"
  },

  "compatibility": {
    "minEditorVersion": "1.0.0",
    "maxEditorVersion": "2.0.0"
  },

  "capabilities": {
    "supportsHotReload": false,
    "supportsDisable": true
  },

  "dependencies": {
    "com.keeneyes.physics-editor": ">=1.0.0"
  },

  "settings": {
    "configFile": "myPlugin.config.json"
  }
}
```

The manifest contract is defined by `KeenEyes.Editor.Plugins.PluginManifest` (`Parse`/`TryParse`/`ToJson`) rather than a published JSON Schema — no plugin-manifest schema file exists in `schemas/`. Beyond the fields above, the shipped format also supports `security` (publicKeyToken, assemblyHash) and `permissions` (required/optional permission lists) sections, consumed by the plugin security subsystem.

### Architecture

```
EditorPluginManager            # Facade: lifecycle + state transitions
├── PluginRepository           # Discovers installed plugins
│   ├── Scan NuGet global cache
│   ├── Scan local plugin folders
│   └── Parse manifests
│
├── PluginLoader               # Loads/unloads assemblies
│   ├── Create PluginLoadContext (collectible if hot-reload)
│   ├── Load assembly + dependencies
│   ├── Instantiate IEditorPlugin via reflection
│   └── Unload context (if collectible)
│
├── LoadedPlugin + PluginState # Per-plugin state machine
│
├── Registry/PluginRegistry    # Persisted registry of installed packages
│
├── Dependencies/              # PluginDependencyResolver + DependencyGraph
│   ├── Load ordering (topological sort)
│   ├── Version constraints (NuGet VersionRange)
│   └── Circular-dependency detection
│
├── Installation/              # PluginInstaller / PluginUninstaller
├── NuGet/                     # NuGetClient (package acquisition)
└── Security/                  # AssemblyAnalyzer, PluginSignatureVerifier,
                               # PermissionManager, TrustedPublisherStore
```

There is no separate `PluginLifecycle` type: state transitions and error recovery live in `EditorPluginManager` itself, with per-plugin state carried by `LoadedPlugin`. Note that `PluginRegistry` is the *persisted installed-package registry*, not an in-memory load-state tracker. The `Installation/`, `NuGet/`, and `Security/` subsystems were added after this ADR was first written.

### PluginLoadContext

```csharp
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly HashSet<string> sharedAssemblies;

    public PluginLoadContext(string pluginPath, bool isCollectible)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath),
               isCollectible: isCollectible)
    {
        resolver = new AssemblyDependencyResolver(pluginPath);

        // Assemblies that should come from the host, not the plugin
        sharedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "KeenEyes.Core",
            "KeenEyes.Abstractions",
            "KeenEyes.Editor",
            "KeenEyes.Editor.Abstractions",
            // Framework assemblies handled by base class
        };
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Use host's version for shared assemblies (type identity)
        if (sharedAssemblies.Contains(assemblyName.Name!))
        {
            return null; // Delegates to default context
        }

        // Resolve plugin's own dependencies
        var path = resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            return LoadFromAssemblyPath(path);
        }

        return null;
    }
}
```

### Plugin States

The shipped `PluginState` enum defines six states: `Discovered`, `Loaded`, `Enabled`, `Disabled`, `Failed`, and `Unloading`. There is no terminal `Unloaded` state — after a hot-reload unload completes, the plugin reverts to `Discovered`.

```
    ┌──────────────┐
    │  Discovered  │◄───────────────────────────┐
    │   (on disk)  │                            │
    └──────┬───────┘                            │
           │ load                       ┌───────┴──────┐
           ▼                            │  Unloading   │
    ┌──────────────┐      unload*       │ (transitional│
    │    Loaded    │───────────────────►│  during hot  │
    │  (in memory) │                    │   reload)    │
    └──────┬───────┘                    └──────────────┘
           │ enable                             ▲
           ▼                                    │
    ┌──────────────┐                            │
    │   Enabled    │────────────────────────────┘
    │  (running)   │     disable + unload*
    └──────┬───────┘
           │ disable / enable
           ▼        ▲
    ┌───────────────┴──┐
    │     Disabled     │
    │    (sleeping)    │
    └──────────────────┘

    Failed — load or Initialize() threw; error isolation
             keeps the editor running

    * Only for hot-reload plugins
```

### Dependency Resolution

Plugins can depend on other plugins:

```json
{
  "dependencies": {
    "com.keeneyes.physics-editor": ">=1.0.0",
    "com.keeneyes.ui-toolkit": "^2.0.0"
  }
}
```

The `PluginLifecycle` ensures:
1. Dependencies are loaded before dependents
2. Dependents are disabled before dependencies
3. Version compatibility is checked at load time
4. Circular dependencies are detected and rejected

### Hot Reload Challenges

For unloading to work, ALL references to plugin types must be released:

1. **UI elements** - Plugin panels must be closed
2. **Event handlers** - All subscriptions must be disposed
3. **Cached types** - No `Type` or `MethodInfo` references retained
4. **Static fields** - Plugin must not store in host statics

The `EditorPluginContext` tracks every event subscription a plugin registers and disposes them on disable/unload; UI cleanup flows through the panel capability rather than a per-context entity list. Capability registrations (panels, drawers, tools, gizmos, shortcuts) are tracked as weak references so `UnloadDiagnostics` can report anything that fails to collect after an unload:

```csharp
internal sealed class EditorPluginContext : IEditorContext
{
    private readonly List<EventSubscription> subscriptions = [];

    // Weak-referenced capability registrations for unload diagnostics
    private readonly List<WeakReference<object>> registeredPanels = [];
    // ... drawers, tools, gizmos, shortcuts

    internal void DisposeSubscriptions()
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
        subscriptions.Clear();
    }
}
```

Hot reload additionally preserves plugin state across reloads via `IStatefulPlugin.SaveState`/`RestoreState` (see the [hot reload guide](../editor-plugin-hot-reload.md)).

### Error Handling

Plugin failures are isolated:

```csharp
public void EnableDynamicPlugin(string pluginId)
{
    var entry = registry.Get(pluginId);
    var context = new EditorPluginContext(this, entry.Plugin);

    try
    {
        entry.Plugin.Initialize(context);
        entry.State = PluginState.Enabled;
    }
    catch (Exception ex)
    {
        // Log error, mark the plugin Failed, release its resources
        logger.Error($"Plugin {pluginId} failed to initialize: {ex}");
        context.DisposeSubscriptions();
        entry.State = PluginState.Failed;

        // Optionally show user notification
        notifications.Show($"Plugin '{entry.Manifest.Name}' failed to start");
    }
}
```

### Plugin Discovery Locations

Plugins are discovered from:

1. **NuGet global cache** - `~/.nuget/packages/<package-id>/<version>/`
2. **Editor plugins folder** - `<editor-install>/plugins/`
3. **Project plugins** - `<project>/.keeneyes/plugins/`
4. **Development folder** - `<project>/Plugins/` (for local development)

### API Surface

The shipped lifecycle surface lives on `EditorPluginManager`:

```csharp
// Discovery and loading
editorPlugins.DiscoverPlugins();
editorPlugins.LoadDynamicPlugin("com.example.myplugin");

// Enable/disable
editorPlugins.EnableDynamicPlugin("com.example.myplugin");
editorPlugins.DisableDynamicPlugin("com.example.myplugin");

// Hot reload (opt-in plugins only)
editorPlugins.UnloadDynamicPlugin("com.example.myplugin");
editorPlugins.ReloadDynamicPlugin("com.example.myplugin");

// Query
var plugin = editorPlugins.GetDynamicPlugin("com.example.myplugin");
var all = editorPlugins.GetDynamicPlugins();
```

Package install, uninstall, and update do not live on the manager: they flow through `PluginInstaller` (`CreatePlanAsync`/`ExecuteAsync`) and `PluginUninstaller`, driven by the `keeneyes plugin install|uninstall|update|list|search` CLI commands and the Plugin Manager panel's Browse tab. (`InstallPlugin<T>()` exists but installs in-process built-in plugin instances, not packages.)

### User Experience

1. **Plugin Manager Panel** - UI for browsing, installing, enabling plugins
2. **Restart Indicator** - Shows when restart is needed for full changes
3. **Error Recovery** - Disable failing plugins, offer to uninstall
4. **Development Mode** - Not yet implemented: auto-reload on rebuild. Plugin reload is manual (Plugin Manager panel Reload action / `ReloadDynamicPlugin`); the editor's `HotReloadManager` auto-reload applies to game assemblies, not editor plugins

## Consequences

### Positive

1. **Standard distribution** - Uses NuGet, familiar to .NET developers
2. **Isolated loading** - Plugins get their own AssemblyLoadContext
3. **Tiered complexity** - Simple plugins just work; advanced features opt-in
4. **Development workflow** - Hot reload for plugin authors
5. **Version compatibility** - Manifests specify compatible editor versions

### Negative

1. **Complexity** - AssemblyLoadContext management is non-trivial
2. **Hot reload limitations** - Many edge cases can prevent clean unload
3. **Memory overhead** - Each plugin's ALC has some overhead
4. **Testing burden** - Must test all three tiers

### Neutral

1. **Reflection in loader** - Acceptable since editor is JIT-compiled
2. **Two-phase install** - NuGet install + editor enable are separate steps

## Implementation Phases

### Phase 1: Static Loading — shipped, with two gaps
- [x] Plugin manifest format (`PluginManifest` `Parse`/`TryParse`/`ToJson`)
- [x] Plugin discovery from NuGet cache and plugin folders (`PluginRepository`)
- [x] PluginLoadContext with dependency resolution
- [x] Basic PluginLoader (load-only)
- Not yet implemented: a published manifest JSON Schema (the contract lives in `PluginManifest.cs`)
- Not yet implemented: startup auto-load of installed plugins — loading is on-demand; `DiscoverPlugins()` has no production caller

### Phase 2: Enable/Disable — shipped
- [x] Per-plugin state tracking (`LoadedPlugin` + `PluginState`)
- [x] Enable/Disable API (`EnableDynamicPlugin` / `DisableDynamicPlugin`)
- [x] Plugin Manager panel UI (`PluginManagerPlugin`)

### Phase 3: Hot Reload — shipped, except auto-reload
- [x] Collectible context support
- [x] Resource tracking in context (`EditorPluginContext` subscription tracking, `UnloadDiagnostics` leak reporting)
- [x] Unload/reload API (`UnloadDynamicPlugin` / `ReloadDynamicPlugin`), with `IStatefulPlugin.SaveState`/`RestoreState` state preservation
- Not yet implemented: development mode auto-reload on rebuild

### Phase 4: NuGet Integration — shipped
- [x] `keeneyes plugin install|uninstall|update|list|search` CLI (plus `keeneyes sources add|list|remove`)
- [x] In-editor package browser (Plugin Manager Browse tab)
- [x] Version upgrade handling (`PluginUpdateCommand` over `PluginInstaller`/`NuGetClient`)

## Related

- [ADR-012: Editor Plugin Extension Architecture](012-editor-plugin-extension-architecture.md)
- [ADR-007: Capability-Based Plugin Architecture](007-capability-based-plugin-architecture.md)
- [Editor Plugin Hot Reload guide](../editor-plugin-hot-reload.md)
- [Editor Plugin Dependencies guide](../editor-plugin-dependencies.md)
- [.NET AssemblyLoadContext docs](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Accepted — the infrastructure landed in the same commit as the ADR (f678a831) and the index already listed it as Accepted. Implementation marked Partial: development-mode auto-reload of plugins, startup auto-load of installed plugins (`DiscoverPlugins()` has no callers; loading is on-demand), and a published plugin-manifest JSON Schema remain unimplemented. Body amended to as-built reality: shipped API names (`EnableDynamicPlugin`/`DisableDynamicPlugin`/`UnloadDynamicPlugin`/`ReloadDynamicPlugin`; install via `PluginInstaller` + `keeneyes plugin` CLI, not `InstallFromNuGet`), real component decomposition (no `PluginLifecycle` type; `PluginRegistry` is the persisted install registry; `Dependencies/`, `Installation/`, `NuGet/`, `Security/` subsystems), actual `PluginState` set (adds `Failed`/`Unloading`, no terminal `Unloaded`), `EditorPluginContext` subscription tracking plus `IStatefulPlugin`/`UnloadDiagnostics`, manifest contract defined by `PluginManifest.cs` (dead `$schema` URL removed; `security`/`permissions` sections documented), and the phase checklist updated to shipped status.
- **v1 — 2026-01-02 (f678a831):** Accepted — tiered dynamic plugin loading for editor plugins (Tier 1 static, Tier 2 enable/disable, Tier 3 collectible-ALC hot reload) with NuGet-package distribution and keeneyes-plugin.json manifests; landed together with the initial infrastructure (PluginLoadContext, PluginLoader, PluginRepository, PluginManifest, LoadedPlugin, EditorPluginManager dynamic-plugin API).
