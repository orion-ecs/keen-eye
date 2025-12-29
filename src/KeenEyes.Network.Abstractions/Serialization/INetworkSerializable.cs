namespace KeenEyes.Network.Serialization;

/// <summary>
/// Interface for components that can be serialized for network transmission.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by generated code when components are marked with
/// <see cref="ReplicatedAttribute"/>. The source generator creates optimized
/// bit-packed serialization code.
/// </para>
/// <para>
/// Unlike binary component serialization which uses byte-aligned encoding for
/// save files, network serialization is bit-packed for bandwidth efficiency
/// and may use lossy quantization.
/// </para>
/// </remarks>
public interface INetworkSerializable
{
    /// <summary>
    /// Serializes the component to a bit writer.
    /// </summary>
    /// <param name="writer">The bit writer to write to.</param>
    void NetworkSerialize(ref BitWriter writer);

    /// <summary>
    /// Deserializes the component from a bit reader.
    /// </summary>
    /// <param name="reader">The bit reader to read from.</param>
    void NetworkDeserialize(ref BitReader reader);
}

/// <summary>
/// Interface for components that support delta serialization.
/// </summary>
/// <typeparam name="TSelf">The component type (for self-referencing).</typeparam>
/// <remarks>
/// <para>
/// Delta serialization only transmits fields that changed since the last acknowledged
/// state, significantly reducing bandwidth for components with many fields where only
/// a few change each frame.
/// </para>
/// <para>
/// A dirty mask tracks which fields changed: each bit corresponds to a field.
/// Only fields with their bit set are included in the serialized data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Generated implementation
/// public partial struct Transform : INetworkDeltaSerializable&lt;Transform&gt;
/// {
///     public uint GetDirtyMask(in Transform baseline)
///     {
///         uint mask = 0;
///         if (!Position.ApproximatelyEquals(baseline.Position)) mask |= 1;
///         if (!Rotation.ApproximatelyEquals(baseline.Rotation)) mask |= 2;
///         if (!Scale.ApproximatelyEquals(baseline.Scale)) mask |= 4;
///         return mask;
///     }
/// }
/// </code>
/// </example>
public interface INetworkDeltaSerializable<TSelf> : INetworkSerializable
    where TSelf : struct
{
    /// <summary>
    /// Gets a bitmask indicating which fields have changed since the baseline.
    /// </summary>
    /// <param name="baseline">The baseline state to compare against.</param>
    /// <returns>
    /// A bitmask where each bit represents a field. Bit N is set if field N changed.
    /// </returns>
    uint GetDirtyMask(in TSelf baseline);

    /// <summary>
    /// Serializes only the changed fields to a bit writer.
    /// </summary>
    /// <param name="writer">The bit writer to write to.</param>
    /// <param name="baseline">The baseline state for delta encoding.</param>
    /// <param name="dirtyMask">The dirty mask from <see cref="GetDirtyMask"/>.</param>
    void NetworkSerializeDelta(ref BitWriter writer, in TSelf baseline, uint dirtyMask);

    /// <summary>
    /// Deserializes changed fields from a bit reader, applying them to the baseline.
    /// </summary>
    /// <param name="reader">The bit reader to read from.</param>
    /// <param name="baseline">The baseline state to apply changes to.</param>
    /// <param name="dirtyMask">The dirty mask indicating which fields to read.</param>
    void NetworkDeserializeDelta(ref BitReader reader, ref TSelf baseline, uint dirtyMask);
}

/// <summary>
/// Interface for components that support interpolation.
/// </summary>
/// <typeparam name="TSelf">The component type (for self-referencing).</typeparam>
/// <remarks>
/// Implemented by components with <see cref="ReplicatedAttribute.GenerateInterpolation"/> = true.
/// Used to smoothly render remote entities between network updates.
/// </remarks>
public interface INetworkInterpolatable<TSelf>
    where TSelf : struct
{
    /// <summary>
    /// Interpolates between two component states.
    /// </summary>
    /// <param name="from">The starting state.</param>
    /// <param name="to">The target state.</param>
    /// <param name="t">The interpolation factor (0 = from, 1 = to).</param>
    /// <returns>The interpolated state.</returns>
    static abstract TSelf Interpolate(in TSelf from, in TSelf to, float t);
}

/// <summary>
/// Interface for components that support extrapolation (prediction beyond known state).
/// </summary>
/// <typeparam name="TSelf">The component type (for self-referencing).</typeparam>
/// <remarks>
/// Used as a fallback when network packets are delayed. Extrapolates the last known
/// state forward in time based on velocity or other motion patterns.
/// </remarks>
public interface INetworkExtrapolatable<TSelf>
    where TSelf : struct
{
    /// <summary>
    /// Extrapolates the component state forward in time.
    /// </summary>
    /// <param name="current">The current/last known state.</param>
    /// <param name="deltaTime">The time to extrapolate forward.</param>
    /// <returns>The extrapolated state.</returns>
    static abstract TSelf Extrapolate(in TSelf current, float deltaTime);
}

/// <summary>
/// Runtime registry for network interpolation operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by generated code that can interpolate components
/// marked with <see cref="ReplicatedAttribute"/> that have interpolation enabled.
/// It provides type-erased interpolation for use at runtime.
/// </para>
/// </remarks>
public interface INetworkInterpolator
{
    /// <summary>
    /// Checks if a component type supports interpolation.
    /// </summary>
    /// <param name="type">The component type to check.</param>
    /// <returns>True if the type can be interpolated; false otherwise.</returns>
    bool IsInterpolatable(Type type);

    /// <summary>
    /// Interpolates between two component values.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <param name="from">The starting value (boxed).</param>
    /// <param name="to">The target value (boxed).</param>
    /// <param name="factor">The interpolation factor (0 = from, 1 = to).</param>
    /// <returns>The interpolated value (boxed), or null if type not interpolatable.</returns>
    object? Interpolate(Type type, object from, object to, float factor);
}
