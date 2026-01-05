# Research Reports

This section contains technical research reports for planned features and architectural decisions in KeenEyes ECS.

## Purpose

Research reports document:
- Evaluation of third-party libraries and approaches
- Architecture recommendations with trade-off analysis
- Implementation strategies and phases
- Integration patterns with existing KeenEyes infrastructure

## Reports

### Planned Systems Architecture

| Topic | Status | Purpose |
|-------|--------|---------|
| [Graphics & Input Abstraction](graphics-input-abstraction.md) | Research Complete | Backend-agnostic graphics and input layer |
| [UI System](ui-system.md) | Research Complete | Retained-mode UI with ECS entities |
| [Audio System](audio-system.md) | Research Complete | OpenAL-based audio with 3D positioning |
| [Particle System](particle-system.md) | Research Complete | High-performance particle rendering |
| [Asset Management](asset-management.md) | Research Complete | Reference-counted asset loading |
| [Animation System](animation-system.md) | Research Complete | Sprite, skeletal, and property animation |
| [AI System](ai-system.md) | Research Complete | FSM, behavior trees, utility AI |
| [Engine Systems Roadmap](engine-systems-roadmap.md) | Research Complete | Pathfinding, Scene Management, Localization, Networking |

### Editor & Tooling

| Topic | Status | Purpose |
|-------|--------|---------|
| [Scene Editor Architecture](scene-editor-architecture.md) | Research Complete | Complete Unity/Godot-class editor design |
| [Framework Editor](framework-editor.md) | Research Complete | Editor feasibility and plugin designs |
| [.NET Debugger Integration](dotnet-debugger-integration.md) | Research Complete | SharpDbg and DAP integration for editor debugging |

### Foundation Research

| Topic | Status | Purpose |
|-------|--------|---------|
| [Networking](networking.md) | Phase 16.4 | Multiplayer synchronization patterns |
| [Audio Systems](audio-systems.md) | Research Complete | Cross-platform audio library evaluation |
| [Asset Loading](asset-loading.md) | Research Complete | Asset pipeline and loading strategies |
| [Cross-Platform Deployment](cross-platform-deployment.md) | Research Complete | Platform targeting and distribution |
| [Game Math Libraries](game-math-libraries.md) | Research Complete | Math library evaluation |
| [OpenGL C# Bindings](opengl-csharp-bindings.md) | Research Complete | Graphics API bindings |
| [Shader Management](shader-management.md) | Research Complete | Shader compilation and management |
| [Shader Language (KESL)](shader-language.md) | Research Complete | Custom shader language design |
| [Windowing & Input](windowing-input.md) | Research Complete | Window management and input handling |

## Contributing

When adding new research:
1. Create a markdown file in `docs/research/`
2. Follow the existing format (Executive Summary, Comparison, Recommendations)
3. Add the report to `docs/research/toc.yml`
4. Update this index page
