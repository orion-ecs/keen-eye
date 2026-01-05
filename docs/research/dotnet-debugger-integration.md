# .NET Debugger Integration Research

## Overview

This document researches how to integrate .NET debugging capabilities into the KeenEyes editor, focusing on [SharpDbg](https://github.com/MattParkerDev/sharpdbg) - a fully managed .NET debugger implementing the Debug Adapter Protocol (DAP).

## SharpDbg Architecture

SharpDbg is an open-source, cross-platform .NET debugger written entirely in C#. It implements the VS Code Debug Adapter Protocol, making it compatible with any editor that supports DAP.

### Three-Layer Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    SharpDbg.Cli                         │
│  Entry point, CLI argument parsing, DAP client init    │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                 SharpDbg.Application                    │
│  DAP protocol implementation, message handling,         │
│  breakpoint events, execution state management          │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                SharpDbg.Infrastructure                  │
│  ManagedDebugger (core engine), ClrDebug wrapper,      │
│  expression evaluator (compiler + interpreter)          │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                      ClrDebug                           │
│  Managed wrappers around ICorDebug, IMetaData,         │
│  ICorProfiler, ISym APIs                                │
└─────────────────────────────────────────────────────────┘
```

### Key Dependencies

| Package | Purpose |
|---------|---------|
| [ClrDebug](https://github.com/lordmilko/ClrDebug) | Managed wrappers around .NET debugging APIs (ICorDebug, IMetaData, etc.) |
| .NET 10 SDK | Build requirement |

### Feature Comparison with netcoredbg

| Feature | SharpDbg | netcoredbg |
|---------|----------|------------|
| Expression Evaluation | ✅ | ✅ |
| DebuggerDisplay Attribute | ✅ | ❌ |
| DebuggerTypeProxy Attribute | ✅ | ❌ |
| DebuggerBrowsable | ✅ | ✅ |
| Async Method Stepping | 🚧 WIP | ✅ |
| Source Link | 🚧 WIP | ✅ |
| Auto Decompilation | 🚧 WIP | ❌ |
| Pure C# Implementation | ✅ | ❌ (C++) |

## Debug Adapter Protocol (DAP)

DAP standardizes communication between development tools and debuggers through a JSON-based protocol.

### Communication Flow

```
┌─────────────┐         DAP Messages          ┌─────────────────┐
│   Editor    │ ◄──────────────────────────► │  Debug Adapter  │
│  (Client)   │      stdin/stdout or TCP      │    (Server)     │
└─────────────┘                               └─────────────────┘
                                                      │
                                                      ▼
                                              ┌─────────────────┐
                                              │   Debugger/     │
                                              │   Runtime       │
                                              └─────────────────┘
```

### Message Types

1. **Requests**: Client-initiated commands expecting responses
2. **Responses**: Replies to requests with success/error status
3. **Events**: Adapter-initiated notifications (stopped, output, etc.)

### Core DAP Requests

| Category | Requests |
|----------|----------|
| Lifecycle | `initialize`, `launch`, `attach`, `disconnect`, `terminate` |
| Execution | `continue`, `next`, `stepIn`, `stepOut`, `pause`, `restart` |
| Breakpoints | `setBreakpoints`, `setFunctionBreakpoints`, `setExceptionBreakpoints` |
| Inspection | `threads`, `stackTrace`, `scopes`, `variables`, `evaluate` |
| Configuration | `configurationDone`, `setVariable`, `setExpression` |

### Launch Sequence

```
Client                                  Adapter
   │                                       │
   │──── initialize ─────────────────────►│
   │◄─── initialize response ─────────────│
   │                                       │
   │──── launch/attach ──────────────────►│
   │◄─── launch/attach response ──────────│
   │                                       │
   │──── setBreakpoints ─────────────────►│
   │◄─── setBreakpoints response ─────────│
   │                                       │
   │──── configurationDone ──────────────►│
   │◄─── configurationDone response ──────│
   │                                       │
   │◄──── initialized event ──────────────│
   │◄──── stopped event (entry point) ────│
   │                                       │
```

## Integration Approaches

### Approach 1: External Process (Recommended)

Launch SharpDbg as an external process and communicate via stdin/stdout.

**Advantages:**
- Clean separation between editor and debugger
- Debugger crashes don't affect editor
- Matches how VS Code and other editors work
- Easy to swap debug adapters (sharpdbg, netcoredbg, etc.)

**Implementation:**

```csharp
public class DebugAdapterClient : IDisposable
{
    private Process debuggerProcess;
    private DebugProtocolHost protocolHost;

    public async Task AttachAsync(int processId, string debuggerPath)
    {
        // Launch debug adapter process
        debuggerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = debuggerPath,
                Arguments = "--interpreter=vscode",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        debuggerProcess.Start();

        // Create DAP protocol host over stdin/stdout
        protocolHost = new DebugProtocolHost(
            debuggerProcess.StandardInput.BaseStream,
            debuggerProcess.StandardOutput.BaseStream);

        // Register event handlers
        protocolHost.EventReceived += OnEventReceived;
        protocolHost.Run();

        // Initialize and attach
        await protocolHost.SendRequestAsync(new InitializeRequest { ... });
        await protocolHost.SendRequestAsync(new AttachRequest { ProcessId = processId });
    }
}
```

### Approach 2: In-Process / Embedded (Recommended for KeenEyes)

Embed SharpDbg.Infrastructure directly for custom ECS debugging views.

**Advantages:**
- Direct access to `ICorDebugValue` for custom type visualization
- ECS-specific views: entity inspector, component values, world state
- Lower latency for high-frequency inspection
- Full control over debugging UX

**Disadvantages:**
- Tighter coupling to SharpDbg internals
- Debugger issues could affect editor stability
- More complex implementation

**Why Embedded for KeenEyes:**
- Custom visualization of `World`, `Entity`, component structs
- Real-time entity/component inspection during pause
- Integration with editor's entity hierarchy view
- Query result visualization during debugging

#### SharpDbg.Infrastructure Components

The Infrastructure layer provides everything needed for embedding:

```
SharpDbg.Infrastructure/
├── Debugger/
│   ├── ManagedDebugger.cs              # Core debugger engine
│   ├── ManagedDebugger_VariableInfo.cs # Variable metadata
│   ├── ManagedDebugger_VariableValues.cs # Value retrieval
│   ├── BreakpointManager.cs            # Breakpoint handling
│   ├── VariableManager.cs              # Variable tracking
│   ├── SymbolReader.cs                 # PDB symbol loading
│   ├── ModuleInfo.cs                   # Assembly metadata
│   └── ExpressionEvaluator/            # Expression evaluation
├── ClrDebugExtensions.cs               # Helper extensions
└── DbgShimResolver.cs                  # Runtime shim resolution
```

#### Embedded Integration Pattern

```csharp
using ClrDebug;
using SharpDbg.Infrastructure.Debugger;

public class EmbeddedDebugger : IDisposable
{
    private ManagedDebugger debugger;
    private BreakpointManager breakpoints;
    private VariableManager variables;

    public async Task AttachAsync(int processId)
    {
        // Initialize ClrDebug wrapper
        var corDebug = new CorDebug();
        var callback = new DebuggerCallback(this);
        corDebug.SetManagedHandler(callback);

        // Attach to running process
        var process = corDebug.DebugActiveProcess(processId, win32Attach: false);

        // Initialize SharpDbg managers
        debugger = new ManagedDebugger(process);
        breakpoints = new BreakpointManager(debugger);
        variables = new VariableManager(debugger);
    }

    // Custom ECS inspection - direct access to ICorDebugValue
    public WorldSnapshot InspectWorld(CorDebugValue worldValue)
    {
        // Read World fields directly via ICorDebugObjectValue
        var objectValue = worldValue.As<CorDebugObjectValue>();

        // Get entity count
        var entityCountField = objectValue.GetFieldValue("entityCount");
        int entityCount = entityCountField.As<CorDebugGenericValue>().GetValue<int>();

        // Enumerate entities via archetype storage
        var archetypeManager = objectValue.GetFieldValue("archetypeManager");
        // ... custom traversal of ECS data structures

        return new WorldSnapshot { EntityCount = entityCount, /* ... */ };
    }
}
```

#### ICorDebugValue Hierarchy for Type Inspection

Understanding the value hierarchy is key for custom visualizers:

```
ICorDebugValue (base)
├── ICorDebugGenericValue     # Primitives (int, float, bool)
├── ICorDebugReferenceValue   # Object references
├── ICorDebugObjectValue      # Object instances (fields, properties)
├── ICorDebugBoxValue         # Boxed value types
├── ICorDebugStringValue      # String values
├── ICorDebugArrayValue       # Arrays
└── ICorDebugHeapValue        # Heap-allocated objects
```

ClrDebug wraps these as `CorDebugValue`, `CorDebugObjectValue`, etc. with proper inheritance.

#### ECS-Specific Visualizers

```csharp
public class EntityVisualizer
{
    public EntityView Visualize(CorDebugObjectValue entityValue, CorDebugObjectValue worldValue)
    {
        // Read Entity struct fields
        var id = entityValue.GetFieldValue("Id").As<CorDebugGenericValue>().GetValue<int>();
        var version = entityValue.GetFieldValue("Version").As<CorDebugGenericValue>().GetValue<int>();

        // Look up components from World's archetype storage
        var components = GetEntityComponents(worldValue, id);

        return new EntityView
        {
            Id = id,
            Version = version,
            Components = components,
            Children = GetEntityChildren(worldValue, id),
            Parent = GetEntityParent(worldValue, id)
        };
    }

    private List<ComponentView> GetEntityComponents(CorDebugObjectValue world, int entityId)
    {
        // Navigate: World -> ArchetypeManager -> find archetype for entity -> read components
        var archetypeManager = world.GetFieldValue("archetypeManager").As<CorDebugObjectValue>();
        // ... traverse archetype storage to find and read component data
    }
}

public class ComponentVisualizer
{
    public ComponentView Visualize(CorDebugObjectValue componentValue, Type componentType)
    {
        var view = new ComponentView { TypeName = componentType.Name };

        // Read all fields of the component struct
        foreach (var field in componentType.GetFields())
        {
            var fieldValue = componentValue.GetFieldValue(field.Name);
            view.Fields.Add(new FieldView
            {
                Name = field.Name,
                Value = FormatValue(fieldValue),
                CanEdit = IsPrimitiveOrSimple(field.FieldType)
            });
        }

        return view;
    }
}
```

#### Hooking into Gameplay Functionality

To integrate with the running game, we need to hook into key ECS lifecycle points:

**1. System Update Interception**

Set breakpoints at system entry/exit to track execution flow:

```csharp
public class SystemExecutionHook
{
    private readonly BreakpointManager breakpoints;
    private readonly Dictionary<string, SystemExecutionInfo> systemStats = new();

    public void HookSystemUpdates(CorDebugModule coreModule)
    {
        // Find SystemManager.ExecuteSystem method via metadata
        var metadata = coreModule.GetMetaDataInterface<MetaDataImport>();
        var systemManagerType = metadata.FindTypeDefByName("KeenEyes.Core.Systems.SystemManager");
        var executeMethod = metadata.EnumMethods(systemManagerType)
            .First(m => metadata.GetMethodProps(m).szMethod == "ExecuteSystem");

        // Set breakpoint at method entry
        var bp = breakpoints.SetMethodBreakpoint(coreModule, executeMethod);
        bp.OnHit += (sender, args) =>
        {
            // Read the ISystem parameter to get system type
            var systemArg = args.Frame.GetArgument(0);
            var systemType = GetTypeName(systemArg);

            // Track timing
            systemStats[systemType] = new SystemExecutionInfo
            {
                StartTime = Stopwatch.GetTimestamp(),
                ThreadId = args.Thread.Id
            };

            // Continue execution (don't pause)
            args.Continue = true;
        };
    }
}
```

**2. Entity Lifecycle Hooks**

Trap entity spawn/despawn to update the entity browser in real-time:

```csharp
public class EntityLifecycleHook
{
    public event Action<int, EntityOperation> OnEntityChanged;

    public void HookEntityLifecycle(CorDebugModule coreModule)
    {
        // Hook World.Spawn() completion
        SetMethodExitBreakpoint("KeenEyes.Core.World", "Spawn", args =>
        {
            var entity = args.ReturnValue.As<CorDebugObjectValue>();
            var id = entity.GetFieldValue("Id").As<CorDebugGenericValue>().GetValue<int>();
            OnEntityChanged?.Invoke(id, EntityOperation.Spawned);
        });

        // Hook World.Despawn()
        SetMethodEntryBreakpoint("KeenEyes.Core.World", "Despawn", args =>
        {
            var entityArg = args.Frame.GetArgument(1); // 'this' is arg0
            var id = entityArg.As<CorDebugObjectValue>()
                .GetFieldValue("Id").As<CorDebugGenericValue>().GetValue<int>();
            OnEntityChanged?.Invoke(id, EntityOperation.Despawning);
        });
    }
}
```

**3. Component Change Tracking**

Use data breakpoints (hardware watchpoints) for component field changes:

```csharp
public class ComponentWatchpoint
{
    public void WatchComponent<T>(int entityId, string fieldName) where T : struct
    {
        // Find component storage address for this entity
        var componentAddress = FindComponentAddress(entityId, typeof(T));
        var fieldOffset = GetFieldOffset(typeof(T), fieldName);

        // Set hardware data breakpoint (x86/x64 debug registers)
        var watchpoint = breakpoints.SetDataBreakpoint(
            componentAddress + fieldOffset,
            size: Marshal.SizeOf(typeof(T).GetField(fieldName).FieldType),
            accessType: DataBreakpointAccessType.Write
        );

        watchpoint.OnHit += (sender, args) =>
        {
            var oldValue = /* read from snapshot */;
            var newValue = ReadFieldValue(componentAddress, fieldOffset);
            OnComponentChanged?.Invoke(entityId, typeof(T), fieldName, oldValue, newValue);
        };
    }
}
```

**4. Game Loop Integration**

Hook the main update loop to enable frame-by-frame stepping:

```csharp
public class GameLoopDebugger
{
    private bool stepOneFrame = false;
    private CorDebugBreakpoint frameBreakpoint;

    public void EnableFrameStepping(CorDebugModule gameModule)
    {
        // Find the game's Update/Tick method
        // Common patterns: Game.Update(), GameLoop.Tick(), App.OnUpdate()
        var updateMethod = FindUpdateMethod(gameModule);

        frameBreakpoint = breakpoints.SetMethodBreakpoint(gameModule, updateMethod);
        frameBreakpoint.OnHit += (sender, args) =>
        {
            if (stepOneFrame)
            {
                stepOneFrame = false;
                OnFrameStart?.Invoke(args.Frame);
                args.Continue = false; // Pause at frame start
            }
            else
            {
                args.Continue = true; // Keep running
            }
        };
    }

    public void StepOneFrame()
    {
        stepOneFrame = true;
        debugger.Continue(); // Resume until next frame
    }
}
```

**5. Expression Evaluation for Runtime Interaction**

Use ICorDebugEval to call methods in the debuggee process:

```csharp
public class RuntimeInteraction
{
    private readonly CorDebugEval eval;

    // Call methods in the debuggee to query ECS state
    public int GetEntityCount(CorDebugValue worldValue)
    {
        // Find World.EntityCount property getter
        var getter = FindMethod(worldValue.ExactType, "get_EntityCount");

        // Call the method in the debuggee
        eval.CallFunction(getter, [worldValue]);
        eval.WaitForResult();

        return eval.Result.As<CorDebugGenericValue>().GetValue<int>();
    }

    // Spawn an entity from the debugger
    public CorDebugValue SpawnEntity(CorDebugValue worldValue)
    {
        var spawnMethod = FindMethod(worldValue.ExactType, "Spawn");
        eval.CallFunction(spawnMethod, [worldValue]);
        eval.WaitForResult();

        var builder = eval.Result; // EntityBuilder
        var buildMethod = FindMethod(builder.ExactType, "Build");
        eval.CallFunction(buildMethod, [builder]);
        eval.WaitForResult();

        return eval.Result; // Entity
    }

    // Add component to entity at runtime
    public void AddComponent<T>(CorDebugValue worldValue, CorDebugValue entityValue, T component)
    {
        // Allocate component struct in debuggee heap
        var componentValue = AllocateStruct<T>(component);

        // Call World.Add<T>(entity, component)
        var addMethod = FindGenericMethod(worldValue.ExactType, "Add", typeof(T));
        eval.CallFunction(addMethod, [worldValue, entityValue, componentValue]);
        eval.WaitForResult();
    }
}
```

**6. Query Execution in Debugger**

Execute ECS queries to find entities matching criteria:

```csharp
public class QueryDebugger
{
    public List<EntityView> ExecuteQuery(CorDebugValue worldValue, string queryExpression)
    {
        // Parse query like "Query<Position, Velocity>().Without<Frozen>()"
        var queryAst = ParseQueryExpression(queryExpression);

        // Build and execute query in debuggee
        var queryBuilderMethod = FindMethod(worldValue.ExactType, "Query");
        eval.CallFunction(queryBuilderMethod, [worldValue], queryAst.ComponentTypes);
        var queryBuilder = eval.Result;

        // Apply filters
        foreach (var without in queryAst.WithoutTypes)
        {
            var withoutMethod = FindGenericMethod(queryBuilder.ExactType, "Without", without);
            eval.CallFunction(withoutMethod, [queryBuilder]);
            queryBuilder = eval.Result;
        }

        // Enumerate results
        var entities = new List<EntityView>();
        var enumerator = GetEnumerator(queryBuilder);
        while (MoveNext(enumerator))
        {
            var entity = GetCurrent(enumerator);
            entities.Add(VisualizeEntity(entity, worldValue));
        }

        return entities;
    }
}
```

**7. Breakpoint Conditions with ECS Context**

Create smart breakpoints that trigger on ECS conditions:

```csharp
public class EcsConditionalBreakpoint
{
    public void SetComponentBreakpoint(
        string sourceFile, int line,
        Func<CorDebugFrame, bool> condition)
    {
        var bp = breakpoints.SetBreakpoint(sourceFile, line);
        bp.OnHit += (sender, args) =>
        {
            // Evaluate ECS condition
            if (condition(args.Frame))
            {
                args.Continue = false; // Break
            }
            else
            {
                args.Continue = true; // Skip
            }
        };
    }

    // Example: Break only when entity has specific component
    public void BreakWhenEntityHas<T>(string file, int line, string entityVarName)
    {
        SetComponentBreakpoint(file, line, frame =>
        {
            var entity = frame.GetLocalVariable(entityVarName);
            var world = FindWorldInScope(frame);
            return HasComponent<T>(world, entity);
        });
    }

    // Example: Break when component field exceeds threshold
    public void BreakWhenFieldExceeds<T>(string file, int line, string field, float threshold)
    {
        SetComponentBreakpoint(file, line, frame =>
        {
            var entity = frame.GetLocalVariable("entity");
            var component = GetComponent<T>(entity);
            var value = component.GetFieldValue(field).As<CorDebugGenericValue>().GetValue<float>();
            return value > threshold;
        });
    }
}
```

#### Breakpoints Without a Code Editor

Since KeenEyes Editor focuses on scene/entity editing rather than code editing, we need alternative ways to set breakpoints:

**1. ECS-Level Breakpoints (No Source Code Needed)**

Break on ECS operations rather than source lines:

```csharp
public class EcsBreakpointPanel : EditorPanel
{
    private EcsDebugger debugger;

    // UI: Dropdown of registered systems → "Break on Entry/Exit"
    public void BreakOnSystem(Type systemType, BreakTiming timing)
    {
        var methodName = timing == BreakTiming.Entry ? "Update" : "Update";
        debugger.SetMethodBreakpoint(systemType.FullName, methodName, timing);
    }

    // UI: Entity selected in hierarchy → "Break when accessed"
    public void BreakOnEntityAccess(int entityId)
    {
        // Break when World.Get<T>(entity) is called for this entity
        debugger.SetConditionalBreakpoint(
            "KeenEyes.Core.World", "Get",
            frame => GetEntityIdFromArg(frame, 1) == entityId
        );
    }

    // UI: Component type selected → "Break on Add/Remove/Modify"
    public void BreakOnComponentChange(Type componentType, ChangeType change)
    {
        var method = change switch
        {
            ChangeType.Add => "Add",
            ChangeType.Remove => "Remove",
            _ => throw new NotSupportedException()
        };
        debugger.SetMethodBreakpoint("KeenEyes.Core.World", method, componentType);
    }

    // UI: "Break on spawn" / "Break on despawn"
    public void BreakOnEntityLifecycle(LifecycleEvent evt)
    {
        var method = evt == LifecycleEvent.Spawn ? "Spawn" : "Despawn";
        debugger.SetMethodBreakpoint("KeenEyes.Core.World", method);
    }
}
```

**UI Panel Concept:**

```
┌─────────────────────────────────────────────────────┐
│ ECS Breakpoints                              [+ Add]│
├─────────────────────────────────────────────────────┤
│ ○ System: MovementSystem                    [Entry] │
│ ○ System: PhysicsSystem                     [Exit]  │
│ ○ Entity #42 accessed                       [Any]   │
│ ○ Component: Health modified                [Write] │
│ ○ Entity spawned                            [All]   │
│ ○ Query: <Position, Velocity>               [Match] │
└─────────────────────────────────────────────────────┘
```

**2. Method Breakpoints (Type + Method Name)**

Allow users to specify breakpoints by class/method name without viewing source:

```csharp
public class MethodBreakpointDialog
{
    // UI: Searchable list of types from loaded assemblies
    public void ShowTypeSearch()
    {
        var types = debugger.GetLoadedTypes()
            .Where(t => t.Namespace?.StartsWith("MyGame") == true)
            .OrderBy(t => t.Name);

        typeListView.ItemsSource = types;
    }

    // UI: After type selected, show methods
    public void OnTypeSelected(Type type)
    {
        var methods = debugger.GetMethods(type)
            .Where(m => !m.IsSpecialName) // Skip property getters/setters
            .OrderBy(m => m.Name);

        methodListView.ItemsSource = methods;
    }

    // UI: Set breakpoint on selected method
    public void OnMethodSelected(MethodInfo method)
    {
        debugger.SetMethodBreakpoint(method.DeclaringType.FullName, method.Name);
    }
}
```

**3. External IDE Sync (Recommended for Source-Level Debugging)**

Integrate with VS Code/Rider for code-level breakpoints while editor shows ECS state:

```csharp
public class ExternalIdeSync
{
    // Read breakpoints from .vscode/launch.json or .idea Run Configurations
    public List<SourceBreakpoint> LoadExternalBreakpoints()
    {
        var vscodeConfig = Path.Combine(projectRoot, ".vscode", "launch.json");
        if (File.Exists(vscodeConfig))
        {
            // Parse VS Code breakpoints from workspace state
            return ParseVsCodeBreakpoints(vscodeConfig);
        }

        // Or sync via DAP - connect to running VS Code debug session
        return SyncFromDapSession();
    }

    // Alternative: Watch for .kebreakpoints file that external tools can write
    public void WatchBreakpointFile()
    {
        var bpFile = Path.Combine(projectRoot, ".kebreakpoints");
        fileWatcher.Watch(bpFile, () =>
        {
            var breakpoints = JsonSerializer.Deserialize<BreakpointList>(File.ReadAllText(bpFile));
            foreach (var bp in breakpoints.Items)
            {
                debugger.SetBreakpoint(bp.File, bp.Line, bp.Condition);
            }
        });
    }
}
```

**.kebreakpoints file format:**
```json
{
  "breakpoints": [
    { "file": "src/Systems/MovementSystem.cs", "line": 42 },
    { "file": "src/Systems/PhysicsSystem.cs", "line": 18, "condition": "entity.Id == 5" }
  ]
}
```

**4. Minimal Source Browser (Lightweight)**

If we want some source visibility without a full editor:

```csharp
public class SourceBreakpointPanel : EditorPanel
{
    // File tree showing .cs files in project
    private TreeView fileTree;
    // Read-only source view with line numbers
    private SourceViewer sourceViewer;
    // Breakpoint gutter (click to toggle)
    private BreakpointGutter gutter;

    public void OnFileSelected(string filePath)
    {
        var source = File.ReadAllText(filePath);
        sourceViewer.SetText(source, readOnly: true);

        var existingBps = debugger.GetBreakpoints(filePath);
        gutter.ShowBreakpoints(existingBps);
    }

    public void OnGutterClicked(int lineNumber)
    {
        var filePath = fileTree.SelectedFile;
        if (debugger.HasBreakpoint(filePath, lineNumber))
        {
            debugger.RemoveBreakpoint(filePath, lineNumber);
        }
        else
        {
            debugger.SetBreakpoint(filePath, lineNumber);
        }
        gutter.Refresh();
    }
}
```

**5. Exception Breakpoints**

Always available without source:

```csharp
public class ExceptionBreakpointPanel : EditorPanel
{
    public void BreakOnAllExceptions()
    {
        debugger.SetExceptionBreakpoint(ExceptionBreakMode.Always);
    }

    public void BreakOnUnhandledOnly()
    {
        debugger.SetExceptionBreakpoint(ExceptionBreakMode.Unhandled);
    }

    public void BreakOnSpecificException(Type exceptionType)
    {
        debugger.SetExceptionBreakpoint(exceptionType);
    }
}
```

**Recommended Approach for KeenEyes:**

| Use Case | Solution |
|----------|----------|
| Debug ECS logic | ECS-level breakpoints (system/entity/component) |
| Debug specific code | Method breakpoints by type/method name |
| Full source debugging | External IDE (VS Code/Rider) + sync |
| Quick source check | Minimal read-only source browser |
| Crash investigation | Exception breakpoints |

The editor's strength is **ECS-aware debugging** - seeing entity state, component values, query results. Leave source-level debugging to IDEs that do it well, and focus on what the editor uniquely provides.

#### Built-in Code Editor Consideration

Should KeenEyes include a basic code editor like Godot? Here's the analysis:

**Godot's Approach:**
- Built-in GDScript editor with syntax highlighting, autocomplete, debugging
- Optional C# support opens external IDE (VS/Rider/VS Code)
- Script editor is tightly integrated with node inspector
- Breakpoints set in same window as scene tree

**Unity's Approach:**
- No built-in code editor
- Double-click script → opens external IDE
- Relies on IDE integration (VS, Rider) for debugging
- Focus on visual tools (Inspector, Animator, etc.)

**Options for KeenEyes:**

| Option | Effort | Experience | Maintenance |
|--------|--------|------------|-------------|
| **1. No editor (Unity-style)** | Low | Context switching | Low |
| **2. Read-only viewer + breakpoints** | Low | View code, set BPs | Low |
| **3. Basic editor (notepad++)** | Medium | Edit + BPs, no intellisense | Medium |
| **4. LSP-integrated editor** | High | Full IDE features | High |
| **5. Embedded Monaco/AvalonEdit** | Medium-High | Good editing, some features | Medium |

**Option 2: Read-Only Viewer with Breakpoints (Minimum Viable)**

```csharp
public class CodeViewerPanel : EditorPanel
{
    private SyntaxHighlightedTextView textView; // Read-only
    private BreakpointGutter gutter;
    private FileTreeView fileTree;

    // View source, click gutter to set breakpoints
    // Double-click line → opens in external IDE at that line
    public void OpenInExternalIde(string file, int line)
    {
        // Launch configured IDE at specific line
        var ide = Settings.PreferredIde; // "code", "rider", "vs"
        Process.Start(ide, $"--goto \"{file}:{line}\"");
    }
}
```

**Option 3: Basic Editor (Godot-lite)**

Provide editing without full IDE features:

```csharp
public class BasicCodeEditor : EditorPanel
{
    private AvalonEdit.TextEditor editor;
    private BreakpointGutter gutter;

    public BasicCodeEditor()
    {
        editor = new TextEditor
        {
            SyntaxHighlighting = LoadCSharpHighlighting(),
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Consolas")
        };

        // Basic features only
        editor.TextArea.TextEntering += OnTextEntering;
        editor.TextArea.TextEntered += OnTextEntered;
    }

    // Simple bracket matching, no intellisense
    private void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == "{") InsertText("}");
        if (e.Text == "(") InsertText(")");
    }
}
```

**Option 5: LSP Integration (Full Featured)**

Use Language Server Protocol for C# features:

```csharp
public class LspCodeEditor : EditorPanel
{
    private ILanguageClient lspClient;
    private TextEditor editor;

