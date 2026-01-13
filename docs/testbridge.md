# TestBridge Architecture Guide

The TestBridge enables external tools and test frameworks to inspect and control running KeenEyes applications. This guide covers the complete architecture, from high-level concepts to protocol-level details.

## Overview

The TestBridge provides a unified API for:

- **State Inspection** - Query entities, components, systems, and world statistics
- **Input Injection** - Simulate keyboard, mouse, and gamepad input
- **Screen Capture** - Take screenshots and record frames
- **Log Access** - Query and search application logs
- **Window State** - Monitor window size, focus, and title

### Use Cases

| Use Case | Description |
|----------|-------------|
| AI-Assisted Testing | Claude Code or other AI tools can explore and test game state |
| Automated Testing | Integration tests can run without graphics drivers via headless mode |
| Game AI Agents | Build AI agents that play games using visual and state feedback |
| Debugging | Inspect live game state from external tools |
| Remote Monitoring | Monitor game state over network connections |

## Architecture

The TestBridge architecture consists of multiple layers, each with a specific responsibility:

```
┌─────────────────────────────────────────────────────────────────┐
│                       External Tools                             │
│  (Claude Code, Test Runners, Debug Tools)                       │
└─────────────────────┬───────────────────────────────────────────┘
                      │ MCP (Model Context Protocol)
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              KeenEyes.Mcp.TestBridge                            │
│  MCP Server exposing TestBridge as standardized tools           │
│  - GameTools: Connection management                              │
│  - StateTools: World inspection                                  │
│  - InputTools: Input simulation                                  │
│  - CaptureTools: Screenshot capture                              │
│  - LogTools: Log queries                                         │
│  - WindowTools: Window state                                     │
└─────────────────────┬───────────────────────────────────────────┘
                      │ IPC (Named Pipes or TCP)
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              KeenEyes.TestBridge.Ipc                            │
│  IPC transport and command routing layer                         │
│  - IpcBridgeServer: Accepts external connections                 │
│  - Command Handlers: Route commands to controllers               │
│  - Protocol: JSON over length-prefixed messages                  │
└─────────────────────┬───────────────────────────────────────────┘
                      │ Direct API calls
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              KeenEyes.TestBridge                                │
│  Core implementation with direct World access                    │
│  - InProcessBridge: Direct World manipulation                    │
│  - StateControllerImpl: Entity/component queries                 │
│  - InputControllerImpl: Virtual input injection                  │
│  - CaptureControllerImpl: Screenshot capture                     │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                        World (ECS)                              │
│  The game's entity-component-system                              │
│  - Entities, Components, Systems                                 │
│  - ArchetypeManager, QueryManager                                │
│  - ComponentRegistry                                             │
└─────────────────────────────────────────────────────────────────┘
```

### Package Overview

| Package | Purpose | Usage |
|---------|---------|-------|
| `KeenEyes.TestBridge.Abstractions` | Interfaces and data types | Reference for custom implementations |
| `KeenEyes.TestBridge` | Core implementation | In-process testing, game integration |
| `KeenEyes.TestBridge.Ipc` | IPC layer | External tool connections |
| `KeenEyes.Mcp.TestBridge` | MCP server | AI tool integration |

## Integration Guide

### Enabling TestBridge in Your Game

Add the TestBridge plugin to your game's World:

```csharp
using KeenEyes.TestBridge;

public class Game
{
    private TestBridgeManager? testBridge;

    public void Initialize(World world, IGraphicsContext? graphics = null)
    {
        // Create options
        var options = new TestBridgeOptions
        {
            GraphicsContext = graphics,          // For screenshot capture
            EnableCapture = graphics != null,
            IpcOptions = new IpcOptions
            {
                PipeName = "MyGame.TestBridge",
                TransportMode = IpcTransportMode.NamedPipe
            }
        };

        // Install plugin
        world.InstallPlugin(new TestBridgePlugin(options));

        // Get bridge reference for use in game code
        var bridge = world.GetExtension<ITestBridge>();

        // Optional: Start IPC server for external connections
        testBridge = new TestBridgeManager(world);
        _ = testBridge.StartAsync(CancellationToken.None);
    }

    public void Shutdown()
    {
        testBridge?.StopAsync().GetAwaiter().GetResult();
        testBridge?.Dispose();
    }
}
```

