# Editor Plugin Dependencies

This guide covers dependency management for editor plugins, including version constraints, resolution order, and error handling.

## Overview

Plugins can declare dependencies on other plugins. The dependency system ensures:

1. **Dependencies load first** - A plugin's dependencies are loaded before the plugin itself
2. **Version compatibility** - Dependencies must satisfy version constraints
3. **Cycle detection** - Circular dependencies are detected and rejected
4. **Safe unloading** - Plugins cannot be unloaded while other plugins depend on them

## Declaring Dependencies

Add a `dependencies` section to your plugin manifest (`keeneyes-plugin.json`):

```json
{
  "id": "com.example.my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",

  "dependencies": {
    "com.keeneyes.physics-editor": ">=1.0.0",
    "com.keeneyes.ui-toolkit": "^2.0.0",
    "com.example.core-utilities": "~1.5.0"
  }
}
```

Each dependency is a key-value pair:
- **Key**: The plugin ID to depend on
- **Value**: A version constraint string

## Version Constraint Syntax

The dependency system supports semantic versioning (SemVer) with flexible constraint syntax:

### Exact Version

```json
"dependencies": {
  "other-plugin": "1.0.0"
}
```

Requires exactly version `1.0.0`. **Not recommended** - prevents users from getting compatible updates.

### Minimum Version (>=)

```json
"dependencies": {
  "other-plugin": ">=1.0.0"
}
```

Requires version `1.0.0` or higher. Allows any newer version.

| Installed Version | Satisfies `>=1.0.0`? |
|-------------------|---------------------|
| `0.9.0` | No |
| `1.0.0` | Yes |
| `1.5.0` | Yes |
| `2.0.0` | Yes |

### Greater Than (>)

```json
"dependencies": {
  "other-plugin": ">1.0.0"
}
```

Requires a version newer than `1.0.0` (not including `1.0.0` itself).

### Less Than (<, <=)

```json
"dependencies": {
  "other-plugin": "<2.0.0",
  "another-plugin": "<=1.5.0"
}
```

Requires versions below a threshold.

### Caret (^) - Compatible With

```json
"dependencies": {
  "other-plugin": "^1.2.0"
}
```

Allows changes that do not modify the **left-most non-zero digit**. This is the **recommended** constraint for most dependencies.

| Constraint | Allowed Range | Description |
|------------|---------------|-------------|
| `^1.2.3` | `[1.2.3, 2.0.0)` | 1.x.x where x >= 2.3 |
| `^0.2.3` | `[0.2.3, 0.3.0)` | 0.2.x where x >= 3 |
| `^0.0.3` | `[0.0.3, 0.0.4)` | Only 0.0.3 exactly |

**Examples:**

| Installed | Satisfies `^1.2.0`? |
|-----------|---------------------|
| `1.2.0` | Yes |
| `1.5.0` | Yes |
| `1.9.9` | Yes |
| `2.0.0` | No (major changed) |
| `0.9.0` | No (below minimum) |

### Tilde (~) - Approximately Equivalent

```json
"dependencies": {
  "other-plugin": "~1.2.0"
}
```

Allows **patch-level** changes only (same major.minor).

| Constraint | Allowed Range | Description |
|------------|---------------|-------------|
| `~1.2.3` | `[1.2.3, 1.3.0)` | 1.2.x where x >= 3 |

**Examples:**

| Installed | Satisfies `~1.2.0`? |
|-----------|---------------------|
| `1.2.0` | Yes |
| `1.2.5` | Yes |
| `1.2.99` | Yes |
| `1.3.0` | No (minor changed) |
| `1.1.0` | No (below minimum) |

## Editor Version Compatibility

Use `compatibility` to specify which editor versions your plugin supports:

```json
{
  "id": "com.example.my-plugin",
  "version": "1.0.0",

  "compatibility": {
    "minEditorVersion": "1.0.0",
    "maxEditorVersion": "2.0.0"
  }
}
```

| Field | Description |
|-------|-------------|
| `minEditorVersion` | Minimum editor version required (inclusive) |
| `maxEditorVersion` | Maximum editor version supported (exclusive) |

Both fields are optional. Omitting means no constraint for that bound.

## Dependency Resolution

When plugins are loaded, the dependency resolver:

1. **Parses constraints** - Validates all version constraint syntax
2. **Checks editor compatibility** - Ensures each plugin works with the current editor
3. **Builds dependency graph** - Maps which plugins depend on which
4. **Detects cycles** - Finds and reports circular dependencies
5. **Validates versions** - Checks installed versions satisfy constraints
6. **Computes load order** - Topologically sorts for safe loading

