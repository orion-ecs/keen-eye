using System.Numerics;

namespace KeenEyes.Animation.IK;

/// <summary>
/// Defines an IK rig containing multiple chains for a character.
/// </summary>
public sealed class IKRigDefinition
{
    private readonly List<IKChainDefinition> chains = [];

    /// <summary>Unique name for this rig.</summary>
    public required string Name { get; init; }

    /// <summary>All chains in this rig.</summary>
    public IReadOnlyList<IKChainDefinition> Chains => chains;

    /// <summary>Number of chains.</summary>
    public int ChainCount => chains.Count;

    /// <summary>Adds a chain to the rig.</summary>
    public IKRigDefinition AddChain(IKChainDefinition chain)
    {
        chains.Add(chain);
        return this;
    }

    /// <summary>Adds a two-bone chain.</summary>
    public IKRigDefinition AddTwoBoneChain(string name, string root, string mid, string tip, Vector3 poleVector)
    {
        chains.Add(IKChainDefinition.TwoBone(name, root, mid, tip, poleVector));
        return this;
    }

    /// <summary>Adds a FABRIK chain.</summary>
    public IKRigDefinition AddFABRIKChain(string name, string[] bones, int maxIterations = 10)
    {
        chains.Add(IKChainDefinition.MultiBone(name, bones, maxIterations));
        return this;
    }

    /// <summary>Finds a chain by name.</summary>
    public IKChainDefinition? FindChain(string name)
    {
        foreach (var chain in chains)
        {
            if (chain.Name == name)
            {
                return chain;
            }
        }

        return null;
    }
}
