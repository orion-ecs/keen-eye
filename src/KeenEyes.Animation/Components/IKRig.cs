namespace KeenEyes.Animation.Components;

/// <summary>
/// Container component for IK rig configuration on a skeleton root entity.
/// </summary>
[Component]
public partial struct IKRig
{
    /// <summary>Registration ID from IKManager (-1 if not registered).</summary>
    public int RigId;

    /// <summary>Master enable/disable for all IK chains on this rig.</summary>
    public bool Enabled;

    /// <summary>Global blend weight (0=FK only, 1=full IK).</summary>
    public float GlobalWeight;

    /// <summary>Default IKRig configuration.</summary>
    public static IKRig Default => new()
    {
        RigId = -1,
        Enabled = true,
        GlobalWeight = 1f
    };

    /// <summary>Creates an IKRig for a registered rig ID.</summary>
    public static IKRig ForRig(int rigId) => Default with { RigId = rigId };
}
