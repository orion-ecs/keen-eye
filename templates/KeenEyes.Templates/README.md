# KeenEyes ECS Templates

Project templates for the [KeenEyes ECS](https://github.com/orion-ecs/keen-eye) framework.

## Installation

```bash
dotnet new install KeenEyes.Templates
```

## Available Templates

| Template | Short Name | Description |
|----------|------------|-------------|
| KeenEyes Game | `keeneyes-game` | Console application for building games with KeenEyes ECS |
| KeenEyes Plugin | `keeneyes-plugin` | Class library for creating KeenEyes plugins |

## Usage

### Create a Game Project

```bash
dotnet new keeneyes-game -n MyGame
cd MyGame
dotnet run
```

### Create a Plugin Project

```bash
dotnet new keeneyes-plugin -n MyPlugin
cd MyPlugin
dotnet build
```

#### Plugin Options

| Option | Description | Default |
|--------|-------------|---------|
| `--IncludeCommon` | Include reference to KeenEyes.Common for shared components | `false` |

```bash
# Include KeenEyes.Common for shared components like Transform2D, Velocity2D
dotnet new keeneyes-plugin -n MyPlugin --IncludeCommon
```

## Template Contents

### Game Template (`keeneyes-game`)

- `Program.cs` - Entry point with game loop
- `Components.cs` - Example component definitions
- `Systems.cs` - Example system implementations

### Plugin Template (`keeneyes-plugin`)

- `Plugin.cs` - Plugin class implementing `IWorldPlugin`
- `Components.cs` - Example plugin components
- `Systems.cs` - Example plugin systems
- `CommonComponents.cs` - Systems using KeenEyes.Common (only with `--IncludeCommon`)

## Uninstallation

```bash
dotnet new uninstall KeenEyes.Templates
```

## Learn More

- [KeenEyes Documentation](https://github.com/orion-ecs/keen-eye)
- [ECS Architecture Overview](https://github.com/orion-ecs/keen-eye/blob/main/CLAUDE.md)