    public async Task InitializeAsync()
    {
        // Connect to OmniSharp or csharp-ls
        lspClient = new LanguageClient();
        await lspClient.InitializeAsync(new InitializeParams
        {
            RootUri = projectRoot,
            Capabilities = new ClientCapabilities
            {
                TextDocument = new TextDocumentClientCapabilities
                {
                    Completion = new CompletionCapability { /* ... */ },
                    Hover = new HoverCapability { /* ... */ },
                    Definition = new DefinitionCapability { /* ... */ }
                }
            }
        });
    }

    // Autocomplete via LSP
    private async void OnCompletionRequested(int line, int column)
    {
        var completions = await lspClient.RequestCompletionAsync(
            currentFile, new Position(line, column));
        ShowCompletionPopup(completions);
    }
}
```

**Recommendation:**

For KeenEyes, consider a **phased approach**:

| Phase | Feature | Rationale |
|-------|---------|-----------|
| **MVP** | Read-only viewer + breakpoints | Minimal effort, enables ECS debugging |
| **v1.1** | Basic editing (AvalonEdit) | Indie devs want all-in-one |
| **v2.0** | LSP integration | Compete with Godot's experience |

**ECS-Specific Editor Enhancements:**

Even a basic editor can have ECS-aware features:

```csharp
public class EcsAwareEditor : BasicCodeEditor
{
    // Hover over component type → show current values from debugger
    public void OnHover(int line, int column)
    {
        var symbol = GetSymbolAtPosition(line, column);
        if (IsComponentType(symbol))
        {
            // Show component data from paused game
            var tooltip = debugger.GetComponentTooltip(symbol.Type);
            ShowTooltip(tooltip);
        }
    }