### In-Process Testing

For unit and integration tests, use the bridge directly without IPC:

```csharp
using KeenEyes.TestBridge;

[Fact]
public async Task Player_TakeDamage_ReducesHealth()
{
    // Arrange
    using var world = new World();
    world.InstallPlugin(new TestBridgePlugin());
    var bridge = world.GetExtension<ITestBridge>();

    var player = world.Spawn()
        .With(new Health { Current = 100, Max = 100 })
        .WithName("Player")
        .Build();

    // Act - Inject input
    await bridge.Input.KeyPressAsync(Key.Space);
    world.Update(0.016f); // One frame

    // Assert - Query state
    var entity = await bridge.State.GetEntityByNameAsync("Player");
    Assert.NotNull(entity);

    var health = await bridge.State.GetComponentAsync(entity.Id, "Health");
    // Check health value...
}
```

### External Tool Connection

Configure the MCP server in `.mcp.json`:

```json
{
  "mcpServers": {
    "keeneyes-bridge": {
      "type": "stdio",
      "command": ".mcp/KeenEyes.Mcp.TestBridge.exe",
      "args": [],
      "env": {
        "KEENEYES_TRANSPORT": "pipe",
        "KEENEYES_PIPE_NAME": "MyGame.TestBridge"
      }
    }
  }
}
```

Publish the MCP server:

```bash
dotnet publish tools/KeenEyes.Mcp.TestBridge -c Release -o .mcp
```

## IPC Protocol Specification

The IPC layer uses a simple request/response protocol over named pipes or TCP.

### Message Framing

Each message is framed with:
1. **Type byte** (1 byte) - Message type identifier
2. **Length** (4 bytes, little-endian) - Payload length
3. **Payload** (variable) - JSON or binary data

```
┌──────┬──────────────────┬─────────────────────────┐
│ Type │ Length (4 bytes) │ Payload (N bytes)       │
│ 0x01 │ 0x2A 0x00 0x00   │ {"id":1,"command":...}  │
└──────┴──────────────────┴─────────────────────────┘
```

### Message Types

| Type | Value | Description |
|------|-------|-------------|
| `Json` | `0x01` | JSON request or response |
| `Binary` | `0x02` | Raw binary data (screenshots) |
| `Ping` | `0x03` | Keep-alive ping |
| `Pong` | `0x04` | Keep-alive response |

### Request Format

