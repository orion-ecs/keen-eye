using System.Numerics;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Full 3D transform: the dominant per-entity component in editor scenes.
/// </summary>
public struct EditorTransform : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}

/// <summary>
/// Linear velocity, consumed by the representative scene-simulation systems.
/// </summary>
public struct Velocity : IComponent
{
    public Vector3 Linear;
}

/// <summary>
/// Health, a small custom gameplay component with two integer fields.
/// </summary>
public struct Health : IComponent
{
    public int Current;
    public int Max;
}

/// <summary>
/// Render metadata: a custom component exercising string and enum inspection.
/// </summary>
public struct RenderMeta : IComponent
{
    public string MaterialName;
    public int LayerId;
    public bool Visible;
    public RenderQueue Queue;
}

/// <summary>
/// Spawn descriptor: a custom component with float and int payload.
/// </summary>
public struct SpawnInfo : IComponent
{
    public int Seed;
    public float Weight;
}

/// <summary>
/// Render ordering bucket, used to exercise enum field inspection/serialization.
/// </summary>
public enum RenderQueue
{
    Background,
    Geometry,
    Transparent,
    Overlay
}

/// <summary>
/// Tag marking entities that are static (non-moving) in the scene.
/// </summary>
public struct StaticTag : ITagComponent;

/// <summary>
/// Tag marking entities that can be selected in the editor.
/// </summary>
public struct SelectableTag : ITagComponent;
