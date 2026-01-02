# ADR-013: Dynamic Plugin Loading

**Status:** Proposed
**Date:** 2026-01-02

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
- Loaded at startup, require editor restart to add/remove
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
  "$schema": "https://keeneyes.dev/schemas/plugin-manifest-v1.json",
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

### Architecture

```
EditorPluginManager
├── PluginRepository           # Discovers installed plugins
│   ├── Scan NuGet global cache
│   ├── Scan local plugin folder
│   └── Parse manifests
│
├── PluginLoader               # Loads/unloads assemblies
│   ├── Create PluginLoadContext (collectible if hot-reload)
│   ├── Load assembly + dependencies
│   ├── Instantiate IEditorPlugin via reflection
│   └── Unload context (if collectible)
│
├── PluginRegistry             # Tracks loaded plugins
│   ├── Plugin metadata
│   ├── Load state (Unloaded, Loaded, Enabled, Disabled)
│   └── Dependency graph
│
└── PluginLifecycle            # Manages state transitions
    ├── Load → Enable → Disable → Unload
    ├── Dependency ordering
    └── Error recovery
```

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

```
                    ┌──────────────┐
         install   │  Discovered  │   scan
            ┌──────│   (on disk)  │◄──────┐
            │      └──────────────┘       │
            │                             │
            ▼                             │
    ┌──────────────┐              ┌───────┴──────┐
    │    Loaded    │◄────────────►│   Unloaded   │
    │  (in memory) │   unload*    │  (assembly   │
    └──────┬───────┘              │   released)  │
           │                      └──────────────┘
           │ enable                     ▲
           ▼                            │ unload*
    ┌──────────────┐                    │
    │   Enabled    │────────────────────┘
    │  (running)   │     disable + unload*
    └──────┬───────┘
           │ disable
           ▼
    ┌──────────────┐
    │   Disabled   │
    │  (sleeping)  │
    └──────────────┘

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

The `EditorPluginContext` tracks all resources and disposes them on unload:

```csharp
public sealed class EditorPluginContext : IEditorContext, IDisposable
{
    private readonly List<EventSubscription> subscriptions = [];
    private readonly List<Entity> createdPanels = [];
    private readonly WeakReference<IEditorPlugin> pluginRef;

    public void Dispose()
    {
        // Dispose subscriptions (removes event handlers)
        foreach (var sub in subscriptions)
            sub.Dispose();

        // Destroy created UI entities
        foreach (var entity in createdPanels)
            EditorWorld.Despawn(entity);

        // Clear capability registrations
        // ...
    }
}
```

### Error Handling

Plugin failures are isolated:

```csharp
public void EnablePlugin(string pluginId)
{
    var entry = registry.Get(pluginId);
    var context = new EditorPluginContext(this, entry.Manifest);

    try
    {
        entry.Plugin.Initialize(context);
        entry.State = PluginState.Enabled;
    }
    catch (Exception ex)
    {
        // Log error, keep plugin in Loaded state
        logger.Error($"Plugin {pluginId} failed to initialize: {ex}");
        context.Dispose();
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

```csharp
// Plugin management
editorPlugins.InstallFromNuGet("com.example.myplugin", "1.0.0");
editorPlugins.InstallFromPath("/path/to/MyPlugin.dll");
editorPlugins.UninstallPlugin("com.example.myplugin");

// Enable/disable
editorPlugins.EnablePlugin("com.example.myplugin");
editorPlugins.DisablePlugin("com.example.myplugin");

// Hot reload (opt-in plugins only)
editorPlugins.ReloadPlugin("com.example.myplugin");

// Query
var plugin = editorPlugins.GetPlugin("com.example.myplugin");
var all = editorPlugins.GetAllPlugins();
var enabled = editorPlugins.GetEnabledPlugins();
```

### User Experience

1. **Plugin Manager Panel** - UI for browsing, installing, enabling plugins
2. **Restart Indicator** - Shows when restart is needed for full changes
3. **Error Recovery** - Disable failing plugins, offer to uninstall
4. **Development Mode** - Auto-reload on rebuild (for plugin developers)

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

### Phase 1: Static Loading
- Plugin manifest schema
- Plugin discovery from NuGet cache
- PluginLoadContext with dependency resolution
- Basic PluginLoader (load-only)

### Phase 2: Enable/Disable
- PluginRegistry with state tracking
- Enable/Disable API
- Plugin Manager panel UI

### Phase 3: Hot Reload
- Collectible context support
- Resource tracking in context
- Unload API
- Development mode auto-reload

### Phase 4: NuGet Integration
- `keeneyes plugin install <package>` CLI
- In-editor package browser
- Version upgrade handling

## Related

- [ADR-012: Editor Plugin Extension Architecture](012-editor-plugin-extension-architecture.md)
- [ADR-007: Capability-Based Plugin Architecture](007-capability-based-plugin-architecture.md)
- [.NET AssemblyLoadContext docs](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
