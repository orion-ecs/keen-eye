# MCP TestBridge Server

Bridge LLM clients to running KeenEyes games via the Model Context Protocol (MCP).

> **Full Documentation:** For complete architecture details, IPC protocol specification, and command reference, see [TestBridge Architecture Guide](testbridge.md).

## What is This?

The MCP TestBridge Server enables AI assistants (Claude, GPT, etc.) to:

- **Connect to running KeenEyes games** via named pipes or TCP
- **Query game state** - entities, components, systems, world statistics
- **Simulate input** - keyboard, mouse, and gamepad
- **Capture screenshots** - for visual analysis and debugging
- **Monitor performance** - FPS, frame times, per-system metrics

This is particularly useful for:
- Automated testing with AI-driven test agents
- Game debugging with natural language queries
- AI-assisted game development workflows
- Building AI game-playing agents

## Installation

### Prerequisites

- .NET 10 SDK
- A running KeenEyes game with TestBridge enabled

### Building from Source

```bash
cd tools/KeenEyes.Mcp.TestBridge
dotnet build
```

For a self-contained executable:

```bash
dotnet publish -c Release -r win-x64
```

## Configuration

### Claude Code / Claude Desktop

Add to your MCP configuration file (e.g., `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "keeneyes": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/KeenEyes.Mcp.TestBridge"],
      "env": {
        "KEENEYES_PIPE_NAME": "KeenEyes.TestBridge",
        "KEENEYES_TRANSPORT": "pipe"
      }
    }
  }
}
```

Or with a published executable:

```json
{
  "mcpServers": {
    "keeneyes": {
      "command": "path/to/KeenEyes.Mcp.TestBridge.exe",
      "args": ["--pipe", "KeenEyes.TestBridge"]
    }
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
**Server → game hop** (how the MCP server reaches the game):

| Variable | Description | Default |
|----------|-------------|---------|
| `KEENEYES_PIPE_NAME` | Named pipe name for IPC | `KeenEyes.TestBridge` |
| `KEENEYES_HOST` | TCP host for network connections | `127.0.0.1` |
| `KEENEYES_PORT` | TCP port for network connections | `19283` |
| `KEENEYES_TRANSPORT` | Transport mode: `pipe` or `tcp` | `pipe` |
| `KEENEYES_HEARTBEAT_INTERVAL` | Heartbeat interval in ms | `5000` |
| `KEENEYES_HEARTBEAT_TIMEOUT` | Heartbeat timeout in ms | `10000` |
| `KEENEYES_MAX_PING_FAILURES` | Max consecutive ping failures | `3` |
| `KEENEYES_TIMEOUT` | Connection timeout in ms | `30000` |

**Client → server hop** (how an MCP client such as Claude Code reaches this server):

| Variable | Description | Default |
|----------|-------------|---------|
| `KEENEYES_MCP_TRANSPORT` | Client transport: `stdio` or `http` | `stdio` |
| `KEENEYES_MCP_URL` | URL the HTTP transport binds to (only when `http`) | `http://127.0.0.1:19284/` |
| `KEENEYES_MCP_TOKEN` | Bearer token required by the HTTP endpoint; unset = unauthenticated | *(unset)* |

`KEENEYES_MCP_*` is orthogonal to the `KEENEYES_TRANSPORT`/pipe/tcp settings: the former chooses how a client reaches this server, the latter how this server reaches the game.

### Command-Line Arguments

| Argument | Description |
|----------|-------------|
| `--pipe <name>` | Named pipe name (server → game) |
| `--host <host>` | TCP host address (server → game) |
| `--port <port>` | TCP port number (server → game) |
| `--transport <mode>` | Server → game transport: `pipe` or `tcp` |
| `--timeout <ms>` | Connection timeout in milliseconds |
| `--mcp-transport <mode>` | Client → server transport: `stdio` or `http` |
| `--mcp-url <url>` | URL the HTTP transport binds to (only when `http`) |

## Remote TestBridge over HTTP

By default the server speaks MCP over **stdio**: Claude Code launches it as a local subprocess, and the standard local `.mcp.json` entry relies on this. To let Claude Code on **one machine** drive a game running on **another**, switch the client-facing transport to **HTTP**.

### Topology

```
Claude Code (host A) --HTTP/LAN--> KeenEyes.Mcp.TestBridge (host B, HTTP server)
                                        └-- loopback named-pipe IPC --> game (host B)
```

The powerful game-control channel (input injection, world mutation, memory read) stays on **loopback** on the game's machine. Only the MCP protocol — a first-class, auth-capable transport — crosses the network. This is cleaner and safer than exposing the raw TCP IPC channel across the network.

### Host B — game + MCP server

Run the game with the TestBridge IPC server on a **loopback named pipe** (no game code change; this is the default), then launch the MCP server bound to host B's LAN address with a bearer token:

```bash
# Bind to host B's LAN IP (e.g. 192.168.1.50); keep the game hop on the loopback pipe.
export KEENEYES_MCP_TRANSPORT=http
export KEENEYES_MCP_URL=http://192.168.1.50:19284/
export KEENEYES_MCP_TOKEN=$(openssl rand -hex 32)
export KEENEYES_TRANSPORT=pipe
export KEENEYES_PIPE_NAME=KeenEyes.TestBridge

./KeenEyes.Mcp.TestBridge         # or: dotnet run --project tools/KeenEyes.Mcp.TestBridge
echo "token: $KEENEYES_MCP_TOKEN"  # copy this to host A
```

### Host A — Claude Code

Add an `http` entry to `.mcp.json` pointing at host B, carrying the bearer token:

```json
{
  "mcpServers": {
    "keeneyes-remote": {
      "type": "http",
      "url": "http://192.168.1.50:19284/",
      "headers": {
        "Authorization": "Bearer <paste KEENEYES_MCP_TOKEN here>"
      }
    }
  }
}
```

### Security

An HTTP endpoint that controls a game process **must not be left open**.

- **Bearer token (required in practice):** set `KEENEYES_MCP_TOKEN`. The endpoint then requires `Authorization: Bearer <token>` and returns **401** for missing or wrong tokens. If the token is unset, the server logs a prominent warning and serves **unauthenticated** — only ever acceptable on loopback.
- **Bind narrowly:** point `KEENEYES_MCP_URL` at a specific loopback or LAN IP. **Never bind to `0.0.0.0`** or a public interface. The default (`http://127.0.0.1:19284/`) is loopback-only.
- **Trusted networks only:** restrict with a host firewall to host A's address; treat the endpoint as you would an SSH port.
- **Token vs mTLS:** a bearer token authenticates the client but leaves the channel in cleartext on plain HTTP — anyone who can read LAN traffic can capture the token. For hardened deployments, terminate TLS at a reverse proxy in front of the server (set `KEENEYES_MCP_URL` behind it) or use **mutual TLS (mTLS)**, which additionally authenticates the client with a certificate and encrypts the channel. mTLS is stronger but needs certificate provisioning on both hosts; the built-in bearer-token check is the minimum bar and is sufficient on a trusted LAN.

## Available Tools

### Game Tools

Tools for managing the connection to the game.

| Tool | Description |
|------|-------------|
| `game_connect` | Connect to a running KeenEyes game |
| `game_disconnect` | Disconnect from the game |
| `game_status` | Get detailed connection status |
| `game_get_screen_size` | Get game window dimensions |
| `game_wait_for_condition` | Wait for a game state condition |

**`game_connect`** parameters:
- `pipeName` (optional) - Named pipe name
- `host` (optional) - TCP host for network connection
- `port` (optional) - TCP port for network connection
- `transport` - Transport mode: `pipe` (default) or `tcp`

**`game_wait_for_condition`** parameters:
- `condition` - One of: `entity_exists`, `entity_gone`, `component_exists`, `component_gone`
- `timeoutMs` - Timeout in milliseconds (default: 5000)
- `entityId` or `entityName` - Entity to check
- `componentType` - Component type name (for component conditions)

### Input Tools

Tools for simulating keyboard, mouse, and gamepad input.

#### Keyboard

| Tool | Description |
|------|-------------|
| `input_key_press` | Press and release a key |
| `input_key_down` | Hold a key down |
| `input_key_up` | Release a held key |
| `input_type_text` | Type a string of text |
| `input_is_key_down` | Check if a key is pressed |

Common keys: `Space`, `Enter`, `Escape`, `Tab`, `Backspace`, `W`, `A`, `S`, `D`, `Up`, `Down`, `Left`, `Right`, `F1`-`F12`

Modifier keys: `Shift`, `Ctrl`, `Alt`, `Super` (comma-separated for combinations)

#### Mouse

| Tool | Description |
|------|-------------|
| `input_mouse_move` | Move mouse to absolute position |
| `input_mouse_move_relative` | Move mouse by relative delta |
| `input_mouse_click` | Click at a position |
| `input_mouse_double_click` | Double-click at a position |
| `input_mouse_down` | Press mouse button down |
| `input_mouse_up` | Release mouse button |
| `input_mouse_drag` | Drag from one position to another |
| `input_mouse_scroll` | Scroll the mouse wheel |
| `input_get_mouse_position` | Get current mouse position |

Mouse buttons: `Left`, `Right`, `Middle`, `Button4`, `Button5`

#### Gamepad

| Tool | Description |
|------|-------------|
| `input_gamepad_button_down` | Press gamepad button down |
| `input_gamepad_button_up` | Release gamepad button |
| `input_gamepad_button_press` | Press and release button |
| `input_gamepad_left_stick` | Set left stick position (-1 to 1) |
| `input_gamepad_right_stick` | Set right stick position (-1 to 1) |
| `input_gamepad_trigger` | Set trigger value (0 to 1) |
| `input_gamepad_connect` | Connect/disconnect virtual gamepad |

Gamepad buttons: `South`, `East`, `West`, `North`, `LeftShoulder`, `RightShoulder`, `Back`, `Start`, `Guide`, `LeftStick`, `RightStick`, `DPadUp`, `DPadDown`, `DPadLeft`, `DPadRight`

#### Input Actions

| Tool | Description |
|------|-------------|
| `input_trigger_action` | Trigger a named input action directly |
| `input_set_action_value` | Set axis-based action value |
| `input_set_action_vector2` | Set 2D axis action (movement) |
| `input_reset` | Reset all input to default state |

### State Tools

Tools for querying game state.

| Tool | Description |
|------|-------------|
| `state_get_entity_count` | Get total entity count |
| `state_query_entities` | Query entities with filters |
| `state_get_entity` | Get entity by ID |
| `state_get_entity_by_name` | Find entity by name |
| `state_get_component` | Get component data from entity |
| `state_get_children` | Get child entity IDs |
| `state_get_parent` | Get parent entity ID |
| `state_get_entities_with_tag` | Find entities with a tag |
| `state_get_world_stats` | Get world statistics |
| `state_get_systems` | List all registered systems |
| `state_get_performance` | Get performance metrics |

**`state_query_entities`** parameters:
- `withComponents` - Component types entities must have
- `withoutComponents` - Component types entities must NOT have
- `withTags` - Tags entities must have
- `namePattern` - Name pattern with wildcards (`*`, `?`)
- `parentId` - Filter by parent entity
- `maxResults` - Maximum results (default: 100)

### Capture Tools

Tools for screenshots and frame recording.

| Tool | Description |
|------|-------------|
| `capture_is_available` | Check if capture is available |
| `capture_screenshot` | Capture screenshot as base64 |
| `capture_screenshot_to_file` | Save screenshot to file |
| `capture_start_recording` | Start recording frames |
| `capture_stop_recording` | Stop recording |
| `capture_is_recording` | Check recording state |
| `capture_get_recorded_count` | Get recorded frame count |

Image formats: `png` (default), `jpeg`, `bmp`

### Mutation Tools

Tools for creating, modifying, and destroying entities at runtime.

| Tool | Description |
|------|-------------|
| `mutation_spawn` | Spawn a new empty entity |
| `mutation_spawn_with_components` | Spawn an entity with components |
| `mutation_despawn` | Destroy an entity |
| `mutation_clone` | Clone an existing entity |
| `mutation_set_name` / `mutation_clear_name` | Set or clear an entity's name |
| `mutation_set_parent` | Re-parent an entity |
| `mutation_get_root_entities` | List entities with no parent |
| `mutation_add_component` / `mutation_remove_component` / `mutation_set_component` | Add, remove, or replace a component |
| `mutation_set_field` | Set a single field on a component |
| `mutation_add_tag` / `mutation_remove_tag` / `mutation_get_all_tags` | Manage string tags |

### System Tools

Tools for inspecting and toggling registered systems.

| Tool | Description |
|------|-------------|
| `system_list` / `system_get_count` / `system_get` | List systems, count them, or get one |
| `system_enable` / `system_disable` / `system_toggle` | Change a system's enabled state |
| `system_get_by_phase` | List systems in a given phase |
| `system_get_enabled` / `system_get_disabled` | Filter systems by state |

### Time Tools

Tools for controlling simulation time.

| Tool | Description |
|------|-------------|
| `time_get_state` | Get the current time/pause/scale state |
| `time_pause` / `time_resume` / `time_toggle_pause` | Control pause state |
| `time_set_scale` | Set the time scale (slow-mo / fast-forward) |
| `time_step_frame` | Advance a single frame while paused |

### Log Tools

Tools for querying the in-game log ring buffer.

| Tool | Description |
|------|-------------|
| `log_get_stats` / `log_get_count` | Aggregate log statistics and counts |
| `log_get_recent` | Most recent entries |
| `log_get_errors` | Error-level entries |
| `log_get_by_level` / `log_get_by_category` | Filter by severity or category |
| `log_search` / `log_query` | Text search and structured queries |
| `log_clear` | Clear the buffer |

### Window Tools

Read-only tools for inspecting the game window.

| Tool | Description |
|------|-------------|
| `window_is_available` | Check if a window is present |
| `window_get_state` | Full window state |
| `window_get_size` / `window_get_aspect_ratio` | Dimensions |
| `window_get_title` | Window title |
| `window_is_closing` / `window_is_focused` | Window lifecycle/focus flags |

### AI Tools

Tools for inspecting and steering `KeenEyes.AI` agents (behavior trees, state machines, utility AI, blackboards).

| Tool | Description |
|------|-------------|
| `ai_get_statistics` | Aggregate AI agent statistics |
| `ai_behavior_tree_list` / `ai_behavior_tree_get` / `ai_behavior_tree_reset` | Inspect and reset behavior trees |
| `ai_state_machine_list` / `ai_state_machine_get` | Inspect FSM agents |
| `ai_state_machine_force_state` / `ai_state_machine_force_state_by_name` | Force an FSM into a state |
| `ai_utility_list` / `ai_utility_get` / `ai_utility_score_all` / `ai_utility_force_evaluation` | Inspect and drive utility AI scoring |
| `ai_blackboard_get` / `ai_blackboard_get_value` / `ai_blackboard_set_value` / `ai_blackboard_remove_value` / `ai_blackboard_clear` | Read and mutate blackboard values |

### Profiling & Memory Tools

Tools for `KeenEyes.Debugging` profilers (system/query timing, GC, memory, timeline). Most require debug mode enabled.

| Tool | Description |
|------|-------------|
| `profile_debug_mode_status` / `profile_debug_mode_enable` / `profile_debug_mode_disable` | Toggle debug/profiling mode |
| `profile_system_*` | System timing: availability, get, list, slowest, reset |
| `profile_query_*` | Query timing: get, list, slowest, cache stats, reset |
| `profile_gc_*` | GC profiling: get, list, hotspots, reset |
| `memory_available` / `memory_get_stats` / `memory_get_archetypes` | Memory and archetype statistics |
| `timeline_*` | Frame timeline: start, stop, status, get frame/recent, per-system stats, reset |

### Replay Tools

Tools for `KeenEyes.Replay` recording, playback, and determinism validation.

| Tool | Description |
|------|-------------|
| `replay_start_recording` / `replay_stop_recording` / `replay_cancel_recording` / `replay_is_recording` | Control recording |
| `replay_force_snapshot` | Force a keyframe snapshot |
| `replay_save` / `replay_load` / `replay_list` / `replay_delete` / `replay_get_metadata` | Manage saved replays |
| `replay_play` / `replay_pause` / `replay_stop` / `replay_get_playback_state` / `replay_set_speed` | Control playback |
| `replay_seek_frame` / `replay_seek_time` / `replay_step_forward` / `replay_step_backward` | Seek and step |
| `replay_get_frame` / `replay_get_frame_range` / `replay_get_inputs` / `replay_get_events` / `replay_get_snapshots` | Inspect recorded data |
| `replay_validate` / `replay_check_determinism` | Validate replays and check determinism |

### Snapshot Tools

Tools for saving, restoring, and diffing world snapshots (including quicksave/quickload).

| Tool | Description |
|------|-------------|
| `snapshot_create` / `snapshot_restore` / `snapshot_delete` / `snapshot_list` / `snapshot_get_info` | Manage in-memory snapshots |
| `snapshot_diff` / `snapshot_diff_current` | Diff two snapshots, or a snapshot against the live world |
| `snapshot_save_file` / `snapshot_load_file` | Persist snapshots to disk |
| `quicksave` / `quickload` | Convenience quicksave slot |
| `snapshot_export_json` / `snapshot_import_json` | JSON import/export |

## Available Resources

Resources provide read-only access to game state via URI templates.

### Connection

| URI | Description |
|-----|-------------|
| `keeneyes://connection/status` | Current connection status |

### World

| URI | Description |
|-----|-------------|
| `keeneyes://world/stats` | World statistics (entity count, memory, etc.) |
| `keeneyes://world/systems` | Registered systems list |
| `keeneyes://world/performance` | Performance metrics |

### Entity

| URI | Description |
|-----|-------------|
| `keeneyes://entity/{id}` | Entity data by ID |
| `keeneyes://entity/name/{name}` | Entity data by name |
| `keeneyes://entity/{id}/component/{type}` | Component data |

### Extension

| URI | Description |
|-----|-------------|
| `keeneyes://extension/{typeName}` | World extension/singleton data |

### Capture

| URI | Description |
|-----|-------------|
| `keeneyes://capture/screenshot` | Current screenshot (image/png) |

## Available Prompts

Pre-built prompts for common workflows.

| Prompt | Description |
|--------|-------------|
| `connect_and_explore` | Connect and explore game entities |
| `test_input_sequence` | Guide through testing input |
| `capture_and_describe` | Capture screenshot and describe scene |
| `monitor_entity` | Watch entity state changes |

## Example Workflows

### Connecting and Exploring

```
User: Help me explore the game state

AI: I'll connect to the game and explore what entities exist.

1. First, let me connect:
   [Uses game_connect tool]

2. Now let me check how many entities exist:
   [Uses state_get_entity_count tool]
   → Found 42 entities

3. Let me query the entities to see what's in the world:
   [Uses state_query_entities with maxResults=10]

4. Here's what I found:
   - Player entity (id: 1) with Position, Velocity, Health components
   - 5 Enemy entities with AI, Position, Health
   - 10 Projectile entities
   - UI entities for HUD elements
```

### Testing Input

```
User: Test pressing the jump button

AI: I'll help you test the jump input.

1. Let me capture the initial state:
   [Uses capture_screenshot]
   → Player is on the ground

2. Now I'll press the Space key (jump):
   [Uses input_key_press with key="Space"]

3. Wait for the jump to complete:
   [Uses game_wait_for_condition with condition="component_exists",
    entityName="Player", componentType="Jumping", timeoutMs=1000]

4. Capture the result:
   [Uses capture_screenshot]
   → Player is now airborne

The jump input is working correctly!
```

### Monitoring an Entity

```
User: Watch what happens to the player's health when they take damage

AI: I'll monitor the Player entity's Health component.

1. Get initial health state:
   [Uses state_get_entity_by_name with name="Player"]
   [Uses state_get_component with entityId=1, componentType="Health"]
   → Current: 100, Max: 100

2. Simulate taking damage by triggering the "TakeDamage" action:
   [Uses input_trigger_action with actionName="DebugTakeDamage"]

3. Check health again:
   [Uses state_get_component with entityId=1, componentType="Health"]
   → Current: 90, Max: 100

The health decreased by 10 points as expected.
```

## Troubleshooting

### Cannot Connect to Game

**Symptom:** `game_connect` returns "Failed to connect"

**Solutions:**
1. Ensure the game is running with TestBridge enabled
2. Check the pipe name matches (default: `KeenEyes.TestBridge`)
3. For TCP connections, verify the host and port are correct
4. Check firewall settings for TCP mode

### Capture Not Available

**Symptom:** `capture_is_available` returns `false`

**Solutions:**
1. The game may be running in headless mode
2. Graphics backend may not support capture
3. Check game logs for capture initialization errors

### Input Not Working

**Symptom:** Input tools succeed but game doesn't respond

**Solutions:**
1. Ensure the game window has focus
2. Check if input is blocked by UI or pause menu
3. Use `input_reset` to clear any stuck input state
4. Verify the key/button names are correct

### Connection Dropped

**Symptom:** Tools fail with "Not connected"

**Solutions:**
1. The game may have closed or crashed
2. Check heartbeat settings if connection times out too quickly
3. Use `game_status` to check connection health
4. Reconnect with `game_connect`

## Game Setup

To enable TestBridge in your KeenEyes game, add the TestBridge plugin:

```csharp
using KeenEyes.TestBridge;

// In your game initialization
var world = new World();
world.InstallPlugin(new TestBridgePlugin(new TestBridgeOptions
{
    PipeName = "KeenEyes.TestBridge",
    EnableCapture = true,
    EnableInput = true
}));
```

See [TestBridge Architecture Guide](testbridge.md) for detailed setup instructions.

## Related Documentation

- [TestBridge Architecture Guide](testbridge.md) - Complete architecture, IPC protocol, command reference
- [Testing Guide](testing.md) - Unit testing with mocks
- [ECS Fundamentals](getting-started.md) - Understanding entities and components
- [Plugin Development](plugins.md) - Creating custom game plugins