# KeenEyes.Sdk

MSBuild SDK for KeenEyes ECS game development.

## Quick Start

Replace your standard SDK reference with KeenEyes.Sdk:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

</Project>
```

That's it! The SDK automatically:
- Sets `TargetFramework` to `net10.0`
- Enables `LangVersion=preview` for C# 13 features
- Enables nullable reference types
- References `KeenEyes.Core` and `KeenEyes.Generators`
- Configures AOT compatibility

## What's Included

| Feature | Default | Override |
|---------|---------|----------|
| Target Framework | `net10.0` | Set `<TargetFramework>` |
| Language Version | `preview` | Set `<LangVersion>` |
| Nullable | `enable` | Set `<Nullable>` |
| Output Type | `Exe` | Set `<OutputType>` |
| AOT Compatible | `true` | Set `<IsAotCompatible>` |
| KeenEyes.Core | Included | Set `<IncludeKeenEyesCore>false</IncludeKeenEyesCore>` |
| KeenEyes.Generators | Included | Set `<IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>` |

## Custom Item Types

The SDK defines custom item types for KeenEyes editor integration:

### Scenes (`.kescene`)

```xml
<ItemGroup>
  <KeenEyesScene Include="Scenes\**\*.kescene" />
</ItemGroup>
```

### Prefabs (`.keprefab`)

```xml
<ItemGroup>
  <KeenEyesPrefab Include="Prefabs\**\*.keprefab" />
</ItemGroup>
```

### Assets

```xml
<ItemGroup>
  <KeenEyesAsset Include="Assets\**\*" />
</ItemGroup>
```

Assets are automatically copied to the output directory.

### World Configuration (`.keworld`)

```xml
<ItemGroup>
  <KeenEyesWorld Include="Worlds\**\*.keworld" />
</ItemGroup>
```

## Version Properties

The SDK exposes version information for tooling:

| Property | Description |
|----------|-------------|
| `$(KeenEyesSdkVersion)` | SDK package version |
| `$(KeenEyesCoreVersion)` | KeenEyes.Core version |
| `$(KeenEyesMinimumCoreVersion)` | Minimum compatible Core version |
| `$(KeenEyesProjectType)` | Project type (`Game`, `Plugin`, `Library`) |
| `$(KeenEyesFeatures)` | Enabled features (`;` separated) |

## Generated Files

During build, the SDK generates:

- `obj/keeneyes.version.json` - Version compatibility info
- `bin/keeneyes.project.json` - Project metadata for editor

## SDK Flavors

| SDK | Use Case |
|-----|----------|
| `KeenEyes.Sdk` | Games and applications |
| `KeenEyes.Sdk.Plugin` | Plugin libraries |
| `KeenEyes.Sdk.Library` | ECS component libraries |

## Opting Out of Automatic References

To exclude automatic package references:

```xml
<PropertyGroup>
  <IncludeKeenEyesCore>false</IncludeKeenEyesCore>
  <IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>
</PropertyGroup>
```

## Requirements

- .NET 10.0 SDK or later
- NuGet 6.0 or later (for SDK resolution)
