# Editor Plugin Hot Reload

This guide covers hot reloading for editor plugins, including requirements, limitations, state preservation, and debugging tips.

## Overview

Hot reload allows editor plugins to be unloaded and reloaded without restarting the editor. This enables rapid iteration during plugin development.

### How Hot Reload Works

1. **Plugin disabled** - The plugin's `Shutdown()` method is called
2. **State captured** - For `IStatefulPlugin` implementations, `SaveState()` is called
3. **Assembly unloaded** - The plugin's assembly is unloaded via `AssemblyLoadContext`
4. **Assembly reloaded** - The updated assembly is loaded into a new context
5. **Plugin enabled** - `Initialize()` is called on the new instance
6. **State restored** - For `IStatefulPlugin`, `RestoreState()` is called

## Requirements for Hot-Reloadable Plugins

### Manifest Configuration

Set `supportsHotReload: true` in your plugin manifest:

```json
{
  "id": "com.example.my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "capabilities": {
    "supportsHotReload": true,
    "supportsDisable": true
  }
}
```

### Resource Cleanup

Plugins must properly clean up all resources in `Shutdown()`:

```csharp
public class MyPlugin : EditorPluginBase
{
    private EventSubscription? selectionSub;

    public override void Initialize(IEditorContext context)
    {
        selectionSub = context.OnSelectionChanged(OnSelectionChanged);
    }

    public override void Shutdown()
    {
        // CRITICAL: Dispose all subscriptions
        selectionSub?.Dispose();
    }
}
```

### Avoid Static State

Static fields survive assembly unload and can cause memory leaks:

```csharp
// ❌ BAD: Static state prevents unload
public class BadPlugin : EditorPluginBase
{
    private static List<Entity> cachedEntities = [];
}

// ✅ GOOD: Instance state is properly managed
public class GoodPlugin : EditorPluginBase
{
    private readonly List<Entity> cachedEntities = [];
}
```

## State Preservation

Implement `IStatefulPlugin` to preserve state across hot reloads:

```csharp
public class MyPlugin : EditorPluginBase, IStatefulPlugin
{
    private bool showOverlay = true;
    private int selectedTabIndex = 0;

    public object? SaveState()
    {
        // Return a simple dictionary - NOT plugin-defined types!
        return new Dictionary<string, object>
        {
            ["ShowOverlay"] = showOverlay,
            ["SelectedTab"] = selectedTabIndex
        };
    }

    public void RestoreState(object? state)
    {
        if (state is Dictionary<string, object> dict)
        {
            showOverlay = dict.GetValueOrDefault("ShowOverlay") as bool? ?? true;
            selectedTabIndex = dict.GetValueOrDefault("SelectedTab") as int? ?? 0;
        }
    }
}
```

### State Object Guidelines

The state object **must not** reference types from your plugin's assembly:

| Safe Types | Unsafe Types |
|------------|--------------|
| Primitives (int, float, string) | Plugin-defined classes |
| Collections of primitives | Plugin-defined structs |
| `Dictionary<string, object>` | Types referencing plugin types |
| Types from KeenEyes.Editor.Abstractions | Entity handles (stale after reload) |

**Why?** Types from the old assembly cannot be deserialized into the new assembly after reload.

### Panel State Preservation

Panel positions and sizes are automatically preserved by the editor's `PanelStateStore`. No additional code is needed for basic panel layout preservation.

For custom panel state, use `IStatefulPlugin`:

```csharp
public class MyPanelPlugin : EditorPluginBase, IStatefulPlugin
{
    private MyPanel? panel;

    public object? SaveState()
    {
        return new Dictionary<string, object>
        {
            ["ScrollPosition"] = panel?.ScrollY ?? 0f,
            ["ExpandedNodes"] = panel?.GetExpandedNodeIds() ?? []
        };
    }

    public void RestoreState(object? state)
    {
        // State will be applied after panel is recreated
        if (state is Dictionary<string, object> dict && panel != null)
        {
            panel.ScrollY = dict.GetValueOrDefault("ScrollPosition") as float? ?? 0f;
            var expanded = dict.GetValueOrDefault("ExpandedNodes") as List<string>;
            if (expanded != null) panel.SetExpandedNodes(expanded);
        }
    }
}
```

## Debugging Hot Reload Issues

### Unload Diagnostics

When a plugin fails to fully unload, the editor provides diagnostics:

```
Plugin 'com.example.my-plugin' may not have fully unloaded.
There may be lingering references.

Potential holders:
- 2 event subscription(s)
- Panel: MyCustomPanel
- PropertyDrawer: ColorDrawer

Resource counts:
- Subscriptions: 2
- Panels: 1
- PropertyDrawers: 1
```

### Common Causes of Unload Failure

1. **Undisposed subscriptions** - Event handlers keeping references
2. **Static fields** - Static collections referencing plugin types
3. **Closures** - Lambdas capturing plugin instances
4. **Cached types** - Reflection-based caches holding Type references

### Type Cache Integration

The editor clears type caches when plugins unload. If you maintain type caches in your plugin, register a clear callback:

```csharp
// In your capability implementation
public class MyCapability : IEditorCapability
{
    private readonly Dictionary<Type, object> typeCache = [];
    private readonly TypeCacheManager cacheManager;

    public MyCapability(TypeCacheManager cacheManager)
    {
        this.cacheManager = cacheManager;
        cacheManager.RegisterClearCallback(OnPluginUnloading);
    }

    private void OnPluginUnloading(string pluginId)
    {
        // Clear any cached types from this plugin
        var keysToRemove = typeCache.Keys
            .Where(t => t.Assembly.GetName().Name?.Contains(pluginId) ?? false)
            .ToList();

        foreach (var key in keysToRemove)
        {
            typeCache.Remove(key);
        }
    }
}
```

## Limitations

### Type Identity

After reload, types from your plugin are **new types**, even if they have the same name:

```csharp
// Before reload: typeof(MyComponent)
// After reload: typeof(MyComponent) <- Different Type object!

// This means:
// - Type comparisons fail across reload
// - Generic type caches become invalid
// - Reflection-based registrations need refresh
```

### Entity References

Entity handles captured before reload remain valid, but component references may need refresh:

```csharp
// ❌ BAD: Stale reference after reload
private ref Position posRef; // Invalid after reload!

// ✅ GOOD: Re-fetch reference each use
private Entity trackedEntity;
// In update: ref var pos = ref world.Get<Position>(trackedEntity);
```

### Services and Singletons

Services obtained from `IEditorContext` are stable across reloads, but your plugin's references to them must be re-established in `Initialize()`.

## Best Practices Checklist

- [ ] Set `supportsHotReload: true` in manifest
- [ ] Dispose all event subscriptions in `Shutdown()`
- [ ] Avoid static fields that reference plugin types
- [ ] Implement `IStatefulPlugin` for UI state preservation
- [ ] Use only primitive types in state objects
- [ ] Don't cache `ref` returns across frames
- [ ] Re-establish service references in `Initialize()`
- [ ] Test hot reload during development

## See Also

- [ADR-012: Editor Plugin Extension Architecture](adr/012-editor-plugin-extension-architecture.md)
- [ADR-013: Dynamic Plugin Loading](adr/013-dynamic-plugin-loading.md)
- [IStatefulPlugin API Reference](api/KeenEyes.Editor.Abstractions/IStatefulPlugin.md)
