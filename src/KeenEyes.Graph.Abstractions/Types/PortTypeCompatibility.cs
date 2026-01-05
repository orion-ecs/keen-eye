namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Provides port type compatibility checking.
/// </summary>
public static class PortTypeCompatibility
{
    /// <summary>
    /// Checks if a source port type can connect to a target port type.
    /// </summary>
    /// <param name="source">The source (output) port type.</param>
    /// <param name="target">The target (input) port type.</param>
    /// <returns>True if the connection is valid.</returns>
    /// <remarks>
    /// Supports implicit widening conversions:
    /// <list type="bullet">
    /// <item><description>float to float2, float3, float4</description></item>
    /// <item><description>float2 to float3, float4</description></item>
    /// <item><description>float3 to float4</description></item>
    /// <item><description>int to float</description></item>
    /// </list>
    /// Narrowing conversions are not allowed.
    /// </remarks>
    public static bool CanConnect(PortTypeId source, PortTypeId target)
    {
        // Same type always connects
        if (source == target)
        {
            return true;
        }

        // Any accepts anything
        if (target == PortTypeId.Any)
        {
            return true;
        }

        // Flow can only connect to flow
        if (source == PortTypeId.Flow || target == PortTypeId.Flow)
        {
            return false;
        }

        // Implicit widening conversions
        return (source, target) switch
        {
            // Float widening
            (PortTypeId.Float, PortTypeId.Float2 or PortTypeId.Float3 or PortTypeId.Float4) => true,
            (PortTypeId.Float2, PortTypeId.Float3 or PortTypeId.Float4) => true,
            (PortTypeId.Float3, PortTypeId.Float4) => true,

            // Int to float conversion
            (PortTypeId.Int, PortTypeId.Float) => true,

            // Int widening
            (PortTypeId.Int, PortTypeId.Int2 or PortTypeId.Int3 or PortTypeId.Int4) => true,
            (PortTypeId.Int2, PortTypeId.Int3 or PortTypeId.Int4) => true,
            (PortTypeId.Int3, PortTypeId.Int4) => true,

            _ => false
        };
    }

    /// <summary>
    /// Checks if a connection would require an implicit conversion.
    /// </summary>
    /// <param name="source">The source port type.</param>
    /// <param name="target">The target port type.</param>
    /// <returns>True if types differ but connection is valid.</returns>
    public static bool RequiresConversion(PortTypeId source, PortTypeId target)
    {
        return source != target && target != PortTypeId.Any && CanConnect(source, target);
    }
}
