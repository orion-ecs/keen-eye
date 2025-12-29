namespace KeenEyes.Network;

/// <summary>
/// Marks a component for network replication.
/// </summary>
/// <remarks>
/// <para>
/// Components marked with this attribute will have network serialization code generated
/// by the source generator. The generated code includes efficient bit-packed serialization,
/// delta encoding, and optional interpolation/prediction helpers.
/// </para>
/// <para>
/// Unlike save/load serialization which preserves full fidelity, network serialization
/// is optimized for bandwidth efficiency and may use lossy quantization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Component]
/// [Replicated(Strategy = SyncStrategy.Interpolated)]
/// public partial struct Position
/// {
///     [Quantized(Min = -1000, Max = 1000, Resolution = 0.01f)]
///     public float X;
///
///     [Quantized(Min = -1000, Max = 1000, Resolution = 0.01f)]
///     public float Y;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ReplicatedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the synchronization strategy for this component.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="SyncStrategy.Authoritative"/> (server state applied directly).
    /// </remarks>
    public SyncStrategy Strategy { get; set; } = SyncStrategy.Authoritative;

    /// <summary>
    /// Gets or sets the target update frequency in updates per second.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lower values reduce bandwidth but increase latency. Higher values provide
    /// smoother updates but consume more bandwidth.
    /// </para>
    /// <para>
    /// A value of 0 means "sync as fast as possible" (every network tick).
    /// Defaults to 0.
    /// </para>
    /// </remarks>
    public int Frequency { get; set; }

    /// <summary>
    /// Gets or sets the network priority for bandwidth allocation.
    /// </summary>
    /// <remarks>
    /// Higher priority components are sent more frequently when bandwidth is limited.
    /// Range: 0 (lowest) to 255 (highest). Defaults to 128 (normal).
    /// </remarks>
    public byte Priority { get; set; } = 128;

    /// <summary>
    /// Gets or sets whether to generate interpolation helpers for this component.
    /// </summary>
    /// <remarks>
    /// When true, generates a static <c>Interpolate(in T from, in T to, float t)</c> method.
    /// Useful for smoothly rendering remote entities between network updates.
    /// </remarks>
    public bool GenerateInterpolation { get; set; }

    /// <summary>
    /// Gets or sets whether to generate prediction/rollback helpers for this component.
    /// </summary>
    /// <remarks>
    /// When true, generates helpers for client-side prediction and server reconciliation.
    /// Required for responsive local player controls.
    /// </remarks>
    public bool GeneratePrediction { get; set; }
}
