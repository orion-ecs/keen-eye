# KeenEyes

[![Build and Test](https://github.com/orion-ecs/keen-eye/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/orion-ecs/keen-eye/actions/workflows/build.yml)
[![AOT Compatible](https://github.com/orion-ecs/keen-eye/actions/workflows/aot-compatibility.yml/badge.svg)](https://github.com/orion-ecs/keen-eye/actions/workflows/aot-compatibility.yml)
[![Coverage Status](https://coveralls.io/repos/github/orion-ecs/keen-eye/badge.svg)](https://coveralls.io/github/orion-ecs/keen-eye)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-Compatible-brightgreen)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/orion-ecs/keen-eye)](https://github.com/orion-ecs/keen-eye/issues)
[![GitHub stars](https://img.shields.io/github/stars/orion-ecs/keen-eye)](https://github.com/orion-ecs/keen-eye/stargazers)

A high-performance Entity Component System (ECS) framework for .NET 10.

This is a C# reimplementation of [OrionECS](https://github.com/tyevco/OrionECS), designed to leverage modern .NET features for maximum performance and developer experience.

## Features

- High-performance archetype-based ECS architecture
- Modern C# 13+ features and syntax
- Zero/minimal allocations in hot paths
- Flexible query system
- Component pools and object recycling
- Plugin architecture for extensibility
- **Native AOT compatible** - deploy as self-contained native executables with no JIT ([docs](docs/aot-deployment.md))

## Requirements

- .NET 10 SDK (preview)

## Project Structure

```
keen-eye/
├── src/                    # Source projects
├── tests/                  # Unit and integration tests
├── samples/                # Example projects
├── benchmarks/             # Performance benchmarks
├── docs/                   # Documentation
├── Directory.Build.props   # Shared MSBuild properties
├── Directory.Packages.props # Centralized NuGet package versions
└── KeenEyes.sln            # Solution file
```

## Getting Started

### Prerequisites

Install the .NET 10 SDK:

```bash
# Check your current .NET version
dotnet --version

# The SDK version is locked via global.json
```

### Building

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Development

This project uses:
- **Central Package Management** - Package versions defined in `Directory.Packages.props`
- **EditorConfig** - Code style enforcement via `.editorconfig`
- **Nullable Reference Types** - Enabled by default
- **Treat Warnings as Errors** - Enabled for clean builds
- **Git Hooks** - Pre-commit and pre-push validations

#### Setting Up Git Hooks

Install the git hooks to validate code before commits and pushes:

```bash
# Linux/macOS
./scripts/install-hooks.sh

# Windows (PowerShell)
.\scripts\install-hooks.ps1
```

This enables:
- **Pre-commit**: Validates code formatting (`dotnet format`)
- **Pre-push**: Runs build and tests

To skip hooks temporarily (not recommended):
```bash
git commit --no-verify
git push --no-verify
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Original [OrionECS](https://github.com/tyevco/OrionECS) TypeScript implementation
