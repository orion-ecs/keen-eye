using System.Numerics;

namespace KeenEyes.Sample.Graphics;

/// <summary>
/// Component that makes an entity spin continuously.
/// </summary>
[Component]
public partial struct Spin
{
    /// <summary>
    /// Rotation speed in radians per second around each axis.
    /// </summary>
    public Vector3 Speed;
}

/// <summary>
/// Component that makes an entity bob up and down.
/// </summary>
[Component]
public partial struct Bob
{
    /// <summary>
    /// The amplitude of the bobbing motion.
    /// </summary>
    public float Amplitude;

    /// <summary>
    /// The frequency of the bobbing motion in cycles per second.
    /// </summary>
    public float Frequency;

    /// <summary>
    /// The current phase of the bobbing motion.
    /// </summary>
    public float Phase;

    /// <summary>
    /// The original Y position to bob around.
    /// </summary>
    public float OriginY;
}

/// <summary>
/// Tag component to identify the ground plane.
/// </summary>
[TagComponent]
public partial struct GroundTag;
