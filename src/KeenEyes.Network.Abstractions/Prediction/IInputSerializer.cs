using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Prediction;

/// <summary>
/// Interface for serializing network inputs.
/// </summary>
/// <remarks>
/// <para>
/// Game-specific input serializers should implement this interface.
/// The serializer handles packing inputs efficiently for network transmission.
/// </para>
/// </remarks>
public interface IInputSerializer
{
    /// <summary>
    /// Serializes an input to a bit writer.
    /// </summary>
    /// <param name="input">The input to serialize (boxed).</param>
    /// <param name="writer">The bit writer to write to.</param>
    void Serialize(object input, ref BitWriter writer);

    /// <summary>
    /// Deserializes an input from a bit reader.
    /// </summary>
    /// <param name="reader">The bit reader to read from.</param>
    /// <returns>The deserialized input (boxed).</returns>
    object Deserialize(ref BitReader reader);

    /// <summary>
    /// Gets the input type this serializer handles.
    /// </summary>
    Type InputType { get; }
}

/// <summary>
/// Typed interface for serializing network inputs.
/// </summary>
/// <typeparam name="T">The input type.</typeparam>
public interface IInputSerializer<T> : IInputSerializer where T : struct, INetworkInput
{
    /// <summary>
    /// Serializes an input to a bit writer.
    /// </summary>
    /// <param name="input">The input to serialize.</param>
    /// <param name="writer">The bit writer to write to.</param>
    void Serialize(in T input, ref BitWriter writer);

    /// <summary>
    /// Deserializes an input from a bit reader.
    /// </summary>
    /// <param name="reader">The bit reader to read from.</param>
    /// <returns>The deserialized input.</returns>
    new T Deserialize(ref BitReader reader);
}
