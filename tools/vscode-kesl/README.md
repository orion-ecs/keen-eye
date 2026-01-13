# KESL - KeenEyes Shader Language

Syntax highlighting support for KeenEyes Shader Language (KESL) files in Visual Studio Code.

## Features

- Syntax highlighting for `.kesl` files
- Support for all KESL constructs:
  - Shader types: `compute`, `vertex`, `fragment`, `geometry`
  - Pipeline declarations
  - Component definitions
  - Built-in types: `float`, `float2`, `float3`, `float4`, `int`, `mat4`, etc.
  - Texture and sampler types
  - Query blocks with access modifiers (`read`, `write`, `optional`, `without`)
  - Geometry shader topologies
  - Built-in functions (math, vector operations, texture sampling)
- Comment support (line and block comments)
- Bracket matching and auto-closing

## Installation

### From VSIX (Recommended)

1. Package the extension: `vsce package`
2. Install in VS Code: `code --install-extension kesl-language-0.1.0.vsix`

### Development Installation

1. Copy this folder to your VS Code extensions directory:
   - Windows: `%USERPROFILE%\.vscode\extensions\kesl-language-0.1.0`
   - macOS/Linux: `~/.vscode/extensions/kesl-language-0.1.0`
2. Restart VS Code

## Example

```kesl
// Component definition
component Position {
    x: float
    y: float
    z: float
}

// Vertex shader
vertex TransformVertex {
    in {
        position: float3 @ 0
        normal: float3 @ 1
    }

    out {
        worldPos: float3
        worldNormal: float3
    }

    params {
        model: mat4
        view: mat4
        projection: mat4
    }

    execute() {
        worldPos = position;
        worldNormal = normal;
    }
}

// Pipeline composition
pipeline DeferredLit {
    vertex: TransformVertex
    fragment: LitSurface
}
```

## License

MIT
