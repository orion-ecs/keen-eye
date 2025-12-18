# ADR-006: Custom MSBuild SDK for KeenEyes Projects

**Status:** Accepted
**Date:** 2025-12-18

## Context

External consumers of KeenEyes must currently configure their projects with significant boilerplate:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="KeenEyes.Core" Version="0.1.0" />
    <PackageReference Include="KeenEyes.Generators" Version="0.1.0"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

This creates several problems:

1. **Error-prone setup** - Forgetting `OutputItemType="Analyzer"` on generators causes silent failures
2. **Version coupling** - Core and Generators versions must match but are specified separately
3. **Convention drift** - Users may not enable nullable, AOT compatibility, or C# 13 features
4. **No tooling hooks** - No standard way to detect KeenEyes projects or their configuration
5. **Editor integration blocked** - A future visual editor needs to identify and introspect KeenEyes projects

## Decision

Create custom MSBuild SDK packages that encapsulate KeenEyes conventions and dependencies:

### SDK Flavors

| Package | Target Use Case | Default Dependencies |
|---------|-----------------|---------------------|
| `KeenEyes.Sdk` | Games and applications | Core + Generators |
| `KeenEyes.Sdk.Plugin` | Plugin libraries | Abstractions + Generators |
| `KeenEyes.Sdk.Library` | Reusable ECS libraries | Core + Generators (private) |

### Minimal Project File

With the SDK, a game project becomes:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
</Project>
```

The SDK automatically:
- Imports `Microsoft.NET.Sdk` as the base
- Sets `TargetFramework=net10.0`, `LangVersion=preview`, `Nullable=enable`
- Sets `OutputType=Exe` (for main SDK) or `Library` (for Plugin/Library SDKs)
- Enables `IsAotCompatible=true`
- References appropriate KeenEyes packages with correct attributes
- Defines custom ItemGroup types for editor integration

### Custom ItemGroups

The SDK defines item types for future editor and build pipeline integration:

```xml
<!-- Scene definitions -->
<ItemGroup>
  <KeenEyesScene Include="Scenes/**/*.kescene" />
</ItemGroup>

<!-- Prefab templates -->
<ItemGroup>
  <KeenEyesPrefab Include="Prefabs/**/*.keprefab" />
</ItemGroup>

<!-- Game assets (auto-copied to output) -->
<ItemGroup>
  <KeenEyesAsset Include="Assets/**/*" />
</ItemGroup>

<!-- World configuration -->
<ItemGroup>
  <KeenEyesWorld Include="Worlds/**/*.keworld" />
</ItemGroup>
```

### Project Metadata

The SDK generates `keeneyes.project.json` in the output directory:

```json
{
  "projectName": "MyGame",
  "projectType": "Game",
  "sdkVersion": "0.1.0",
  "coreVersion": "0.1.0",
  "targetFramework": "net10.0",
  "isAotCompatible": true,
  "features": ["ECS", "SourceGenerators", "AOT"]
}
```

This enables:
- Editor project detection without loading the full MSBuild graph
- Version compatibility checking
- Upgrade path recommendations
- Feature flag queries

### Version Properties

The SDK exposes version metadata for tooling:

| Property | Description |
|----------|-------------|
| `$(KeenEyesSdkVersion)` | SDK package version |
| `$(KeenEyesCoreVersion)` | KeenEyes.Core version included |
| `$(KeenEyesMinimumCoreVersion)` | Minimum compatible Core version |
| `$(KeenEyesProjectType)` | Project type (Game, Plugin, Library) |
| `$(IsKeenEyesProject)` | Always `true` for SDK projects |

## Alternatives Considered

### Option 1: NuGet Meta-Package Only

Create a single `KeenEyes` package that references Core and Generators:

```xml
<PackageReference Include="KeenEyes" Version="0.1.0" />
```

**Rejected because:**
- Cannot set project properties (TargetFramework, LangVersion, etc.)
- Cannot define custom ItemGroup types
- No project metadata generation
- Generators still need special attributes that meta-packages can't enforce

### Option 2: dotnet new Templates Only

Rely solely on `dotnet new keeneyes-game` templates:

**Rejected because:**
- Templates are point-in-time snapshots; don't receive updates
- No version tracking or upgrade tooling
- Custom ItemGroups would need manual documentation
- Editor integration still needs project detection

### Option 3: MSBuild Props/Targets Package

Create a package with `.props` and `.targets` files instead of an SDK:

```xml
<PackageReference Include="KeenEyes.Build" Version="0.1.0" />
```

**Rejected because:**
- Users still need to reference Core, Generators separately
- Props/targets are added to existing SDK, not replacements
- Cannot override TFM or LangVersion defaults cleanly
- Less elegant than `Sdk="..."` attribute

## Consequences

### Positive

- **Minimal boilerplate** - New projects need only the SDK reference
- **Convention enforcement** - C# 13, nullable, AOT enabled by default
- **Version coupling** - SDK version implies compatible package versions
- **Editor foundation** - Project detection, metadata, custom item types ready
- **Upgrade tooling** - Version properties enable compatibility checking
- **Asset pipeline ready** - Custom ItemGroups for future build-time processing

### Negative

- **SDK resolution complexity** - Requires NuGet SDK resolution (not just packages)
- **Version lock-in** - SDK version determines all package versions
- **Additional packages** - Three more packages to publish and maintain
- **Learning curve** - Users must understand SDK vs regular packages

### Neutral

- **Opt-in architecture** - Users can still use regular `Microsoft.NET.Sdk` with explicit references
- **Internal usage optional** - Monorepo samples continue using ProjectReferences

## Implementation Notes

### Package Structure

```
KeenEyes.Sdk/
├── Sdk/
│   ├── Sdk.props    # Imported first (sets defaults)
│   └── Sdk.targets  # Imported last (adds references, targets)
├── README.md
└── KeenEyes.Sdk.csproj
```

### SDK Props/Targets Flow

1. **Sdk.props** runs before user's PropertyGroups
   - Sets conditional defaults (only if not already set)
   - Imports `Microsoft.NET.Sdk.props`
   - Defines custom ItemDefinitionGroups

2. **User's .csproj content** runs
   - Can override any SDK defaults
   - Can add custom ItemGroups

3. **Sdk.targets** runs after user's content
   - Imports `Microsoft.NET.Sdk.targets`
   - Adds PackageReferences based on Include properties
   - Defines build targets for asset processing
   - Generates project metadata JSON

### Opting Out

Users can disable automatic references:

```xml
<PropertyGroup>
  <IncludeKeenEyesCore>false</IncludeKeenEyesCore>
  <IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>
</PropertyGroup>
```

## Future Considerations

1. **Editor integration** - Read `keeneyes.project.json` for project discovery
2. **Asset processing** - Build targets for scene/prefab compilation
3. **Analyzers** - Include KeenEyes-specific Roslyn analyzers
4. **Upgrade CLI** - `dotnet keeneyes upgrade` command using version metadata