    // Right-click entity variable → "Inspect in Entity Browser"
    public void OnContextMenu(int line, int column)
    {
        var symbol = GetSymbolAtPosition(line, column);
        if (symbol.Type == typeof(Entity))
        {
            contextMenu.Add("Inspect Entity", () =>
            {
                var entityValue = debugger.EvaluateExpression(symbol.Name);
                entityBrowser.Select(entityValue);
            });
        }
    }

    // Autocomplete for Query<...>
    public void OnQueryCompletion()
    {
        // Show list of known component types
        var components = project.GetComponentTypes();
        ShowCompletionPopup(components.Select(c => c.Name));
    }
}
```

**Godot-Style Integration Example:**

```
┌─────────────────────────────────────────────────────────────────────┐
│ KeenEyes Editor                                                     │
├──────────────┬──────────────────────────────────────────────────────┤
│ Scene Tree   │  MovementSystem.cs                           [x]    │
│              │ ─────────────────────────────────────────────────────│
│ ▼ World      │  1 │ public class MovementSystem : SystemBase       │
│   ▼ Player   │  2 │ {                                              │
│     Position │  3 │     public override void Update(float dt)      │
│     Velocity │ ●4 │     {                                          │← Breakpoint
│     Sprite   │  5 │         foreach (var e in Query<Pos, Vel>())   │
│   ▼ Enemy    │  6 │         {                                      │
│     Position │  7 │             ref var pos = ref Get<Pos>(e);     │← Current line
│     Health   │  8 │             ref var vel = ref Get<Vel>(e);     │
│              │  9 │             pos.X += vel.X * dt;  ← pos.X=42.5 │← Inline value
│──────────────│ 10 │         }                                      │
│ Inspector    │ 11 │     }                                          │
│──────────────│ 12 │ }                                              │
│ Position     │─────────────────────────────────────────────────────│
│  X: 42.5     │ Locals          │ Call Stack                        │
│  Y: 100.0    │ e: Entity(42)   │ MovementSystem.Update() line 7    │
│ Velocity     │ pos: {X=42.5}   │ SystemManager.Execute() line 89   │
│  X: 5.0      │ vel: {X=5.0}    │ World.Update() line 156           │
│  Y: 0.0      │ dt: 0.016       │                                   │
└──────────────┴─────────────────┴───────────────────────────────────┘
```

**Decision Factors:**

| Factor | Lean Toward Built-in | Lean Toward External |
|--------|---------------------|---------------------|
| Target audience | Indie/hobbyist | Professional |
| Language complexity | Simple (GDScript-like) | C# (complex) |
| Team size | Small team wants all-in-one | Large team has IDE preferences |
| Debugging focus | Code debugging | ECS visualization |
| Development resources | Have time for editor | Focus on core engine |

**Recommendation for KeenEyes:**

Start with **Option 2 (read-only + breakpoints)** and gauge demand. The unique value is ECS-aware debugging, not competing with VS Code. If users strongly request it, add basic editing (Option 3) later.

### Approach 3: Hybrid (Recommended Architecture)

Use SharpDbg.Infrastructure for core debugging + custom ECS layer on top.

```
┌─────────────────────────────────────────────────────────────────┐
│                     KeenEyes Editor                             │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Debug Toolbar   │  │ Entity Inspector│  │ World Snapshot  │ │
│  │ (Step/Continue) │  │ (Live View)     │  │ (Pause View)    │ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
│           │                    │                    │          │
│  ┌────────▼────────────────────▼────────────────────▼────────┐ │
│  │              ECS Debug Integration Layer                  │ │
│  │  - EntityVisualizer, ComponentVisualizer, QueryVisualizer │ │
│  │  - WorldSnapshot, ArchetypeInspector                      │ │
│  └────────────────────────────┬──────────────────────────────┘ │
│                               │                                 │
│  ┌────────────────────────────▼──────────────────────────────┐ │
│  │              SharpDbg.Infrastructure                      │ │
│  │  - ManagedDebugger, BreakpointManager, VariableManager    │ │
│  │  - ExpressionEvaluator, SymbolReader                      │ │
│  └────────────────────────────┬──────────────────────────────┘ │
│                               │                                 │
│  ┌────────────────────────────▼──────────────────────────────┐ │
│  │                     ClrDebug                              │ │
│  │  - ICorDebug wrappers, IMetaData, Symbol APIs             │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │   Debuggee Game     │
                    │   (KeenEyes App)    │
                    └─────────────────────┘