### Load Order Example

Given plugins:
- `core` (no dependencies)
- `utils` depends on `core ^1.0.0`
- `ui` depends on `utils ^2.0.0`
- `app` depends on `ui ^1.0.0` and `core ^1.0.0`

Load order: `core` → `utils` → `ui` → `app`

Dependencies always load before their dependents.

### Unload Order

When unloading, the order reverses. A plugin cannot be unloaded while other enabled plugins depend on it.

Unload order for the example: `app` → `ui` → `utils` → `core`

## Error Messages

### Missing Dependency

```
Plugin 'com.example.my-app' requires 'com.keeneyes.core' (>=1.0.0) but it is not installed.
```

**Cause**: A required plugin is not installed.

**Fix**: Install the missing plugin:
```bash
keeneyes plugin install com.keeneyes.core
```

### Version Mismatch

```
Plugin 'com.example.my-app' requires 'com.keeneyes.core' (>=2.0.0) but version 1.5.0 is installed.
```

**Cause**: The installed version doesn't satisfy the version constraint.

**Fix**: Upgrade the dependency:
```bash
keeneyes plugin install com.keeneyes.core --version 2.0.0
```

### Circular Dependency

```
Circular dependency detected: com.example.a → com.example.b → com.example.c → com.example.a
```

**Cause**: Plugins depend on each other in a cycle.

**Fix**: Refactor plugins to remove the cycle. Common solutions:
- Extract shared code into a separate plugin both can depend on
- Use optional/runtime dependencies instead of hard dependencies
- Merge tightly coupled plugins

### Editor Version Incompatible

```
Plugin 'com.example.my-plugin' requires editor version >=2.0.0, current editor is 1.5.0.
```

**Cause**: Your editor version is too old for this plugin.

**Fix**: Either upgrade your editor or use an older plugin version:
```bash
keeneyes plugin install com.example.my-plugin --version 1.x
```

Or if the current editor is too new:

```
Plugin 'com.example.legacy' is not compatible with editor version 3.0.0 (max: 2.0.0).
```

**Fix**: Contact the plugin author for an update, or downgrade your editor.

## Best Practices

### Choosing Constraints

| Scenario | Recommended Constraint |
|----------|----------------------|
| General dependency | `^1.0.0` (caret) |
| Need specific minor features | `>=1.2.0` |
| Unstable pre-1.0 dependency | `~0.5.0` (tilde) |
| Must not break on major change | `^1.0.0 <2.0.0` |

### Avoid Overly Strict Constraints

```json
// ❌ BAD: Too strict, forces exact version
"dependencies": {
  "core": "1.0.0"
}

// ✅ GOOD: Allows compatible updates
"dependencies": {
  "core": "^1.0.0"
}
```

### Declare All Direct Dependencies

```json
// ❌ BAD: Relying on transitive dependency
// Your plugin uses 'utils' but doesn't declare it
"dependencies": {
  "framework": "^1.0.0"  // framework depends on utils
}

// ✅ GOOD: Explicitly declare what you use
"dependencies": {
  "framework": "^1.0.0",
  "utils": "^2.0.0"
}
```

### Test with Dependency Ranges

Test your plugin with both minimum and maximum versions of dependencies to ensure compatibility across the allowed range.

### Document Breaking Changes

When publishing a new major version, document what changed so dependent plugins know what to update:

```markdown
## Breaking Changes in 2.0.0

- `OldApi.DoThing()` renamed to `NewApi.PerformAction()`
- Removed deprecated `LegacyHelper` class
- Minimum .NET version changed from 8.0 to 10.0
```

## Programmatic Access

Plugins can query dependency information at runtime:

```csharp
public class MyPlugin : EditorPluginBase
{
    public override void Initialize(IEditorContext context)
    {
        // Check if an optional dependency is available
        if (context.PluginManager.HasPlugin("com.optional.feature"))
        {
            var feature = context.PluginManager.GetPlugin("com.optional.feature");
            // Use the optional feature
        }

        // Get all enabled plugins
        foreach (var plugin in context.PluginManager.GetEnabledPlugins())
        {
            Console.WriteLine($"Enabled: {plugin.Manifest.Name} v{plugin.Manifest.Version}");
        }
    }
}
```

## See Also

- [Editor Plugin Hot Reload](editor-plugin-hot-reload.md) - Hot reload requirements and limitations
- [ADR-013: Dynamic Plugin Loading](adr/013-dynamic-plugin-loading.md) - Architecture decisions
- [Plugin System Guide](plugins.md) - World plugin system (different from editor plugins)
