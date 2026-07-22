namespace KeenEyes.Animation;

/// <summary>
/// Configuration for the animation plugin.
/// </summary>
public sealed class AnimationConfig
{
    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static AnimationConfig Default => new();

    /// <summary>
    /// Gets or sets whether to enable animation event dispatch.
    /// </summary>
    /// <remarks>
    /// Animation events can be fired at specific keyframe times.
    /// Disabling this reduces overhead if events are not used.
    /// </remarks>
    public bool EnableEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of simultaneous active tweens per entity.
    /// </summary>
    /// <remarks>
    /// This limit helps prevent runaway tween creation.
    /// </remarks>
    public int MaxTweensPerEntity { get; set; } = 16;

    /// <summary>
    /// Gets or sets whether inverse kinematics support is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the plugin registers the IK components (<see cref="Components.IKRig"/>,
    /// <see cref="Components.IKChainReference"/>, <see cref="Components.IKTarget"/>,
    /// <see cref="Components.IKConstraint"/>, <see cref="Components.LookAtTarget"/>),
    /// exposes an <see cref="IKManager"/> world extension preloaded with the TwoBone and
    /// FABRIK solvers, and adds the IK pipeline systems:
    /// <see cref="Systems.IKSolverSystem"/> at order 57 so IK chains are solved after
    /// <see cref="Systems.SkeletonPoseSystem"/> writes the FK pose, followed by
    /// <see cref="Systems.LookAtSystem"/> at order 58 so look-at/aim constraints are
    /// layered on top of the solved chains.
    /// </para>
    /// <para>
    /// Disabled by default: worlds that do not use IK pay no per-frame cost.
    /// </para>
    /// </remarks>
    public bool EnableIK { get; set; }

    /// <summary>
    /// Gets or sets whether root motion support is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the plugin registers the <see cref="Components.RootMotion"/> component
    /// and adds <see cref="Systems.RootMotionSystem"/> at order 56, directly after
    /// <see cref="Systems.SkeletonPoseSystem"/> (order 55). The system extracts the root
    /// bone's per-frame movement from the playing clip(s), suppresses the root bone's
    /// animated local translation, and either applies the delta to the skeleton root
    /// entity's Transform3D or exposes it for character controllers, depending on
    /// <see cref="Components.RootMotion.Mode"/>.
    /// </para>
    /// <para>
    /// Disabled by default: worlds that do not use root motion pay no per-frame cost.
    /// </para>
    /// </remarks>
    public bool EnableRootMotion { get; set; }

    /// <summary>
    /// Gets or sets whether GPU skinning support is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the plugin registers the <see cref="Components.SkinnedMesh"/> component
    /// and adds <see cref="Systems.SkinnedMeshBoneSystem"/> at order 80, which computes final
    /// bone matrices (world transform × inverse bind matrix) after animation and IK have
    /// produced the frame's final bone transforms.
    /// </para>
    /// <para>
    /// Disabled by default: only worlds that render skinned meshes need it.
    /// </para>
    /// </remarks>
    public bool EnableGpuSkinning { get; set; }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (MaxTweensPerEntity <= 0)
        {
            return "MaxTweensPerEntity must be positive";
        }

        return null;
    }
}
