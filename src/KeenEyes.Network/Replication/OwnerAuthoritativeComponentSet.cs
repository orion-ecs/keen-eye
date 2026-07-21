using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Replication;

/// <summary>
/// Resolves which replicated component types use the
/// <see cref="SyncStrategy.OwnerAuthoritative"/> synchronization strategy.
/// </summary>
/// <remarks>
/// <para>
/// The set is built once from the strategy metadata exposed by
/// <see cref="INetworkSerializer.GetRegisteredComponentInfo"/>, which the network
/// source generator emits per replicated component. This keeps runtime strategy
/// dispatch reflection-free and AOT-compatible.
/// </para>
/// </remarks>
internal sealed class OwnerAuthoritativeComponentSet
{
    private readonly HashSet<Type> types = [];

    /// <summary>
    /// Initializes the set from a network serializer's component metadata.
    /// </summary>
    /// <param name="serializer">The serializer providing component strategy metadata.</param>
    public OwnerAuthoritativeComponentSet(INetworkSerializer serializer)
    {
        foreach (var info in serializer.GetRegisteredComponentInfo())
        {
            if (info.Strategy == SyncStrategy.OwnerAuthoritative)
            {
                types.Add(info.Type);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified component type is owner-authoritative.
    /// </summary>
    /// <param name="type">The component type to check.</param>
    /// <returns><see langword="true"/> if the type uses the owner-authoritative strategy.</returns>
    public bool Contains(Type type) => types.Contains(type);
}
