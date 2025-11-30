# KeenEye

A high-performance Entity Component System (ECS) framework for .NET 10.

This is a C# reimplementation of [OrionECS](https://github.com/tyevco/OrionECS), designed to leverage modern .NET features for maximum performance and developer experience.

## Features

- High-performance archetype-based ECS architecture
- Modern C# 13+ features and syntax
- Zero/minimal allocations in hot paths
- Flexible query system
- Component pools and object recycling
- Plugin architecture for extensibility

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
└── KeenEye.sln            # Solution file
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
