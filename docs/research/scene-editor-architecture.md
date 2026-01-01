# Scene/World Editor Architecture

**Date:** December 2024
**Status:** Research / Planning
**Author:** Claude (Anthropic)

## Executive Summary

This document outlines the complete architecture for a Unity/Godot-class scene and world editor for KeenEyes. The editor leverages KeenEyes' existing infrastructure (~95% complete) and uses the custom MSBuild SDK for project detection and asset tracking.

**Key Architectural Decisions:**
- **Project-centric**: Editor opens `.csproj` or `.slnx` files, not custom project formats
- **SDK-based detection**: `KeenEyes.Sdk` provides project metadata and custom item types
- **Source-generated assets**: `.kescene`, `.keprefab`, `.keworld` files compile to C# code (Native AOT safe)
- **Multi-world isolation**: Separate editor and game worlds
- **Plugin-based features**: Editor functionality delivered via `IEditorPlugin` plugins
- **ECS-native UI**: Uses existing `KeenEyes.UI` widget system

---

## Table of Contents

1. [Project System](#1-project-system)
2. [Editor Application Shell](#2-editor-application-shell)
3. [Scene Hierarchy Panel](#3-scene-hierarchy-panel)
4. [Inspector Panel](#4-inspector-panel)
5. [Scene Viewport](#5-scene-viewport)
6. [Asset Management](#6-asset-management)
7. [Undo/Redo System](#7-undoredo-system)
8. [Play Mode](#8-play-mode)
9. [Hot Reload](#9-hot-reload)
10. [Debugging & Profiling](#10-debugging--profiling)
11. [Graph Node Editor](#11-graph-node-editor)
12. [Build Pipeline](#12-build-pipeline)
13. [Source Generated Assets](#13-source-generated-assets)
14. [Editor Plugin System](#14-editor-plugin-system)
15. [Implementation Phases](#15-implementation-phases)

---

## 1. Project System

### 1.1 Project Detection

The editor opens standard MSBuild project files (`.csproj`) or solution files (`.slnx`). KeenEyes projects are detected via the custom SDK.

**Detection Flow:**
```
User opens MyGame.csproj (or MyGame.slnx)
    ↓
Editor reads project XML
    ↓
Check for SDK="KeenEyes.Sdk/x.x.x" or $(IsKeenEyesProject)=true
    ↓
If KeenEyes project → Load into editor
If not → Show error "Not a KeenEyes project"
```

**Project Detection Markers:**
```xml
<!-- Option 1: SDK-style project -->
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <!-- KeenEyes project detected via SDK -->
</Project>

<!-- Option 2: Traditional with SDK package -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsKeenEyesProject>true</IsKeenEyesProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="KeenEyes.Sdk" Version="0.1.0" />
  </ItemGroup>
</Project>
```

### 1.2 Solution Support (.slnx)

For multi-project games, the editor supports the new `.slnx` format (XML-based solutions):

```xml
<Solution>
  <Project Path="src/MyGame/MyGame.csproj" Type="Game" />
  <Project Path="src/MyGame.Plugins/MyGame.Plugins.csproj" Type="Plugin" />
  <Project Path="src/MyGame.Shared/MyGame.Shared.csproj" Type="Library" />
</Solution>
```

**Editor Behavior with Solutions:**
- Detects all KeenEyes projects in solution
- Primary game project (first `KeenEyes.Sdk` project) is the "active" project
- Plugin projects are available for hot-reload
- Shared libraries are referenced but not directly edited

### 1.3 Project Metadata

The SDK generates `keeneyes.project.json` in the output directory:

```json
{
  "sdkVersion": "0.1.0",
  "coreVersion": "0.1.0",
  "projectType": "Game",
  "targetFramework": "net10.0",
  "scenes": ["Scenes/Main.kescene", "Scenes/Menu.kescene"],
  "prefabs": ["Prefabs/Player.keprefab", "Prefabs/Enemy.keprefab"],
  "worlds": ["Worlds/Default.keworld"]
}
```

### 1.4 Custom Item Types

The SDK defines item types that the editor tracks:

| Item Type | File Extension | Purpose |
|-----------|----------------|---------|
| `<KeenEyesScene>` | `.kescene` | Scene definitions |
| `<KeenEyesPrefab>` | `.keprefab` | Prefab templates |
| `<KeenEyesWorld>` | `.keworld` | World configurations |
| `<KeenEyesAsset>` | `*` | General game assets |

**Example Project Structure:**
```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <ItemGroup>
    <!-- Auto-detected by convention -->
    <KeenEyesScene Include="Scenes/**/*.kescene" />
    <KeenEyesPrefab Include="Prefabs/**/*.keprefab" />
    <KeenEyesAsset Include="Assets/**/*" />
  </ItemGroup>
</Project>
```

### 1.5 Project Browser Integration

The editor's Project panel mirrors the csproj structure:

```
MyGame (Project Root)
├── Scenes/
│   ├── Main.kescene        [KeenEyesScene]
│   └── Menu.kescene        [KeenEyesScene]
├── Prefabs/
│   ├── Player.keprefab     [KeenEyesPrefab]
│   └── Enemy.keprefab      [KeenEyesPrefab]
├── Assets/
│   ├── Textures/           [KeenEyesAsset]
│   ├── Audio/              [KeenEyesAsset]
│   └── Models/             [KeenEyesAsset]
├── Scripts/
│   ├── Systems/            [C# source]
│   └── Components/         [C# source]
└── MyGame.csproj           [Project file]
```

### 1.6 Project State

```csharp
public sealed class EditorProject
{
    public string ProjectPath { get; }          // Path to .csproj
    public string? SolutionPath { get; }        // Path to .slnx (if opened via solution)
    public string RootDirectory { get; }        // Project root folder
    public string OutputDirectory { get; }      // bin/Debug/net10.0

    public string SdkVersion { get; }           // KeenEyes.Sdk version
    public string CoreVersion { get; }          // KeenEyes.Core version
    public ProjectType Type { get; }            // Game, Plugin, Library

    public IReadOnlyList<string> Scenes { get; }
    public IReadOnlyList<string> Prefabs { get; }
    public IReadOnlyList<string> Worlds { get; }
    public IReadOnlyList<string> Assets { get; }

    public bool IsDirty { get; }                // Unsaved changes
    public DateTime LastBuildTime { get; }
}

public enum ProjectType { Game, Plugin, Library }
```

---

## 2. Editor Application Shell

### 2.1 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Editor Application                           │
├─────────────────────────────────────────────────────────────────────┤
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                         Menu Bar                               │  │
│  │  File  Edit  View  Scene  Entity  Component  Window  Help      │  │
│  └───────────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                         Toolbar                                │  │
│  │  [▶ Play] [⏸ Pause] [⏭ Step] | [Move] [Rotate] [Scale] | ...  │  │
│  └───────────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┬───────────────────────────┬───────────────────┐  │
│  │  Hierarchy  │                           │    Inspector      │  │
│  │             │                           │                   │  │
│  │  ▼ Root     │      Scene Viewport       │  [Entity Name]    │  │
│  │    Player   │                           │                   │  │
│  │    ▼ Enemies│                           │  ▼ Transform      │  │
│  │      Goblin │                           │    Position: ...  │  │
│  │      Orc    │                           │    Rotation: ...  │  │
│  │    Camera   │                           │                   │  │
│  │             │                           │  ▼ Health         │  │
│  ├─────────────┤                           │    Current: 100   │  │
│  │  Project    │                           │    Max: 100       │  │
│  │             │                           │                   │  │
│  │  📁 Scenes  ├───────────────────────────┤  [Add Component]  │  │
│  │  📁 Prefabs │        Console            │                   │  │
│  │  📁 Assets  │  [Info] [Warn] [Error]    │                   │  │
│  │  📁 Scripts │  > Scene loaded: Main     │                   │  │
│  └─────────────┴───────────────────────────┴───────────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│  Status: Ready | Entities: 142 | FPS: 60 | Memory: 128 MB          │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Multi-World Architecture

The editor uses separate worlds for isolation:

```csharp
public sealed class EditorWorldManager
{
    // Editor UI world - panels, menus, dialogs
    public World EditorWorld { get; }

    // Currently open scene world - game entities
    public World? SceneWorld { get; private set; }

    // Preview world for prefab editing
    public World? PreviewWorld { get; private set; }

    // Play mode snapshot (for restoring after exit)
    private WorldSnapshot? playModeSnapshot;

    public void OpenScene(string scenePath)
    {
        SceneWorld?.Dispose();
        SceneWorld = new World(name: Path.GetFileName(scenePath));
        LoadSceneFile(scenePath, SceneWorld);
    }

    public void EnterPlayMode()
    {
        playModeSnapshot = SceneWorld?.CreateSnapshot();
        // Systems start updating
    }

    public void ExitPlayMode()
    {
        if (playModeSnapshot != null && SceneWorld != null)
        {
            playModeSnapshot.RestoreTo(SceneWorld);
            playModeSnapshot = null;
        }
    }
}
```

### 2.3 Layout Persistence

Editor layout saved to user settings:

```json
{
  "layout": {
    "docks": [
      { "panel": "Hierarchy", "position": "left", "width": 250 },
      { "panel": "Inspector", "position": "right", "width": 300 },
      { "panel": "Console", "position": "bottom", "height": 200 },
      { "panel": "Project", "position": "left", "tabWith": "Hierarchy" }
    ],
    "viewport": { "camera": "perspective", "showGrid": true }
  },
  "recentProjects": [
    "C:/Projects/MyGame/MyGame.csproj",
    "C:/Projects/Demo/Demo.slnx"
  ]
}
```

### 2.4 Existing UI Components

KeenEyes.UI provides all necessary widgets:

| Widget | Editor Use |
|--------|------------|
| `DockContainer` | Main layout with draggable panels |
| `TreeView` | Hierarchy and Project panels |
| `PropertyGrid` | Inspector component display |
| `MenuBar` | Top menu with submenus |
| `Toolbar` | Play controls, tool selection |
| `StatusBar` | Bottom status display |
| `TabView` | Multiple open scenes |
| `Window` | Floating dialogs |
| `Modal` | Confirmation dialogs |
| `ContextMenu` | Right-click menus |
| `TextField`, `Slider`, `Checkbox` | Property editors |
| `ScrollView` | Scrollable panels |

---

## 3. Scene Hierarchy Panel

### 3.1 Features

| Feature | Implementation |
|---------|----------------|
| Entity tree display | `TreeView` + `World.GetChildren()` |
| Expand/collapse | Built into `TreeView` |
| Drag reparenting | Drag events + `World.SetParent()` |
| Multi-select | Shift/Ctrl + selection manager |
| Context menu | Right-click: Create, Delete, Duplicate |
| Search filter | Filter by name, tag, component |
| Visibility toggle | `EditorMetadata.IsHidden` component |
| Lock toggle | `EditorMetadata.IsLocked` component |

### 3.2 Entity Display

```csharp
public sealed class HierarchyPanel
{
    private readonly World sceneWorld;
    private readonly SelectionManager selection;

    public void BuildTree(TreeView tree)
    {
        tree.Clear();

        // Get root entities (no parent)
        var roots = sceneWorld.GetAllEntities()
            .Where(e => sceneWorld.GetParent(e) == Entity.Null)
            .OrderBy(e => sceneWorld.GetName(e) ?? $"Entity_{e.Id}");

        foreach (var entity in roots)
        {
            AddEntityNode(tree.Root, entity);
        }
    }

    private void AddEntityNode(TreeNode parent, Entity entity)
    {
        var name = sceneWorld.GetName(entity) ?? $"Entity_{entity.Id}";
        var icon = GetEntityIcon(entity);

        var node = parent.AddChild(name, icon, entity);
        node.IsSelected = selection.IsSelected(entity);

        foreach (var child in sceneWorld.GetChildren(entity))
        {
            AddEntityNode(node, child);
        }
    }

    private string GetEntityIcon(Entity entity)
    {
        // Return icon based on primary component
        if (sceneWorld.Has<Camera>(entity)) return "🎥";
        if (sceneWorld.Has<Light>(entity)) return "💡";
        if (sceneWorld.Has<RigidBody>(entity)) return "🔷";
        if (sceneWorld.Has<AudioSource>(entity)) return "🔊";
        return "📦";
    }
}
```

### 3.3 Context Menu Actions

```csharp
public enum HierarchyAction
{
    CreateEmpty,
    CreateFromPrefab,
    Duplicate,
    Delete,
    Rename,
    CopyEntity,
    PasteEntity,
    SelectChildren,
    FocusInViewport,
    SaveAsPrefab
}
```

---

## 4. Inspector Panel

### 4.1 Component Display

```
┌─────────────────────────────────────┐
│  Player                        [⋮]  │
├─────────────────────────────────────┤
│  ▼ Transform                   [x]  │
│    Position                         │
│      X: [  10.5  ] Y: [  0.0  ]    │
│      Z: [   0.0  ]                  │
│    Rotation                         │
│      X: [   0.0  ] Y: [ 45.0  ]    │
│      Z: [   0.0  ]                  │
│    Scale                            │
│      X: [   1.0  ] Y: [  1.0  ]    │
│      Z: [   1.0  ]                  │
├─────────────────────────────────────┤
│  ▼ Health                      [x]  │
│    Current    [████████░░] 80/100   │
│    Max        [ 100 ]               │
│    Regen Rate [ 5.0 ] per second    │
├─────────────────────────────────────┤
│  ▼ PlayerController            [x]  │
│    Move Speed [ 10.0 ] ─────○───    │
│    Jump Force [ 15.0 ]              │
│    ☑ Can Double Jump               │
├─────────────────────────────────────┤
│  [+] Add Component                  │
└─────────────────────────────────────┘
```

### 4.2 Property Metadata Attributes

```csharp
namespace KeenEyes.Editor;

/// <summary>Restricts numeric field to a range with slider.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class RangeAttribute(float min, float max) : Attribute
{
    public float Min => min;
    public float Max => max;
}

/// <summary>Displays tooltip on hover.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct)]
public sealed class TooltipAttribute(string text) : Attribute
{
    public string Text => text;
}

/// <summary>Adds header above field group.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HeaderAttribute(string text) : Attribute
{
    public string Text => text;
}

/// <summary>Adds vertical spacing.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SpaceAttribute(float height = 8) : Attribute
{
    public float Height => height;
}

/// <summary>Hides field from inspector.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HideInInspectorAttribute : Attribute;

/// <summary>Shows color picker for uint/Color fields.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ColorPickerAttribute : Attribute;

/// <summary>Makes field read-only in inspector.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ReadOnlyAttribute : Attribute;

/// <summary>Shows as multiline text area.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TextAreaAttribute(int minLines = 3, int maxLines = 10) : Attribute
{
    public int MinLines => minLines;
    public int MaxLines => maxLines;
}

/// <summary>Shows entity reference picker.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class EntityRefAttribute : Attribute;

/// <summary>Shows asset picker for specific type.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class AssetRefAttribute(Type assetType) : Attribute
{
    public Type AssetType => assetType;
}
```

**Usage Example:**
```csharp
[Component]
public partial struct PlayerController
{
    [Header("Movement")]
    [Range(1f, 20f)]
    [Tooltip("Movement speed in units per second")]
    public float MoveSpeed;

    [Range(5f, 30f)]
    public float JumpForce;

    [Space]
    [Header("Abilities")]
    public bool CanDoubleJump;

    [HideInInspector]
    public int InternalState;

    [EntityRef]
    public Entity Target;
}
```

### 4.3 Component Inspector Implementation

```csharp
public sealed class ComponentInspector
{
    private readonly World world;

    public IEnumerable<FieldInfo> GetEditableFields(Type componentType)
    {
        return componentType
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !f.IsInitOnly)
            .Where(f => f.GetCustomAttribute<HideInInspectorAttribute>() == null);
    }

    public PropertyDrawer GetDrawer(FieldInfo field)
    {
        var fieldType = field.FieldType;

        // Check for explicit drawer attributes first
        if (field.GetCustomAttribute<RangeAttribute>() is { } range)
            return new SliderDrawer(range.Min, range.Max);
        if (field.GetCustomAttribute<ColorPickerAttribute>() != null)
            return new ColorPickerDrawer();
        if (field.GetCustomAttribute<TextAreaAttribute>() is { } textArea)
            return new TextAreaDrawer(textArea.MinLines, textArea.MaxLines);
        if (field.GetCustomAttribute<EntityRefAttribute>() != null)
            return new EntityRefDrawer(world);
        if (field.GetCustomAttribute<AssetRefAttribute>() is { } assetRef)
            return new AssetRefDrawer(assetRef.AssetType);

        // Default drawers by type
        return fieldType switch
        {
            _ when fieldType == typeof(float) => new FloatDrawer(),
            _ when fieldType == typeof(int) => new IntDrawer(),
            _ when fieldType == typeof(bool) => new BoolDrawer(),
            _ when fieldType == typeof(string) => new StringDrawer(),
            _ when fieldType == typeof(Vector2) => new Vector2Drawer(),
            _ when fieldType == typeof(Vector3) => new Vector3Drawer(),
            _ when fieldType == typeof(Vector4) => new Vector4Drawer(),
            _ when fieldType == typeof(Color) => new ColorPickerDrawer(),
            _ when fieldType == typeof(Entity) => new EntityRefDrawer(world),
            _ when fieldType.IsEnum => new EnumDrawer(fieldType),
            _ => new DefaultDrawer()
        };
    }
}
```

---

## 5. Scene Viewport

### 5.1 Viewport Features

| Feature | Status | Notes |
|---------|--------|-------|
| 3D/2D scene rendering | Requires integration | Use KeenEyes.Graphics |
| Editor camera | Missing | Orbit, pan, zoom, fly modes |
| Grid overlay | Partial | Basic grid exists |
| Transform gizmos | Missing | Move, rotate, scale handles |
| Selection outline | Missing | Highlight selected entities |
| Click selection | Partial | Need raycast integration |
| Box selection | Missing | Rectangle select |
| Entity icons | Missing | Camera, light, audio icons |

### 5.2 Editor Camera Controller

```csharp
public sealed class EditorCameraController
{
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public float Distance { get; set; } = 10f;

    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public CameraMode Mode { get; set; } = CameraMode.Orbit;

    public void ProcessInput(InputState input, float deltaTime)
    {
        switch (Mode)
        {
            case CameraMode.Orbit:
                if (input.IsMiddleMouseDown)
                {
                    if (input.IsAltDown)
                    {
                        // Orbit around target
                        Yaw += input.MouseDelta.X * 0.5f;
                        Pitch += input.MouseDelta.Y * 0.5f;
                    }
                    else
                    {
                        // Pan
                        var right = GetRightVector();
                        var up = GetUpVector();
                        Target -= right * input.MouseDelta.X * 0.01f * Distance;
                        Target += up * input.MouseDelta.Y * 0.01f * Distance;
                    }
                }
                // Zoom with scroll
                Distance *= 1f - input.ScrollDelta * 0.1f;
                break;

            case CameraMode.Fly:
                // WASD movement
                var forward = GetForwardVector();
                var right2 = GetRightVector();
                if (input.IsKeyDown(Key.W)) Position += forward * deltaTime * 10f;
                if (input.IsKeyDown(Key.S)) Position -= forward * deltaTime * 10f;
                if (input.IsKeyDown(Key.A)) Position -= right2 * deltaTime * 10f;
                if (input.IsKeyDown(Key.D)) Position += right2 * deltaTime * 10f;
                break;
        }
    }

    public void FocusOnEntity(Entity entity, World world)
    {
        if (world.Has<Transform>(entity))
        {
            ref readonly var transform = ref world.Get<Transform>(entity);
            Target = transform.Position;
            Distance = 5f; // Default focus distance
        }
    }
}

public enum CameraMode { Orbit, Fly, TopDown, SideView, FrontView }
```

### 5.3 Transform Gizmos

```csharp
public sealed class TransformGizmo
{
    public GizmoMode Mode { get; set; } = GizmoMode.Translate;
    public GizmoSpace Space { get; set; } = GizmoSpace.World;
    public GizmoPivot Pivot { get; set; } = GizmoPivot.Center;

    public float SnapTranslate { get; set; } = 0f; // 0 = no snap
    public float SnapRotate { get; set; } = 0f;    // degrees
    public float SnapScale { get; set; } = 0f;

    private GizmoAxis? hoveredAxis;
    private GizmoAxis? activeAxis;
    private Vector3 dragStart;

    public void Render(I3DRenderer renderer, IReadOnlyList<Entity> selection, World world)
    {
        if (selection.Count == 0) return;

        var pivot = CalculatePivot(selection, world);
        var rotation = Space == GizmoSpace.Local && selection.Count == 1
            ? world.Get<Transform>(selection[0]).Rotation
            : Quaternion.Identity;

        switch (Mode)
        {
            case GizmoMode.Translate:
                RenderTranslateGizmo(renderer, pivot, rotation);
                break;
            case GizmoMode.Rotate:
                RenderRotateGizmo(renderer, pivot, rotation);
                break;
            case GizmoMode.Scale:
                RenderScaleGizmo(renderer, pivot, rotation);
                break;
        }
    }

    private void RenderTranslateGizmo(I3DRenderer renderer, Vector3 position, Quaternion rotation)
    {
        // X axis - red arrow
        var xColor = hoveredAxis == GizmoAxis.X ? Color.Yellow : Color.Red;
        renderer.DrawArrow(position, position + rotation * Vector3.UnitX, xColor);

        // Y axis - green arrow
        var yColor = hoveredAxis == GizmoAxis.Y ? Color.Yellow : Color.Green;
        renderer.DrawArrow(position, position + rotation * Vector3.UnitY, yColor);

        // Z axis - blue arrow
        var zColor = hoveredAxis == GizmoAxis.Z ? Color.Yellow : Color.Blue;
        renderer.DrawArrow(position, position + rotation * Vector3.UnitZ, zColor);

        // XY plane
        // XZ plane
        // YZ plane
    }
}

public enum GizmoMode { Translate, Rotate, Scale }
public enum GizmoSpace { World, Local }
public enum GizmoPivot { Center, Pivot }
public enum GizmoAxis { X, Y, Z, XY, XZ, YZ, XYZ }
```

### 5.4 Selection System

```csharp
public sealed class SelectionManager
{
    private readonly List<Entity> selected = [];
    private Entity primary;

    public event Action<IReadOnlyList<Entity>>? SelectionChanged;

    public IReadOnlyList<Entity> Selected => selected;
    public Entity Primary => primary;

    public bool IsSelected(Entity entity) => selected.Contains(entity);

    public void Select(Entity entity, bool additive = false)
    {
        if (!additive)
            selected.Clear();

        if (!selected.Contains(entity))
            selected.Add(entity);

        primary = entity;
        SelectionChanged?.Invoke(selected);
    }

    public void SelectMultiple(IEnumerable<Entity> entities, bool additive = false)
    {
        if (!additive)
            selected.Clear();

        foreach (var entity in entities)
        {
            if (!selected.Contains(entity))
                selected.Add(entity);
        }

        if (selected.Count > 0 && !selected.Contains(primary))
            primary = selected[0];

        SelectionChanged?.Invoke(selected);
    }

    public void Deselect(Entity entity)
    {
        selected.Remove(entity);
        if (primary == entity && selected.Count > 0)
            primary = selected[0];
        SelectionChanged?.Invoke(selected);
    }

    public void Clear()
    {
        selected.Clear();
        primary = Entity.Null;
        SelectionChanged?.Invoke(selected);
    }

    public void SelectAll(World world)
    {
        selected.Clear();
        selected.AddRange(world.GetAllEntities());
        if (selected.Count > 0)
            primary = selected[0];
        SelectionChanged?.Invoke(selected);
    }
}
```

---

## 6. Asset Management

### 6.1 Asset Database

```csharp
public sealed class AssetDatabase
{
    private readonly string projectRoot;
    private readonly Dictionary<string, AssetEntry> assets = [];
    private readonly FileSystemWatcher watcher;

    public event Action<string>? AssetCreated;
    public event Action<string>? AssetModified;
    public event Action<string>? AssetDeleted;

    public void Refresh()
    {
        // Scan project for all assets matching ItemGroups
        ScanDirectory(Path.Combine(projectRoot, "Assets"));
        ScanDirectory(Path.Combine(projectRoot, "Scenes"));
        ScanDirectory(Path.Combine(projectRoot, "Prefabs"));
    }

    public AssetEntry? GetAsset(string relativePath)
        => assets.GetValueOrDefault(relativePath);

    public IEnumerable<AssetEntry> GetAssetsOfType(AssetType type)
        => assets.Values.Where(a => a.Type == type);

    public IEnumerable<AssetEntry> Search(string query)
        => assets.Values.Where(a =>
            a.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
}

public record AssetEntry(
    string Path,
    string Name,
    AssetType Type,
    long Size,
    DateTime Modified,
    byte[]? Thumbnail
);

public enum AssetType
{
    Scene,      // .kescene
    Prefab,     // .keprefab
    World,      // .keworld
    Texture,    // .png, .jpg, .dds
    Audio,      // .wav, .ogg, .mp3
    Model,      // .gltf, .glb, .fbx
    Shader,     // .kesl
    Script,     // .cs
    Data,       // .json
    Other
}
```

### 6.2 Scene File Format (.kescene)

```json
{
  "version": "1.0",
  "name": "Main",
  "settings": {
    "ambientColor": [0.1, 0.1, 0.15, 1.0],
    "gravity": [0, -9.81, 0]
  },
  "entities": [
    {
      "id": 1,
      "name": "Player",
      "parent": null,
      "tags": ["player", "controllable"],
      "components": [
        {
          "type": "Transform",
          "data": {
            "position": [0, 1, 0],
            "rotation": [0, 0, 0, 1],
            "scale": [1, 1, 1]
          }
        },
        {
          "type": "MyGame.Components.Health",
          "data": {
            "current": 100,
            "max": 100
          }
        }
      ]
    },
    {
      "id": 2,
      "name": "MainCamera",
      "parent": 1,
      "prefab": null,
      "components": [
        {
          "type": "Transform",
          "data": { "position": [0, 5, -10] }
        },
        {
          "type": "Camera",
          "data": { "fov": 60, "near": 0.1, "far": 1000 }
        }
      ]
    }
  ],
  "prefabInstances": [
    {
      "id": 100,
      "prefab": "Prefabs/Enemy.keprefab",
      "overrides": {
        "Transform.position": [10, 0, 5]
      }
    }
  ]
}
```

### 6.3 Prefab File Format (.keprefab)

```json
{
  "version": "1.0",
  "name": "Enemy",
  "basePrefab": null,
  "root": {
    "name": "Enemy",
    "components": [
      { "type": "Transform", "data": {} },
      { "type": "Health", "data": { "current": 50, "max": 50 } },
      { "type": "AI", "data": { "behavior": "Patrol" } }
    ],
    "children": [
      {
        "name": "Model",
        "components": [
          { "type": "MeshRenderer", "data": { "mesh": "Assets/Models/goblin.glb" } }
        ]
      },
      {
        "name": "HitBox",
        "components": [
          { "type": "BoxCollider", "data": { "size": [1, 2, 1] } }
        ]
      }
    ]
  }
}
```

---

## 7. Undo/Redo System

### 7.1 Command Pattern

```csharp
public interface IEditorCommand
{
    string Description { get; }
    void Execute();
    void Undo();
    bool CanMergeWith(IEditorCommand other);
    void MergeWith(IEditorCommand other);
}

public sealed class UndoRedoManager
{
    private readonly Stack<IEditorCommand> undoStack = [];
    private readonly Stack<IEditorCommand> redoStack = [];

    public event Action? HistoryChanged;

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public string? UndoDescription => undoStack.TryPeek(out var cmd) ? cmd.Description : null;
    public string? RedoDescription => redoStack.TryPeek(out var cmd) ? cmd.Description : null;

    public void Execute(IEditorCommand command)
    {
        // Try to merge with previous command (for continuous dragging, etc.)
        if (undoStack.TryPeek(out var previous) && previous.CanMergeWith(command))
        {
            previous.MergeWith(command);
        }
        else
        {
            command.Execute();
            undoStack.Push(command);
        }

        redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (undoStack.TryPop(out var command))
        {
            command.Undo();
            redoStack.Push(command);
            HistoryChanged?.Invoke();
        }
    }

    public void Redo()
    {
        if (redoStack.TryPop(out var command))
        {
            command.Execute();
            undoStack.Push(command);
            HistoryChanged?.Invoke();
        }
    }

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
        HistoryChanged?.Invoke();
    }
}
```

### 7.2 Command Implementations

```csharp
public sealed class SetComponentCommand<T> : IEditorCommand where T : struct
{
    private readonly World world;
    private readonly Entity entity;
    private readonly T oldValue;
    private T newValue;

    public string Description => $"Modify {typeof(T).Name}";

    public SetComponentCommand(World world, Entity entity, T oldValue, T newValue)
    {
        this.world = world;
        this.entity = entity;
        this.oldValue = oldValue;
        this.newValue = newValue;
    }

    public void Execute() => world.Set(entity, newValue);
    public void Undo() => world.Set(entity, oldValue);

    public bool CanMergeWith(IEditorCommand other)
        => other is SetComponentCommand<T> cmd && cmd.entity == entity;

    public void MergeWith(IEditorCommand other)
    {
        if (other is SetComponentCommand<T> cmd)
            newValue = cmd.newValue;
    }
}

public sealed class CreateEntityCommand : IEditorCommand
{
    private readonly World world;
    private readonly string? name;
    private Entity createdEntity;

    public string Description => $"Create Entity '{name ?? "Entity"}'";

    public void Execute()
    {
        createdEntity = world.Spawn(name).Build();
    }

    public void Undo()
    {
        world.Despawn(createdEntity);
    }

    public bool CanMergeWith(IEditorCommand other) => false;
    public void MergeWith(IEditorCommand other) { }
}

public sealed class DeleteEntitiesCommand : IEditorCommand
{
    private readonly World world;
    private readonly Entity[] entities;
    private readonly EntitySnapshot[] snapshots;

    public string Description => entities.Length == 1
        ? "Delete Entity"
        : $"Delete {entities.Length} Entities";

    public void Execute()
    {
        foreach (var entity in entities)
            world.Despawn(entity);
    }

    public void Undo()
    {
        foreach (var snapshot in snapshots)
            snapshot.RestoreTo(world);
    }

    public bool CanMergeWith(IEditorCommand other) => false;
    public void MergeWith(IEditorCommand other) { }
}

public sealed class ReparentEntityCommand : IEditorCommand
{
    private readonly World world;
    private readonly Entity entity;
    private readonly Entity oldParent;
    private readonly Entity newParent;

    public string Description => "Reparent Entity";

    public void Execute() => world.SetParent(entity, newParent);
    public void Undo() => world.SetParent(entity, oldParent);

    public bool CanMergeWith(IEditorCommand other) => false;
    public void MergeWith(IEditorCommand other) { }
}

public sealed class CompositeCommand : IEditorCommand
{
    private readonly IEditorCommand[] commands;

    public string Description { get; }

    public CompositeCommand(string description, params IEditorCommand[] commands)
    {
        Description = description;
        this.commands = commands;
    }

    public void Execute()
    {
        foreach (var cmd in commands)
            cmd.Execute();
    }

    public void Undo()
    {
        for (int i = commands.Length - 1; i >= 0; i--)
            commands[i].Undo();
    }

    public bool CanMergeWith(IEditorCommand other) => false;
    public void MergeWith(IEditorCommand other) { }
}
```

---

## 8. Play Mode

### 8.1 State Machine

```csharp
public enum EditorPlayState
{
    Editing,    // Normal edit mode
    Playing,    // Game running
    Paused      // Game paused, can inspect
}

public sealed class PlayModeManager
{
    private readonly EditorWorldManager worldManager;
    private readonly UndoRedoManager undoRedo;

    public EditorPlayState State { get; private set; } = EditorPlayState.Editing;

    public event Action<EditorPlayState>? StateChanged;

    public float TimeScale { get; set; } = 1f;

    public void Play()
    {
        if (State == EditorPlayState.Editing)
        {
            // Save state for restoration
            worldManager.EnterPlayMode();

            // Disable undo during play
            undoRedo.Clear();

            State = EditorPlayState.Playing;
            StateChanged?.Invoke(State);
        }
        else if (State == EditorPlayState.Paused)
        {
            State = EditorPlayState.Playing;
            StateChanged?.Invoke(State);
        }
    }

    public void Pause()
    {
        if (State == EditorPlayState.Playing)
        {
            State = EditorPlayState.Paused;
            StateChanged?.Invoke(State);
        }
    }

    public void Stop()
    {
        if (State != EditorPlayState.Editing)
        {
            // Restore pre-play state
            worldManager.ExitPlayMode();

            State = EditorPlayState.Editing;
            StateChanged?.Invoke(State);
        }
    }

    public void Step()
    {
        if (State == EditorPlayState.Paused)
        {
            // Run single frame
            worldManager.SceneWorld?.Update(1f / 60f);
        }
    }

    public void Update(float deltaTime)
    {
        if (State == EditorPlayState.Playing)
        {
            worldManager.SceneWorld?.Update(deltaTime * TimeScale);
        }
    }
}
```

---

## 9. Hot Reload

### 9.1 AssemblyLoadContext Approach

```csharp
public sealed class HotReloadManager : IDisposable
{
    private readonly EditorProject project;
    private readonly World sceneWorld;
    private AssemblyLoadContext? gameContext;
    private readonly List<ISystem> registeredSystems = [];

    private FileSystemWatcher? sourceWatcher;

    public event Action? ReloadStarted;
    public event Action? ReloadCompleted;
    public event Action<string[]>? CompilationFailed;

    public void StartWatching()
    {
        sourceWatcher = new FileSystemWatcher(project.RootDirectory, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
        };

        sourceWatcher.Changed += OnSourceChanged;
        sourceWatcher.EnableRaisingEvents = true;
    }

    private async void OnSourceChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce multiple changes
        await Task.Delay(500);
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        ReloadStarted?.Invoke();

        try
        {
            // 1. Capture current state
            var snapshot = sceneWorld.CreateSnapshot();

            // 2. Unregister all game systems
            foreach (var system in registeredSystems)
            {
                sceneWorld.RemoveSystem(system);
            }
            registeredSystems.Clear();

            // 3. Unload previous assembly
            if (gameContext != null)
            {
                gameContext.Unload();
                gameContext = null;

                // Force GC
                for (int i = 0; i < 5; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // 4. Rebuild project
            var result = await BuildProjectAsync();
            if (!result.Success)
            {
                CompilationFailed?.Invoke(result.Errors);
                return;
            }

            // 5. Load new assembly
            gameContext = new AssemblyLoadContext("GameCode", isCollectible: true);
            var assembly = gameContext.LoadFromAssemblyPath(result.OutputPath);

            // 6. Re-register systems
            RegisterSystemsFromAssembly(assembly);

            // 7. Component data survives (stored as bytes in archetypes)
            // Systems now operate on existing entities

            ReloadCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            CompilationFailed?.Invoke([ex.Message]);
        }
    }

    private async Task<BuildResult> BuildProjectAsync()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{project.ProjectPath}\" -c Debug --no-restore",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            var outputPath = Path.Combine(project.OutputDirectory,
                Path.GetFileNameWithoutExtension(project.ProjectPath) + ".dll");
            return new BuildResult(true, outputPath, []);
        }

        return new BuildResult(false, "", ParseErrors(error));
    }

    private void RegisterSystemsFromAssembly(Assembly assembly)
    {
        var systemTypes = assembly.GetTypes()
            .Where(t => typeof(ISystem).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var systemType in systemTypes)
        {
            var system = (ISystem)Activator.CreateInstance(systemType)!;
            sceneWorld.AddSystem(system);
            registeredSystems.Add(system);
        }
    }

    public void Dispose()
    {
        sourceWatcher?.Dispose();
        gameContext?.Unload();
    }
}

public record BuildResult(bool Success, string OutputPath, string[] Errors);
```

---

## 10. Debugging & Profiling

### 10.1 Existing Infrastructure

KeenEyes already provides comprehensive debugging:

| Class | Location | Purpose |
|-------|----------|---------|
| `EntityInspector` | KeenEyes.Debugging | Runtime entity introspection |
| `Profiler` | KeenEyes.Debugging | System timing with history |
| `MemoryTracker` | KeenEyes.Debugging | Memory usage tracking |
| `GCTracker` | KeenEyes.Debugging | Per-system GC allocations |
| `ParallelProfiler` | KeenEyes.Parallelism | DOT graphs, bottlenecks |

### 10.2 Console Panel

```csharp
public sealed class ConsolePanel
{
    private readonly List<LogEntry> entries = [];
    private readonly RingBuffer<LogEntry> buffer = new(1000);

    private bool showInfo = true;
    private bool showWarning = true;
    private bool showError = true;

    private string filterText = "";

    public void AddEntry(LogLevel level, string message, string? source, string? stackTrace)
    {
        var entry = new LogEntry(DateTime.Now, level, message, source, stackTrace);
        buffer.Add(entry);

        // Collapse duplicates
        if (entries.Count > 0 && entries[^1].Message == message)
        {
            entries[^1] = entries[^1] with { Count = entries[^1].Count + 1 };
        }
        else
        {
            entries.Add(entry);
        }
    }

    public IEnumerable<LogEntry> GetVisibleEntries()
    {
        return entries
            .Where(e => (e.Level == LogLevel.Info && showInfo) ||
                       (e.Level == LogLevel.Warning && showWarning) ||
                       (e.Level == LogLevel.Error && showError))
            .Where(e => string.IsNullOrEmpty(filterText) ||
                       e.Message.Contains(filterText, StringComparison.OrdinalIgnoreCase));
    }

    public void Clear() => entries.Clear();
}

public record LogEntry(
    DateTime Time,
    LogLevel Level,
    string Message,
    string? Source,
    string? StackTrace,
    int Count = 1
);
```

### 10.3 Profiler Panel

```
┌─ System Profiler ────────────────────────────────────┐
│  Frame: 16.67ms (60 FPS)  |  GC: 0.0 KB             │
├──────────────────────────────────────────────────────┤
│  System              Avg     Max     Alloc    %      │
│  ───────────────────────────────────────────────────│
│  PhysicsSystem      4.2ms   6.1ms    0 KB   25.2%   │
│  RenderSystem       3.8ms   5.2ms   12 KB   22.8%   │
│  AISystem           2.1ms   3.4ms    0 KB   12.6%   │
│  InputSystem        0.1ms   0.2ms    0 KB    0.6%   │
│  ───────────────────────────────────────────────────│
│  ████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░   │
│  [Physics][Render][AI][Other]                        │
└──────────────────────────────────────────────────────┘
```

---

## 11. Graph Node Editor

See [ADR-010](../adr/010-graph-node-editor.md) for complete architecture.

### 11.1 Package Structure

```
KeenEyes.Graph.Abstractions/
├── Components/
│   ├── GraphCanvas.cs
│   ├── GraphNode.cs
│   └── GraphConnection.cs
├── Ports/
│   ├── PortDefinition.cs
│   ├── PortTypeId.cs
│   └── PortDirection.cs
└── Interfaces/
    ├── INodeTypeDefinition.cs
    └── IGraphRenderer.cs

KeenEyes.Graph/
├── Systems/
│   ├── GraphInputSystem.cs
│   ├── GraphLayoutSystem.cs
│   └── GraphRenderSystem.cs
├── GraphContext.cs
└── Registries/
    ├── PortRegistry.cs
    └── NodeTypeRegistry.cs

KeenEyes.Graph.Kesl/
├── Nodes/
│   ├── MathNodes.cs
│   ├── VectorNodes.cs
│   └── ComponentNodes.cs
├── KeslGraphCompiler.cs
└── KeslGraphValidator.cs
```

### 11.2 KESL Integration Flow

```
Visual Graph (editor)
    ↓ KeslGraphCompiler.ToAst()
KESL AST
    ↓ existing pipeline
┌──────────────────────────────────────┐
│  GlslGenerator  │  CSharpGenerator   │
│  (GPU shader)   │  (CPU binding)     │
└──────────────────────────────────────┘
```

---

## 12. Build Pipeline

### 12.1 Build Window

```csharp
public sealed class BuildManager
{
    private readonly EditorProject project;

    public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;
    public BuildTarget Target { get; set; } = BuildTarget.Windows;

    public async Task<BuildResult> BuildAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Cleaning output directory...");
        CleanOutput();

        progress?.Report("Compiling scripts...");
        var compileResult = await CompileAsync();
        if (!compileResult.Success) return compileResult;

        progress?.Report("Processing assets...");
        await ProcessAssetsAsync();

        progress?.Report("Packaging...");
        await PackageAsync();

        progress?.Report("Build complete!");
        return new BuildResult(true, OutputPath, []);
    }

    private async Task ProcessAssetsAsync()
    {
        // Copy/process all KeenEyesAsset items
        foreach (var asset in project.Assets)
        {
            var destPath = Path.Combine(OutputPath, "Assets", asset);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            // Apply asset-specific processing
            await ProcessAssetAsync(asset, destPath);
        }
    }
}

public enum BuildConfiguration { Debug, Release }
public enum BuildTarget { Windows, Linux, MacOS, WebAssembly }
```

---

## 13. Source Generated Assets

A key architectural decision: **KE files are processed by source generators at compile time**, not parsed at runtime. This aligns with KeenEyes' "No Reflection in Production" principle and ensures Native AOT compatibility.

### 13.1 Why Source Generation?

| Benefit | Description |
|---------|-------------|
| **Native AOT** | No runtime parsing, no `System.Text.Json` reflection |
| **Compile-time validation** | Missing components, invalid entity references caught at build |
| **IntelliSense** | Generated types appear in IDE autocomplete |
| **Performance** | No file I/O or deserialization at runtime |
| **Type safety** | Component types validated against actual C# definitions |
| **Refactoring** | Rename a component → build errors show all affected scenes |
| **Dead code elimination** | Unused scenes/prefabs can be detected |

### 13.2 Generator Pipeline

```
MyGame/
├── Scenes/
│   ├── Main.kescene
│   └── Menu.kescene
├── Prefabs/
│   ├── Player.keprefab
│   └── Enemy.keprefab
└── Worlds/
    └── Default.keworld

        ↓ KeenEyes.Generators.Assets (source generator)

obj/Generated/
├── Scenes.g.cs          → SceneRegistry, typed loader methods
├── Prefabs.g.cs         → PrefabRegistry, typed spawn methods
└── WorldConfigs.g.cs    → World configuration builders
```

### 13.3 Scene Generation (.kescene → C#)

**Input: Scenes/Main.kescene**
```json
{
  "name": "Main",
  "entities": [
    {
      "name": "Player",
      "components": [
        { "type": "Transform", "data": { "position": [0, 1, 0] } },
        { "type": "MyGame.Health", "data": { "current": 100, "max": 100 } }
      ]
    }
  ]
}
```

**Output: Scenes.g.cs**
```csharp
// <auto-generated />
namespace MyGame;

public static partial class Scenes
{
    /// <summary>All available scenes in this project.</summary>
    public static IReadOnlyList<string> All { get; } = ["Main", "Menu"];

    /// <summary>Load the Main scene into the specified world.</summary>
    public static void LoadMain(World world)
    {
        // Generated entity creation - no parsing!
        world.Spawn("Player")
            .With(new Transform { Position = new Vector3(0, 1, 0) })
            .With(new Health { Current = 100, Max = 100 })
            .Build();
    }

    /// <summary>Load the Menu scene into the specified world.</summary>
    public static void LoadMenu(World world)
    {
        // ...
    }

    /// <summary>Load a scene by name.</summary>
    public static void Load(World world, string sceneName)
    {
        switch (sceneName)
        {
            case "Main": LoadMain(world); break;
            case "Menu": LoadMenu(world); break;
            default: throw new ArgumentException($"Unknown scene: {sceneName}");
        }
    }
}
```

**Usage:**
```csharp
// Type-safe, IntelliSense-enabled scene loading
Scenes.LoadMain(world);

// Or by name if needed
Scenes.Load(world, "Main");

// Enumerate available scenes
foreach (var scene in Scenes.All)
    Console.WriteLine(scene);
```

### 13.4 Prefab Generation (.keprefab → C#)

**Input: Prefabs/Player.keprefab**
```json
{
  "name": "Player",
  "root": {
    "components": [
      { "type": "Transform", "data": {} },
      { "type": "Health", "data": { "current": 100, "max": 100 } },
      { "type": "PlayerController", "data": { "speed": 10 } }
    ],
    "children": [
      {
        "name": "Camera",
        "components": [
          { "type": "Transform", "data": { "position": [0, 2, -5] } },
          { "type": "Camera", "data": { "fov": 60 } }
        ]
      }
    ]
  }
}
```

**Output: Prefabs.g.cs**
```csharp
// <auto-generated />
namespace MyGame;

public static partial class Prefabs
{
    /// <summary>All available prefabs in this project.</summary>
    public static IReadOnlyList<string> All { get; } = ["Player", "Enemy"];

    /// <summary>Spawn a Player prefab instance.</summary>
    public static Entity SpawnPlayer(World world, Vector3? position = null)
    {
        var root = world.Spawn("Player")
            .With(new Transform { Position = position ?? Vector3.Zero })
            .With(new Health { Current = 100, Max = 100 })
            .With(new PlayerController { Speed = 10f })
            .Build();

        var camera = world.Spawn("Camera")
            .With(new Transform { Position = new Vector3(0, 2, -5) })
            .With(new Camera { Fov = 60f })
            .Build();

        world.SetParent(camera, root);

        return root;
    }

    /// <summary>Spawn a Player with custom overrides.</summary>
    public static Entity SpawnPlayer(
        World world,
        Vector3? position = null,
        Health? health = null,
        PlayerController? controller = null)
    {
        var root = world.Spawn("Player")
            .With(new Transform { Position = position ?? Vector3.Zero })
            .With(health ?? new Health { Current = 100, Max = 100 })
            .With(controller ?? new PlayerController { Speed = 10f })
            .Build();

        // ... children

        return root;
    }

    /// <summary>Spawn an Enemy prefab instance.</summary>
    public static Entity SpawnEnemy(World world, Vector3? position = null)
    {
        // ...
    }
}
```

**Usage:**
```csharp
// Type-safe spawning with IntelliSense
var player = Prefabs.SpawnPlayer(world, position: new Vector3(10, 0, 0));

// With component overrides
var boss = Prefabs.SpawnEnemy(world,
    position: bossSpawnPoint,
    health: new Health { Current = 500, Max = 500 });
```

### 13.5 World Configuration Generation (.keworld → C#)

**Input: Worlds/Default.keworld**
```json
{
  "name": "Default",
  "settings": {
    "fixedTimeStep": 0.02,
    "gravity": [0, -9.81, 0],
    "maxEntities": 10000
  },
  "plugins": ["Physics", "Audio", "Graphics"],
  "systems": {
    "update": ["Movement", "AI", "Animation"],
    "fixedUpdate": ["Physics"],
    "render": ["Rendering", "UI"]
  }
}
```

**Output: WorldConfigs.g.cs**
```csharp
// <auto-generated />
namespace MyGame;

public static partial class WorldConfigs
{
    /// <summary>Configure a world with Default settings.</summary>
    public static WorldBuilder ConfigureDefault(this WorldBuilder builder)
    {
        return builder
            .WithFixedTimeStep(0.02f)
            .WithSingleton(new Gravity { Value = new Vector3(0, -9.81f, 0) })
            .WithPlugin<PhysicsPlugin>()
            .WithPlugin<AudioPlugin>()
            .WithPlugin<GraphicsPlugin>()
            .WithSystem<MovementSystem>(SystemPhase.Update)
            .WithSystem<AISystem>(SystemPhase.Update)
            .WithSystem<AnimationSystem>(SystemPhase.Update)
            .WithSystem<PhysicsSystem>(SystemPhase.FixedUpdate)
            .WithSystem<RenderingSystem>(SystemPhase.Render)
            .WithSystem<UISystem>(SystemPhase.Render);
    }

    /// <summary>Create a world with Default configuration.</summary>
    public static World CreateDefault()
    {
        return new WorldBuilder()
            .ConfigureDefault()
            .Build();
    }
}
```

**Usage:**
```csharp
// Create fully configured world
var world = WorldConfigs.CreateDefault();

// Or extend with custom additions
var world = new WorldBuilder()
    .ConfigureDefault()
    .WithPlugin<DebugPlugin>()  // Add debug overlay
    .Build();
```

### 13.6 Compile-Time Validation

The source generator validates KE files at compile time:

```
Scenes/Main.kescene(3,15): error KE001: Component type 'Helth' not found. Did you mean 'Health'?
Prefabs/Enemy.keprefab(7,8): error KE002: Entity reference 'Player' not found in prefab.
Prefabs/Player.keprefab(12,3): warning KE003: Component 'Transform' has no data, using defaults.
Worlds/Default.keworld(5,1): error KE004: Plugin 'Physic' not found. Did you mean 'Physics'?
```

### 13.7 Editor Integration

The editor writes KE files in JSON format (human-readable, VCS-friendly), and the source generator converts them to code:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Editor (Design Time)                      │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │ Scene View  │ →  │ .kescene    │ →  │ Source Gen  │ → .g.cs  │
│  │ (visual)    │    │ (JSON)      │    │ (compile)   │          │
│  └─────────────┘    └─────────────┘    └─────────────┘          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        Game (Runtime)                            │
│  ┌─────────────┐    ┌─────────────┐                             │
│  │ Scenes.g.cs │ →  │ World       │  ← No file parsing!         │
│  │ (compiled)  │    │ (entities)  │  ← No reflection!           │
│  └─────────────┘    └─────────────┘  ← Native AOT safe!         │
└─────────────────────────────────────────────────────────────────┘
```

### 13.8 Hybrid Approach for Editor

During editing, the editor needs to:
1. **Read** KE files as JSON for display/modification
2. **Write** KE files when user saves
3. **Hot-preview** without recompilation (optional)

For hot-preview, the editor can use a runtime interpreter:

```csharp
public static class SceneInterpreter
{
    /// <summary>
    /// Load scene from JSON at runtime (editor only, not AOT-safe).
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection for component instantiation")]
    public static void LoadFromJson(World world, string json)
    {
        var scene = JsonSerializer.Deserialize<SceneDefinition>(json);
        foreach (var entityDef in scene.Entities)
        {
            var builder = world.Spawn(entityDef.Name);
            foreach (var comp in entityDef.Components)
            {
                // Reflection-based (editor only)
                AddComponentDynamic(builder, comp.Type, comp.Data);
            }
            builder.Build();
        }
    }
}
```

This gives:
- **Editor**: JSON parsing for immediate preview
- **Build**: Source generation for AOT-safe runtime

### 13.9 Generator Package Structure

```
editor/KeenEyes.Generators.Assets/
├── SceneGenerator.cs           # .kescene → Scenes.g.cs
├── PrefabGenerator.cs          # .keprefab → Prefabs.g.cs
├── WorldConfigGenerator.cs     # .keworld → WorldConfigs.g.cs
├── Parsing/
│   ├── SceneParser.cs          # JSON → SceneDefinition
│   ├── PrefabParser.cs         # JSON → PrefabDefinition
│   └── WorldConfigParser.cs    # JSON → WorldConfigDefinition
├── Validation/
│   ├── ComponentValidator.cs   # Verify component types exist
│   ├── ReferenceValidator.cs   # Verify entity references
│   └── DiagnosticDescriptors.cs
└── Emitters/
    ├── SceneEmitter.cs         # Generate C# for scenes
    ├── PrefabEmitter.cs        # Generate C# for prefabs
    └── WorldConfigEmitter.cs   # Generate C# for world configs
```

### 13.10 MSBuild Integration

The SDK automatically includes the generator for KE files:

```xml
<!-- In KeenEyes.Sdk/Sdk.props -->
<ItemGroup>
  <!-- Source generator for KE files -->
  <PackageReference Include="KeenEyes.Generators.Assets"
                    Version="$(KeenEyesSdkVersion)"
                    PrivateAssets="all"
                    IncludeAssets="analyzers" />

  <!-- Additional files for the generator to process -->
  <AdditionalFiles Include="@(KeenEyesScene)" Generator="KeenEyes" />
  <AdditionalFiles Include="@(KeenEyesPrefab)" Generator="KeenEyes" />
  <AdditionalFiles Include="@(KeenEyesWorld)" Generator="KeenEyes" />
</ItemGroup>
```

---

## 14. Editor Plugin System

### 14.1 Editor Plugin Interface

```csharp
public interface IEditorPlugin
{
    string Name { get; }
    string Version { get; }

    /// <summary>Called when editor starts.</summary>
    void Initialize(IEditorContext context);

    /// <summary>Called when editor shuts down.</summary>
    void Shutdown();
}

public interface IEditorContext
{
    EditorProject Project { get; }
    EditorWorldManager Worlds { get; }
    SelectionManager Selection { get; }
    UndoRedoManager UndoRedo { get; }
    AssetDatabase Assets { get; }

    // Extension points
    void RegisterPanel(string name, Func<Panel> factory);
    void RegisterTool(string name, ITool tool);
    void RegisterInspector(Type componentType, IComponentInspector inspector);
    void RegisterMenuItem(string path, Action action, string? shortcut = null);
    void RegisterContextMenuItem(string path, Func<Entity, bool> filter, Action<Entity> action);
}
```

### 14.2 Built-in Editor Plugins

| Plugin | Purpose |
|--------|---------|
| `EditorCorePlugin` | Selection, commands, core UI |
| `InspectorPlugin` | Property editing, component display |
| `HierarchyPlugin` | Scene tree, drag-and-drop |
| `ViewportPlugin` | 3D/2D scene rendering, gizmos |
| `ProjectPlugin` | Asset browser, file management |
| `ConsolePlugin` | Log display, filtering |
| `ProfilerPlugin` | System timing, memory stats |
| `BuildPlugin` | Build pipeline, packaging |

---

## 15. Implementation Phases

> **Status:** Phases 1-2 completed. See [Epic #600](https://github.com/orion-ecs/keen-eye/issues/600) for tracking.

### Phase 1: Asset Source Generators ✅

**Goal:** Compile-time asset processing for Native AOT

| Task | Effort | Status |
|------|--------|--------|
| SceneGenerator (.kescene → C#) | Medium | ✅ Done |
| PrefabGenerator (.keprefab → C#) | Medium | ✅ Done |
| WorldConfigGenerator (.keworld → C#) | Medium | ✅ Done |
| Compile-time validation | Medium | ✅ Done |

**Deliverable:** Assets compile to type-safe C# code

### Phase 2: Editor Infrastructure ✅

**Goal:** Core editor application with panels and systems

| Task | Effort | Status |
|------|--------|--------|
| EditorApplication shell | Medium | ✅ Done |
| EditorWorldManager | Low | ✅ Done |
| UndoRedoManager (command pattern) | Medium | ✅ Done |
| SelectionManager with multi-select | Low | ✅ Done |
| AssetDatabase for project files | Medium | ✅ Done |
| SceneSerializer for .kescene files | Medium | ✅ Done |
| HierarchyPanel | Low | ✅ Done |
| InspectorPanel | Medium | ✅ Done |

**Deliverable:** Can open project, view/edit entities, save scenes

### Phase 3: Editor Panels

**Goal:** Visual panels for scene editing

| Task | Effort | Priority | Issue |
|------|--------|----------|-------|
| ViewportPanel with 3D rendering | High | P0 | #588 |
| Transform gizmos | High | P0 | #588 |
| ProjectPanel for asset browser | Medium | P0 | #593 |
| ConsolePanel for logs | Low | P0 | #594 |
| ComponentIntrospector (reflection) | Medium | P0 | #592 |
| PropertyDrawers for custom fields | Medium | P1 | #595 |
| Editor camera controller | Medium | P1 | #588 |
| Click/box selection in viewport | Medium | P1 | #588 |
| Grid overlay | Low | P2 | - |
| Entity icons | Low | P2 | - |

**Deliverable:** Full visual editing experience

### Phase 4: Advanced Features

**Goal:** Power user features

| Task | Effort | Priority | Issue |
|------|--------|----------|-------|
| Play mode state machine | Medium | P0 | #589 |
| Snapshot/restore on play/stop | Low | P0 | #589 |
| Pause and step | Low | P0 | #589 |
| Hot reload via AssemblyLoadContext | High | P1 | #590 |
| Prefab system with overrides | Medium | P1 | #596 |
| Settings and preferences | Medium | P1 | #597 |
| Keyboard shortcuts system | Low | P1 | #598 |
| Layout persistence | Low | P1 | #599 |
| Time scale control | Low | P2 | #589 |
| Live editing during play | Medium | P2 | #589 |

**Deliverable:** Professional editor workflow

### Phase 5: Developer Experience

**Goal:** Debugging and profiling

| Task | Effort | Priority |
|------|--------|----------|
| Profiler panel | Medium | P0 |
| Debug draw integration | Medium | P1 |
| Build pipeline UI | Medium | P1 |

**Deliverable:** Console output, profiling, build management

### Phase 6: Graph Editor

**Goal:** Visual shader and scripting

| Task | Effort | Priority | Issue |
|------|--------|----------|-------|
| KeenEyes.Graph.Abstractions | Medium | P0 | #570 |
| KeenEyes.Graph core systems | High | P0 | #570 |
| Bezier connection rendering | Medium | P0 | #572 |
| Pan/zoom canvas | Medium | P0 | #570 |
| Port type system | Medium | P0 | #572 |
| Multi-select with box selection | Medium | P1 | #574 |
| Context menu for node creation | Low | P1 | #574 |
| INodeTypeDefinition interface | Medium | P1 | #575 |
| NodeTypeRegistry | Medium | P1 | #575 |
| KESL node library | Medium | P1 | #576 |
| KeslGraphCompiler (graph → AST) | High | P1 | #576 |
| Shader preview | Medium | P2 | #576 |

**Deliverable:** Create KESL shaders visually

### Cross-Cutting: Quality Assurance

| Task | Effort | Priority | Issue |
|------|--------|----------|-------|
| Comprehensive test suite | High | Ongoing | #591 |
| Performance benchmarks | Medium | P1 | - |
| Documentation | Medium | Ongoing | - |

---

## Appendix A: File Extension Summary

| Extension | ItemGroup | Purpose |
|-----------|-----------|---------|
| `.kescene` | `<KeenEyesScene>` | Scene definition (entities, hierarchy) |
| `.keprefab` | `<KeenEyesPrefab>` | Prefab template (reusable entity) |
| `.keworld` | `<KeenEyesWorld>` | World configuration (settings) |
| `.kesl` | `<KeenEyesShader>` | Shader source (KESL language) |

---

## Appendix B: Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+S` | Save scene |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+D` | Duplicate selection |
| `Delete` | Delete selection |
| `F` | Focus viewport on selection |
| `W` | Translate tool |
| `E` | Rotate tool |
| `R` | Scale tool |
| `Ctrl+P` | Play/Stop |
| `Ctrl+Shift+P` | Pause |

---

## Appendix C: Comparison with Unity/Godot

| Feature | Unity | Godot | KeenEyes (Planned) |
|---------|-------|-------|-------------------|
| Project format | `.unity` folder | `project.godot` | `.csproj` / `.slnx` |
| Scene format | `.unity` YAML | `.tscn` text | `.kescene` JSON |
| Prefab format | `.prefab` YAML | `.tres` / `.scn` | `.keprefab` JSON |
| Scripting | C# (Mono/CoreCLR) | GDScript/C# | C# (.NET 10) |
| Visual scripting | Bolt (deprecated) | VisualScript | Graph Node Editor |
| Shader graphs | Shader Graph | Visual Shaders | KESL Graphs |
| Hot reload | Limited | Full | AssemblyLoadContext |
| Native AOT | No | No | Yes |
| ECS native | No (DOTS separate) | No | Yes (core design) |

---

## References

- [ADR-006: Custom MSBuild SDK](../adr/006-custom-msbuild-sdk.md)
- [ADR-008: Asset Management Architecture](../adr/008-asset-management-architecture.md)
- [ADR-010: Graph Node Editor Architecture](../adr/010-graph-node-editor.md)
- [Framework Editor Feasibility](./framework-editor.md)
- [UI System Documentation](../ui.md)
- [ROADMAP.md](../../ROADMAP.md) - Long-Term Vision: Browser-Based Editor