```

## NuGet Packages for DAP

### For Hosting Debug Adapters (Client Side)

```xml
<!-- Microsoft's official DAP client/host library -->
<PackageReference Include="Microsoft.VisualStudio.Shared.VSCodeDebugProtocol" Version="18.0.10427.1" />
```

This package provides:
- `DebugProtocolHost` for communicating with adapters
- Request/response/event types for all DAP messages
- Stream-based communication helpers

### For Implementing Debug Adapters (Server Side)

```xml
<!-- ClrDebug for ICorDebug access -->
<PackageReference Include="ClrDebug" Version="0.3.4" />
```

## SharpIDE Reference Implementation

Matt Parker's [SharpIDE](https://github.com/MattParkerDev/SharpIDE) provides a working reference for debugger integration.

### Key Files

| File | Purpose |
|------|---------|
| `Features/Debugging/Debugger.cs` | Facade wrapping debugging operations |
| `Features/Debugging/DebuggingService.cs` | DAP client implementation |
| `Features/Debugging/Breakpoint.cs` | Breakpoint model |
| `Features/Debugging/ExecutionStopInfo.cs` | Execution pause state |
| `Features/Debugging/ThreadsStackTraceModel.cs` | Thread/stack representation |

### DebuggingService Pattern

```csharp
public class DebuggingService
{
    private DebugProtocolHost protocolHost;
    private Process debuggerProcess;