```json
{
  "id": 1,
  "command": "state.getEntityCount",
  "args": null
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | `int` | Unique request ID for response correlation |
| `command` | `string` | Command in `prefix.action` format |
| `args` | `object?` | Command-specific arguments (optional) |

### Response Format

```json
{
  "id": 1,
  "success": true,
  "error": null,
  "data": 42
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | `int` | Correlates to request ID |
| `success` | `bool` | Whether command succeeded |
| `error` | `string?` | Error message if failed |
| `data` | `any?` | Command result (type varies) |

### Command Prefixes

| Prefix | Handler | Description |
|--------|---------|-------------|
| `state` | `StateCommandHandler` | World state queries |
| `input` | `InputCommandHandler` | Input injection |
| `capture` | `CaptureCommandHandler` | Screenshot capture |
| `log` | `LogCommandHandler` | Log queries |
| `window` | `WindowCommandHandler` | Window state |

## Command Reference

### State Commands

Commands for querying world state. All commands return snapshots; the actual state may change between queries.

#### `state.getEntityCount`

Returns the total number of entities in the world.

**Arguments:** None

**Returns:** `int` - Entity count

**Example:**
```json
// Request
{"id": 1, "command": "state.getEntityCount", "args": null}

// Response
{"id": 1, "success": true, "data": 42}
```

#### `state.queryEntities`

Queries entities with optional filters.

**Arguments:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `withComponents` | `string[]?` | `null` | Component types entities must have |
| `withoutComponents` | `string[]?` | `null` | Component types entities must NOT have |
| `withTags` | `string[]?` | `null` | Tags entities must have |
| `namePattern` | `string?` | `null` | Wildcard pattern (`*`, `?`) for names |
| `parentId` | `int?` | `null` | Filter by parent entity |
| `skip` | `int` | `0` | Results to skip (pagination) |
| `maxResults` | `int` | `1000` | Maximum results |
| `includeComponentData` | `bool` | `false` | Include component field values |

**Returns:** `EntitySnapshot[]`

**Example:**
```json
// Request
{
  "id": 2,
  "command": "state.queryEntities",
  "args": {
    "withComponents": ["Position", "Velocity"],
    "withoutComponents": ["Frozen"],
    "maxResults": 50
  }
}

// Response
{
  "id": 2,
  "success": true,
  "data": [
    {
      "id": 1,
      "version": 1,
      "name": "Player",
      "componentTypes": ["Position", "Velocity", "Health"],
      "components": {},
      "parentId": null,
      "childIds": [],
      "tags": ["Player"]
    }
  ]
}
```

#### `state.getEntity`

Gets detailed information about a specific entity.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `entityId` | `int` | Yes | Entity ID |

**Returns:** `EntitySnapshot?` - Entity data or null if not found

#### `state.getEntityByName`

Finds an entity by name.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | `string` | Yes | Entity name |

**Returns:** `EntitySnapshot?` - First matching entity or null

#### `state.getComponent`

Gets component data from an entity.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `entityId` | `int` | Yes | Entity ID |
| `componentTypeName` | `string` | Yes | Component type name |

**Returns:** `JsonElement?` - Component fields as JSON

**Example:**
```json
// Request
{
  "id": 5,
  "command": "state.getComponent",
  "args": {"entityId": 1, "componentTypeName": "Position"}
}

// Response
{
  "id": 5,
  "success": true,
  "data": {"x": 10.5, "y": 20.0}
}
```

#### `state.getWorldStats`

Gets world statistics.

**Arguments:** None

**Returns:** `WorldStats`

```typescript
interface WorldStats {
  entityCount: number;
  archetypeCount: number;
  systemCount: number;
  memoryBytes: number;
  componentTypeCount: number;
  pluginCount: number;
  frameNumber: number;
  elapsedTime: number;  // milliseconds
  logStats?: LogStatsSnapshot;
}
```

#### `state.getSystems`

Gets all registered systems.

**Arguments:** None

**Returns:** `SystemInfo[]`

```typescript
interface SystemInfo {
  typeName: string;
  phase: string;
  order: number;
  enabled: boolean;
  averageExecutionMs: number;
  groupName?: string;
}
```

#### `state.getPerformanceMetrics`

Gets performance metrics for recent frames.

**Arguments:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `frameCount` | `int` | `60` | Frames to analyze |

**Returns:** `PerformanceMetrics`

```typescript
interface PerformanceMetrics {
  averageFrameTimeMs: number;
  minFrameTimeMs: number;
  maxFrameTimeMs: number;
  averageFps: number;
  p99FrameTimeMs: number;
  sampleCount: number;
  systemAverages: Record<string, number>;  // system name -> avg ms
}
```

#### `state.getChildren`

Gets child entity IDs.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `parentId` | `int` | Yes | Parent entity ID |

**Returns:** `int[]` - Child entity IDs

#### `state.getParent`

Gets parent entity ID.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `entityId` | `int` | Yes | Entity ID |

**Returns:** `int?` - Parent ID or null

#### `state.getEntitiesWithTag`

Finds entities with a specific tag.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `tag` | `string` | Yes | Tag name |

**Returns:** `int[]` - Entity IDs

#### `state.getExtension`

Gets a world extension by type name.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `typeName` | `string` | Yes | Extension type name |

**Returns:** `object?` - Extension data (if serializable)

#### `state.hasExtension`

Checks if an extension is registered.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `typeName` | `string` | Yes | Extension type name |

**Returns:** `bool`

---

### Input Commands

Commands for injecting keyboard, mouse, and gamepad input.

#### Keyboard Commands

##### `input.keyDown`

Presses a key down (hold).

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `key` | `string` | Yes | - | Key name (see Key Values) |
| `modifiers` | `string` | No | `None` | Modifier keys (comma-separated) |

##### `input.keyUp`

Releases a held key.

**Arguments:** Same as `keyDown`

##### `input.keyPress`

Presses and releases a key.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `key` | `string` | Yes | - | Key name |
| `modifiers` | `string` | No | `None` | Modifier keys |
| `holdDurationMs` | `float` | No | `0` | Hold duration in milliseconds |

##### `input.typeText`

Types a string of text.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `text` | `string` | Yes | - | Text to type |
| `delayBetweenCharsMs` | `float` | No | `0` | Delay between characters |

##### `input.isKeyDown`

Checks if a key is currently pressed.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `key` | `string` | Yes | Key name |

**Returns:** `bool`

**Key Values:**

Common keys: `Space`, `Enter`, `Escape`, `Tab`, `Backspace`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`

Arrow keys: `Up`, `Down`, `Left`, `Right`

Letters: `A` through `Z`

Numbers: `Number0` through `Number9`, `Keypad0` through `Keypad9`

Function keys: `F1` through `F12`

Modifiers: `ShiftLeft`, `ShiftRight`, `ControlLeft`, `ControlRight`, `AltLeft`, `AltRight`, `SuperLeft`, `SuperRight`

**Modifier Values:** `None`, `Shift`, `Ctrl`, `Alt`, `Super` (can be comma-separated)

#### Mouse Commands

##### `input.mouseMove`

Moves mouse to absolute position.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `x` | `float` | Yes | X coordinate |
| `y` | `float` | Yes | Y coordinate |

##### `input.mouseMoveRelative`

Moves mouse by relative delta.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `deltaX` | `float` | Yes | X delta |
| `deltaY` | `float` | Yes | Y delta |

##### `input.mouseDown`

Presses a mouse button down.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `button` | `string` | No | `Left` | Mouse button |

##### `input.mouseUp`

Releases a mouse button.

**Arguments:** Same as `mouseDown`

##### `input.click`

Clicks at a position.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `x` | `float` | Yes | - | X coordinate |
| `y` | `float` | Yes | - | Y coordinate |
| `button` | `string` | No | `Left` | Mouse button |

##### `input.doubleClick`

Double-clicks at a position.

**Arguments:** Same as `click`

##### `input.drag`

Drags from one position to another.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `startX` | `float` | Yes | - | Start X |
| `startY` | `float` | Yes | - | Start Y |
| `endX` | `float` | Yes | - | End X |
| `endY` | `float` | Yes | - | End Y |
| `button` | `string` | No | `Left` | Mouse button |

##### `input.scroll`

Scrolls the mouse wheel.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `deltaX` | `float` | No | `0` | Horizontal scroll |
| `deltaY` | `float` | No | `0` | Vertical scroll |

##### `input.getMousePosition`

Gets current mouse position.

**Arguments:** None

**Returns:** `{x: float, y: float}`

##### `input.isMouseButtonDown`

Checks if a mouse button is pressed.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `button` | `string` | Yes | Mouse button |

**Returns:** `bool`

**Mouse Button Values:** `Left`, `Right`, `Middle`, `Button4`, `Button5`

#### Gamepad Commands

##### `input.gamepadButtonDown`

Presses a gamepad button.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `gamepadIndex` | `int` | Yes | Gamepad index (0-based) |
| `button` | `string` | Yes | Button name |

##### `input.gamepadButtonUp`

Releases a gamepad button.

**Arguments:** Same as `gamepadButtonDown`

##### `input.setLeftStick`

Sets left stick position.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `gamepadIndex` | `int` | Yes | Gamepad index |
| `x` | `float` | Yes | X position (-1.0 to 1.0) |
| `y` | `float` | Yes | Y position (-1.0 to 1.0) |

##### `input.setRightStick`

Sets right stick position.

**Arguments:** Same as `setLeftStick`

##### `input.setTrigger`

Sets trigger value.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `gamepadIndex` | `int` | Yes | Gamepad index |
| `isLeft` | `bool` | Yes | True for left trigger |
| `value` | `float` | Yes | Trigger value (0.0 to 1.0) |

##### `input.setGamepadConnected`

Connects or disconnects a virtual gamepad.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `gamepadIndex` | `int` | Yes | Gamepad index |
| `connected` | `bool` | Yes | Connection state |

##### `input.isGamepadButtonDown`

Checks if a gamepad button is pressed.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `gamepadIndex` | `int` | Yes | Gamepad index |
| `button` | `string` | Yes | Button name |

**Returns:** `bool`

##### `input.gamepadCount`

Gets the number of connected gamepads.

**Arguments:** None

**Returns:** `int`

**Gamepad Button Values:** `South` (A), `East` (B), `West` (X), `North` (Y), `LeftShoulder`, `RightShoulder`, `Back`, `Start`, `Guide`, `LeftStick`, `RightStick`, `DPadUp`, `DPadDown`, `DPadLeft`, `DPadRight`

#### Action Commands

##### `input.triggerAction`

Triggers a named input action.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `actionName` | `string` | Yes | Action name |

##### `input.setActionValue`

Sets an axis-based action value.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `actionName` | `string` | Yes | Action name |
| `value` | `float` | Yes | Action value |

##### `input.setActionVector2`

Sets a 2D action value.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `actionName` | `string` | Yes | Action name |
| `x` | `float` | Yes | X value |
| `y` | `float` | Yes | Y value |

##### `input.resetAll`

Resets all input state.

**Arguments:** None

---

### Capture Commands

Commands for screenshot capture and frame recording.

#### `capture.isAvailable`

Checks if capture is available.

**Arguments:** None

**Returns:** `bool`

#### `capture.getFrameSize`

Gets the frame dimensions.

**Arguments:** None

**Returns:** `{width: int, height: int}`

#### `capture.getScreenshotBytes`

Captures a screenshot.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `format` | `string` | No | `Png` | Image format |

**Returns:** `string` - Base64-encoded image data

**Image Format Values:** `Png`, `Jpeg`, `Bmp`

#### `capture.saveScreenshot`

Saves a screenshot to a file.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `filePath` | `string` | Yes | - | Output file path |
| `format` | `string` | No | `Png` | Image format |

**Returns:** `string` - Saved file path

#### `capture.captureRegion`

Captures a region of the screen.

**Arguments:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `x` | `int` | Yes | Region X |
| `y` | `int` | Yes | Region Y |
| `width` | `int` | Yes | Region width |
| `height` | `int` | Yes | Region height |

**Returns:** `FrameCapture` - Captured frame data

#### `capture.getRegionScreenshotBytes`

Gets a region screenshot as bytes.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `x` | `int` | Yes | - | Region X |
| `y` | `int` | Yes | - | Region Y |
| `width` | `int` | Yes | - | Region width |
| `height` | `int` | Yes | - | Region height |
| `format` | `string` | No | `Png` | Image format |

**Returns:** `string` - Base64-encoded image data

#### `capture.saveRegionScreenshot`

Saves a region screenshot to a file.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `x` | `int` | Yes | - | Region X |
| `y` | `int` | Yes | - | Region Y |
| `width` | `int` | Yes | - | Region width |
| `height` | `int` | Yes | - | Region height |
| `filePath` | `string` | Yes | - | Output file path |
| `format` | `string` | No | `Png` | Image format |

**Returns:** `string` - Saved file path

#### `capture.startRecording`

Starts frame recording.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `maxFrames` | `int` | No | `300` | Maximum frames to record |

#### `capture.stopRecording`

Stops frame recording.

**Arguments:** None

#### `capture.isRecording`

Checks if recording is active.

**Arguments:** None

**Returns:** `bool`

#### `capture.recordedFrameCount`

Gets the number of recorded frames.

**Arguments:** None

**Returns:** `int`

---

### Log Commands

Commands for querying application logs.

#### `log.getCount`

Gets total log entry count.

**Arguments:** None

**Returns:** `int`

#### `log.getRecent`

Gets recent log entries.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `count` | `int` | No | `100` | Number of entries |

**Returns:** `LogEntrySnapshot[]`

#### `log.query`

Queries logs with filters.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `minLevel` | `int` | No | `0` | Minimum log level |
| `maxLevel` | `int` | No | `int.Max` | Maximum log level |
| `categoryPattern` | `string` | No | `null` | Category pattern (wildcards) |
| `messagePattern` | `string` | No | `null` | Message pattern (wildcards) |
| `startTime` | `datetime` | No | `null` | Start time filter |
| `endTime` | `datetime` | No | `null` | End time filter |
| `skip` | `int` | No | `0` | Entries to skip |
| `maxResults` | `int` | No | `1000` | Maximum results |

**Returns:** `LogEntrySnapshot[]`

#### `log.getStats`

Gets log statistics.

**Arguments:** None

**Returns:** `LogStatsSnapshot`

```typescript
interface LogStatsSnapshot {
  totalCount: number;
  countByLevel: Record<number, number>;
  categories: string[];
}
```

#### `log.clear`

Clears all log entries.

**Arguments:** None

#### `log.getByLevel`

Gets logs filtered by level.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `level` | `int` | Yes | - | Log level |
| `maxResults` | `int` | No | `1000` | Maximum results |

**Returns:** `LogEntrySnapshot[]`

#### `log.getByCategory`

Gets logs filtered by category.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `categoryPattern` | `string` | Yes | - | Category pattern |
| `maxResults` | `int` | No | `1000` | Maximum results |

**Returns:** `LogEntrySnapshot[]`

#### `log.search`

Searches log messages.

**Arguments:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `searchText` | `string` | Yes | - | Search text |
| `maxResults` | `int` | No | `1000` | Maximum results |

**Returns:** `LogEntrySnapshot[]`

---

### Window Commands

Commands for querying window state.

#### `window.isAvailable`

Checks if window controller is available.

**Arguments:** None

**Returns:** `bool`

#### `window.getState`

Gets complete window state.

**Arguments:** None

**Returns:** `WindowStateSnapshot`

```typescript
interface WindowStateSnapshot {
  title: string;
  width: number;
  height: number;
  isFocused: boolean;
  isClosing: boolean;
  aspectRatio: number;
}
```

#### `window.getSize`

Gets window dimensions.

**Arguments:** None

**Returns:** `{width: int, height: int}`

#### `window.getTitle`

Gets window title.

**Arguments:** None

**Returns:** `string`

#### `window.isFocused`

Checks if window is focused.

**Arguments:** None

**Returns:** `bool`

#### `window.isClosing`

Checks if window is closing.

**Arguments:** None

**Returns:** `bool`

#### `window.getAspectRatio`

Gets window aspect ratio.

**Arguments:** None

**Returns:** `float`

---

## MCP Server Configuration

The MCP server bridges the TestBridge to AI tools via the Model Context Protocol.

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KEENEYES_TRANSPORT` | `pipe` | Transport mode: `pipe` or `tcp` |
| `KEENEYES_PIPE_NAME` | `KeenEyes.TestBridge` | Named pipe name |
| `KEENEYES_HOST` | `127.0.0.1` | TCP host address |
| `KEENEYES_PORT` | `19283` | TCP port |
| `KEENEYES_TIMEOUT` | `30000` | Connection timeout (ms) |
| `KEENEYES_HEARTBEAT_INTERVAL` | `5000` | Heartbeat interval (ms) |
| `KEENEYES_HEARTBEAT_TIMEOUT` | `10000` | Heartbeat timeout (ms) |
| `KEENEYES_MAX_PING_FAILURES` | `3` | Max consecutive ping failures |

### Command-Line Arguments

| Argument | Description |
|----------|-------------|
| `--pipe <name>` | Named pipe name |
| `--host <host>` | TCP host address |
| `--port <port>` | TCP port |
| `--transport <mode>` | Transport mode: `pipe` or `tcp` |
| `--timeout <ms>` | Connection timeout |

### MCP Tools

The MCP server exposes TestBridge functionality as MCP tools:

| Tool | Description |
|------|-------------|
| `game_connect` | Connect to running game |
| `game_disconnect` | Disconnect from game |
| `game_status` | Get connection status |
| `game_get_screen_size` | Get window dimensions |
| `game_wait_for_condition` | Wait for game state |
| `state_*` | State query tools |
| `input_*` | Input injection tools |
| `capture_*` | Screenshot capture tools |
| `log_*` | Log query tools |
| `window_*` | Window state tools |

### MCP Resources

URI-based resources for direct state access:

| URI | Description |
|-----|-------------|
| `keeneyes://connection/status` | Connection status |
| `keeneyes://world/stats` | World statistics |
| `keeneyes://world/systems` | System list |
| `keeneyes://world/performance` | Performance metrics |
| `keeneyes://entity/{id}` | Entity by ID |
| `keeneyes://entity/name/{name}` | Entity by name |
| `keeneyes://entity/{id}/component/{type}` | Component data |
| `keeneyes://extension/{typeName}` | World extension |
| `keeneyes://capture/screenshot` | Current screenshot |

### MCP Prompts

Pre-built prompts for common workflows:

| Prompt | Description |
|--------|-------------|
| `connect_and_explore` | Connect and explore entities |
| `test_input_sequence` | Guide through input testing |
| `capture_and_describe` | Capture and describe scene |
| `monitor_entity` | Watch entity state changes |

## Transport Options

### Named Pipes

**Advantages:**
- Lower latency (~0.1ms vs ~1ms for TCP)
- More secure (local-only by default)
- No port conflicts
- Simpler firewall handling

**Disadvantages:**
- Local machine only
- Platform-specific naming (`\\.\pipe\` on Windows, socket file on Unix)

**Use when:**
- Testing on the same machine
- Security is a concern
- Performance is critical

### TCP

**Advantages:**
- Works across machines
- Standard network tooling
- Language-agnostic

**Disadvantages:**
- Higher latency
- Requires port management
- Firewall configuration needed

**Use when:**
- Testing from a different machine
- Using tools that don't support named pipes
- Network debugging scenarios

## Performance Considerations

### Query Optimization

1. **Avoid `includeComponentData` when not needed** - Component serialization is expensive
2. **Use pagination** - Set appropriate `maxResults` and use `skip` for large result sets
3. **Filter early** - Use `withComponents`/`withoutComponents` to reduce results
4. **Batch queries** - Combine multiple queries when possible

### IPC Overhead

| Operation | Named Pipe | TCP |
|-----------|------------|-----|
| Small query | ~0.1ms | ~1ms |
| Entity list (100) | ~1ms | ~2ms |
| Component query | ~0.5ms | ~1.5ms |
| Screenshot | ~50ms | ~60ms |

### Memory Usage

- The bridge maintains a buffer for message framing (~64KB)
- Screenshot capture allocates image buffer (width × height × 4 bytes)
- Frame recording stores up to 300 frames in memory
- Entity snapshots are created per query (not cached)

## Security Considerations

### Named Pipes

- Named pipes are accessible to any process on the local machine
- Use unique pipe names to avoid conflicts
- Consider adding authentication for production builds

### TCP

- Default binding to `127.0.0.1` restricts to local connections
- **Never bind to `0.0.0.0` in production** without authentication
- Use firewall rules to restrict access
- Consider TLS for remote connections

### Input Injection

- Input injection bypasses normal input validation
- Use only in development/testing environments
- Consider disabling in release builds

## Troubleshooting

### Connection Issues

**Cannot connect to game:**
1. Verify the game is running with TestBridge enabled
2. Check pipe name matches exactly
3. For TCP, verify host/port are correct
4. Check firewall settings

**Connection drops frequently:**
1. Increase `KEENEYES_HEARTBEAT_TIMEOUT`
2. Check for long-running operations blocking the game loop
3. Verify network stability (TCP)

### State Query Issues

**Query returns empty results:**
1. Verify component names are correct (case-sensitive)
2. Check entity exists with `getEntityCount`
3. Verify filter conditions aren't too restrictive

**Component data serialization fails:**
1. Components must be serializable to JSON
2. Avoid circular references
3. Check for non-serializable types (delegates, pointers)

### Input Issues

**Input not working:**
1. Verify window has focus
2. Check if input is blocked by UI
3. Use `input.resetAll` to clear stuck state
4. Verify key/button names are correct

### Capture Issues

**Capture not available:**
1. Requires graphics context (not headless)
2. Must be called from render thread
3. Check for OpenGL/Vulkan context errors

## Related Documentation

- [MCP Server Quick Start](mcp-server.md) - Quick setup guide
- [Testing Guide](testing.md) - Unit testing with mocks
- [Plugins Guide](plugins.md) - Plugin architecture
- [Input Guide](input.md) - Input system overview
