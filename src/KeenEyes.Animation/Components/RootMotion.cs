using System.Numerics;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Defines how extracted root motion is delivered.
/// </summary>
public enum RootMotionMode
{
    /// <summary>
    /// The extracted delta is applied directly to the skeleton root entity's Transform3D.
    /// </summary>
    ApplyToEntity,

    /// <summary>
    /// The extracted delta is written to <see cref="RootMotion.DeltaPosition"/> and
    /// <see cref="RootMotion.DeltaRotation"/> without touching the entity's Transform3D,
    /// so a character controller or physics system can consume it.
    /// </summary>
    ExposeDelta
}

/// <summary>
/// Component that enables root motion extraction for a skeleton root entity.
/// </summary>
/// <remarks>
/// <para>
/// Root motion extracts the per-frame movement of a designated root bone from the
/// playing animation clip(s) and delivers it as an entity-space delta, so animations
/// like walking or turning drive the character through the world instead of playing
/// in place. The root bone's animated local translation (and rotation, when
/// <see cref="ApplyRotation"/> is set) is suppressed so the motion is not applied twice.
/// </para>
/// <para>
/// Attach this component to the same entity that carries the
/// <see cref="AnimationPlayer"/> or <see cref="Animator"/>. Requires
/// <see cref="AnimationConfig.EnableRootMotion"/> to be set when installing the
/// <see cref="AnimationPlugin"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var character = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(AnimationPlayer.ForClip(walkClipId))
///     .With(RootMotion.ForBone("Root"))
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct RootMotion
{
    /// <summary>
    /// Whether root motion extraction is active for this entity.
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// The name of the bone to extract root motion from (typically "Root" or "Hips").
    /// </summary>
    public string RootBoneName;

    /// <summary>
    /// How the extracted delta is delivered.
    /// </summary>
    public RootMotionMode Mode;

    /// <summary>
    /// Whether to extract and deliver position deltas.
    /// </summary>
    public bool ApplyPosition;

    /// <summary>
    /// Whether to extract and deliver rotation deltas.
    /// </summary>
    public bool ApplyRotation;

    /// <summary>
    /// Whether to restrict the delivered position delta to the XZ plane.
    /// </summary>
    /// <remarks>
    /// When set, the entity-space Y component of the delta is suppressed and the root
    /// bone keeps its animated Y translation, so vertical motion (crouching, bobbing)
    /// stays in the skeleton while horizontal motion drives the entity.
    /// </remarks>
    public bool PlanarOnly;

    /// <summary>
    /// Multiplier for the extracted position delta (e.g. 1.5 for faster movement).
    /// </summary>
    public float PositionScale;

    /// <summary>
    /// Multiplier for the extracted rotation delta (e.g. 0.5 for slower turning).
    /// </summary>
    public float RotationScale;

    /// <summary>
    /// The position delta extracted this frame, in the skeleton root entity's parent space.
    /// </summary>
    /// <remarks>
    /// Written by the root motion system every frame in both modes. In
    /// <see cref="RootMotionMode.ExposeDelta"/> mode this is the value a character
    /// controller should consume.
    /// </remarks>
    [BuilderIgnore]
    public Vector3 DeltaPosition;

    /// <summary>
    /// The rotation delta extracted this frame.
    /// </summary>
    /// <remarks>
    /// Written by the root motion system every frame in both modes.
    /// </remarks>
    [BuilderIgnore]
    public Quaternion DeltaRotation;

    /// <summary>
    /// Creates a default root motion configuration.
    /// </summary>
    public static RootMotion Default => new()
    {
        Enabled = true,
        RootBoneName = "Root",
        Mode = RootMotionMode.ApplyToEntity,
        ApplyPosition = true,
        ApplyRotation = true,
        PlanarOnly = false,
        PositionScale = 1f,
        RotationScale = 1f,
        DeltaPosition = Vector3.Zero,
        DeltaRotation = Quaternion.Identity
    };

    /// <summary>
    /// Creates a root motion configuration for the specified root bone.
    /// </summary>
    /// <param name="rootBoneName">The name of the root bone.</param>
    /// <returns>A configured root motion component.</returns>
    public static RootMotion ForBone(string rootBoneName) => Default with
    {
        RootBoneName = rootBoneName
    };
}