    public async Task AttachAsync(int processId, string debuggerPath)
    {
        // 1. Start debugger process
        debuggerProcess = StartDebugger(debuggerPath, "--interpreter=vscode");

        // 2. Create protocol host
        protocolHost = new DebugProtocolHost(
            debuggerProcess.StandardInput.BaseStream,
            debuggerProcess.StandardOutput.BaseStream);

        // 3. Register event handlers
        protocolHost.EventReceived += (sender, args) =>
        {
            switch (args.Event)
            {
                case StoppedEvent stopped:
                    OnExecutionStopped(stopped);
                    break;
                case OutputEvent output:
                    OnDebugOutput(output);
                    break;
                // ... other events
            }
        };

        // 4. Start protocol host
        protocolHost.Run();

        // 5. Initialize
        await protocolHost.SendRequestAsync(new InitializeRequest
        {
            ClientID = "keeneyes-editor",
            AdapterID = "coreclr",
            LinesStartAt1 = true,
            ColumnsStartAt1 = true,
            PathFormat = "path"
        });

        // 6. Attach to process
        await protocolHost.SendRequestAsync(new AttachRequest
        {
            // ProcessId is a custom argument handled by the adapter
            __ConfigurationProperties = new Dictionary<string, JToken>
            {
                ["processId"] = processId
            }
        });

        // 7. Set breakpoints
        await SetBreakpointsAsync(initialBreakpoints);

        // 8. Signal configuration complete
        await protocolHost.SendRequestAsync(new ConfigurationDoneRequest());
    }

