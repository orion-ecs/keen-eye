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

### Approach 2: In-Process (Advanced)

Embed SharpDbg.Infrastructure directly for tighter integration.

**Advantages:**
- Lower latency
- Direct access to debugger internals
- Custom debugging features beyond DAP

**Disadvantages:**
- Tighter coupling
- Debugger issues could crash editor
- More complex implementation

**When to Use:**
- Custom debugging visualizations
- Game-specific debugging (entity inspection, component views)
- Performance-critical scenarios

### Approach 3: Hybrid

Use DAP for standard debugging, but add custom channels for editor-specific features.

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

### Phase 1: Basic DAP Client

1. Add `Microsoft.VisualStudio.Shared.VSCodeDebugProtocol` package
2. Create `DebugAdapterManager` in editor infrastructure
3. Implement launch/attach workflow
4. Handle stopped events and update UI
5. Support basic breakpoints (line-based)

### Phase 2: UI Components

1. Debug toolbar (continue, step over, step in, step out, pause)
2. Breakpoint gutter markers
3. Call stack panel
4. Variables panel (locals, watch)
5. Debug console for output

### Phase 3: ECS-Specific Features

1. Entity inspector during debugging
2. Component value visualization
3. System execution profiling
4. World state snapshots
5. Query result inspection

### Phase 4: Advanced Features

1. Conditional breakpoints
2. Logpoints
3. Exception breakpoints
4. Hot reload integration
5. Edit and continue (if supported)

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
