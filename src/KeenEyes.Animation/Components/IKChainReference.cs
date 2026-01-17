namespace KeenEyes.Animation.Components;

/// <summary>
/// Links an end effector bone entity to its IK chain configuration.
/// </summary>
[Component]
public partial struct IKChainReference
{
    /// <summary>Chain definition ID from IKManager.</summary>
    public int ChainId;

    /// <summary>Entity ID containing the IKTarget component.</summary>
    public int TargetEntityId;

    /// <summary>Per-chain weight multiplier (combined with IKRig.GlobalWeight).</summary>
    public float Weight;

    /// <summary>Enable/disable this specific chain.</summary>
    public bool Enabled;

    /// <summary>Default IKChainReference configuration.</summary>
    public static IKChainReference Default => new()
    {
        ChainId = -1,
        TargetEntityId = -1,
        Weight = 1f,
        Enabled = true
    };

    /// <summary>Creates a chain reference with target.</summary>
    public static IKChainReference ForChain(int chainId, int targetEntityId) => Default with
    {
        ChainId = chainId,
        TargetEntityId = targetEntityId
    };
}