    public async Task<StackTraceResponse> GetStackTraceAsync(int threadId)
    {
        return await protocolHost.SendRequestAsync(new StackTraceRequest
        {
            ThreadId = threadId
        });
    }

    public async Task StepOverAsync(int threadId)
    {
        await protocolHost.SendRequestAsync(new NextRequest
        {
            ThreadId = threadId
        });
    }
}
```

## KeenEyes Editor Integration Plan

### Phase 1: Core Debugger Integration

1. Add SharpDbg.Infrastructure as a project reference or submodule
2. Add ClrDebug NuGet package
3. Create `EcsDebugger` service wrapping ManagedDebugger
4. Implement attach/detach workflow for game processes
5. Handle debugger callbacks (breakpoint hit, step complete, exception)
6. Basic breakpoint support (line-based)

### Phase 2: Standard Debug UI

1. Debug toolbar (continue, step over, step in, step out, pause, stop)
2. Breakpoint gutter markers in code editor
3. Call stack panel with frame navigation
4. Locals/Watch panel using VariableManager
5. Debug console for application output
6. Threads panel for multi-threaded debugging

### Phase 3: ECS-Specific Visualizers

Custom views leveraging direct ICorDebugValue access:

| Visualizer | Description |
|------------|-------------|
| **World Inspector** | Shows all worlds, entity counts, registered systems |
| **Entity Browser** | Hierarchical entity tree with parent/child relationships |
| **Component Inspector** | Struct field editor with live values during pause |
| **Archetype Viewer** | Visualize archetype composition and entity distribution |
| **Query Debugger** | Show which entities match a query, with component data |
| **System Profiler** | Execution time per system, integrated with debug stepping |

```csharp
// Example: Entity browser integration
public class EntityBrowserPanel : EditorPanel
{
    private EcsDebugger debugger;

