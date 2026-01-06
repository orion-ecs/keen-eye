using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// AOT-compatible JSON converter for <see cref="Vector3"/>.
/// </summary>
/// <remarks>
/// Serializes Vector3 as <c>{"x":1.0,"y":2.0,"z":3.0}</c>.
/// </remarks>
internal sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
    /// <inheritdoc />
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object");
        }

        float x = 0, y = 0, z = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new Vector3(x, y, z);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "x":
                    x = reader.GetSingle();
                    break;
                case "y":
                    y = reader.GetSingle();
                    break;
                case "z":
                    z = reader.GetSingle();
                    break;
            }
        }

        throw new JsonException("Expected end of object");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}

/// <summary>
/// AOT-compatible JSON converter for <see cref="Quaternion"/>.
/// </summary>
/// <remarks>
/// Serializes Quaternion as <c>{"x":0.0,"y":0.0,"z":0.0,"w":1.0}</c>.
/// </remarks>
internal sealed class QuaternionJsonConverter : JsonConverter<Quaternion>
{
    /// <inheritdoc />
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object");
        }

        float x = 0, y = 0, z = 0, w = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new Quaternion(x, y, z, w);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "x":
                    x = reader.GetSingle();
                    break;
                case "y":
                    y = reader.GetSingle();
                    break;
                case "z":
                    z = reader.GetSingle();
                    break;
                case "w":
                    w = reader.GetSingle();
                    break;
            }
        }

        throw new JsonException("Expected end of object");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteNumber("w", value.W);
        writer.WriteEndObject();
    }
}
