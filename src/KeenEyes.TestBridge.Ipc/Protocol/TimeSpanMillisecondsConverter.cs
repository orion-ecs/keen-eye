using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.TestBridge.Ipc.Protocol;

/// <summary>
/// Converts <see cref="TimeSpan"/> to/from milliseconds for JSON serialization.
/// </summary>
/// <remarks>
/// This converter serializes TimeSpan as a numeric milliseconds value rather than
/// the default ISO 8601 duration string, making it more debuggable and compatible
/// with other languages.
/// </remarks>
public sealed class TimeSpanMillisecondsConverter : JsonConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TimeSpan.FromMilliseconds(reader.GetDouble());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.TotalMilliseconds);
    }
}