    public void OnDebuggerPaused(PausedEventArgs args)
    {
        // Find World instance in current scope
        var worldValue = debugger.FindLocalVariable("world");
        if (worldValue == null)
            worldValue = debugger.FindStaticField("Game.Instance.World");

        if (worldValue != null)
        {
            var snapshot = debugger.InspectWorld(worldValue);
            RefreshEntityTree(snapshot);
        }
    }

    private void OnEntitySelected(int entityId)
    {
        var components = debugger.GetEntityComponents(entityId);
        componentInspector.Show(components);
    }
}
```

### Phase 4: Live Debugging Features

1. **Component Edit**: Modify component values during pause, apply on continue
2. **Entity Spawn/Despawn**: Create/destroy entities from debug UI
3. **Query Filtering**: Filter entity browser by component query
4. **Breakpoint on Entity**: Break when specific entity is accessed
5. **System Step**: Step through systems one at a time in update loop

### Phase 5: Advanced Integration

1. **Conditional breakpoints** with ECS expressions (`entity.Has<Position>()`)
2. **Data breakpoints** on component field changes
3. **Exception breakpoints** with component context
4. **Hot reload** integration (recompile systems, keep world state)
5. **Replay integration**: Debug from replay checkpoint
6. **Remote debugging**: Debug game running on another machine

## Building SharpDbg

```bash
git clone https://github.com/MattParkerDev/sharpdbg.git
cd sharpdbg
dotnet build
# Output: artifacts/bin/SharpDbg.Cli/Debug/net10.0/SharpDbg.Cli.exe
```

The built executable is functionally equivalent to `netcoredbg.exe` and can be used with the `--interpreter=vscode` argument for DAP mode.

## Key Considerations

### Cross-Platform Support

- SharpDbg and ClrDebug support Windows, Linux, and macOS
- ICorDebug APIs are available on all .NET platforms
- DAP is platform-agnostic

### Native AOT Compatibility

- SharpDbg itself uses reflection for the expression evaluator
- The DAP client (our editor code) can be AOT-compatible
- ClrDebug uses COM interop which works with AOT

### Error Handling

The debug adapter may crash or become unresponsive. Handle:
- Process exit events
- Communication timeouts
- Malformed DAP messages
- Adapter initialization failures

### Debugger Selection

Consider supporting multiple debuggers:
- SharpDbg (primary, pure C#)
- netcoredbg (fallback, more mature)
- Custom adapters for specific scenarios

## See Also

- [Scene Editor Architecture](scene-editor-architecture.md) - Overall editor design
- [Framework Editor](framework-editor.md) - Editor plugin architecture
- [Editor Plugin Architecture](../adr/012-editor-plugin-extension-architecture.md) - ADR for plugin extensions
- [Dynamic Plugin Loading](../adr/013-dynamic-plugin-loading.md) - ADR for runtime plugin loading

## Sources

- [SharpDbg GitHub](https://github.com/MattParkerDev/sharpdbg)
- [SharpIDE GitHub](https://github.com/MattParkerDev/SharpIDE)
- [Debug Adapter Protocol Specification](https://microsoft.github.io/debug-adapter-protocol/specification.html)
- [DAP Overview](https://microsoft.github.io/debug-adapter-protocol/overview.html)
- [ClrDebug GitHub](https://github.com/lordmilko/ClrDebug)
- [netcoredbg GitHub](https://github.com/Samsung/netcoredbg)
- [ICorDebug Interface](https://learn.microsoft.com/en-us/dotnet/core/unmanaged-api/debugging/icordebug/icordebug-interface)
- [Microsoft.VisualStudio.Shared.VSCodeDebugProtocol NuGet](https://www.nuget.org/packages/Microsoft.VisualStudio.Shared.VsCodeDebugProtocol)
- [VSDebugAdapterHost Sample](https://github.com/microsoft/VSDebugAdapterHost)
