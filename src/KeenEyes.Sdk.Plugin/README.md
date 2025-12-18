# KeenEyes.Sdk.Plugin

MSBuild SDK for KeenEyes plugin development.

## Quick Start

```xml
<Project Sdk="KeenEyes.Sdk.Plugin/0.1.0">

  <PropertyGroup>
    <Description>My awesome KeenEyes plugin</Description>
  </PropertyGroup>

</Project>
```

## What's Included

Unlike the main `KeenEyes.Sdk`, the Plugin SDK:
- References `KeenEyes.Abstractions` (not Core)
- Defaults to `OutputType=Library`
- Is designed for distributable plugin packages

| Feature | Default | Override |
|---------|---------|----------|
| Target Framework | `net10.0` | Set `<TargetFramework>` |
| Output Type | `Library` | Set `<OutputType>` |
| KeenEyes.Abstractions | Included | Set `<IncludeKeenEyesAbstractions>false</IncludeKeenEyesAbstractions>` |
| KeenEyes.Common | Not included | Set `<IncludeKeenEyesCommon>true</IncludeKeenEyesCommon>` |
| KeenEyes.Generators | Included | Set `<IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>` |

## Including Common Components

To use standard components from KeenEyes.Common:

```xml
<PropertyGroup>
  <IncludeKeenEyesCommon>true</IncludeKeenEyesCommon>
</PropertyGroup>
```

## Plugin vs Game SDK

| Aspect | KeenEyes.Sdk | KeenEyes.Sdk.Plugin |
|--------|--------------|---------------------|
| Default OutputType | Exe | Library |
| Core Reference | Yes | No |
| Abstractions Reference | Via Core | Direct |
| Use Case | Games | Plugins |
