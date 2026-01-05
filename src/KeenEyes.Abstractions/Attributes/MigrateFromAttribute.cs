using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Marks a static method as a migration handler for upgrading component data from an older schema version.
/// </summary>
/// <remarks>
/// <para>
/// Migration methods are used during deserialization to transform component data from older
/// versions to the current version. When a serialized component has a lower version than the
/// current <see cref="ComponentAttribute.Version"/>, the migration pipeline will invoke
/// the appropriate migration methods in sequence.
/// </para>
/// <para>
/// Migration methods must:
/// <list type="bullet">
/// <item><description>Be static</description></item>
/// <item><description>Return the component type they are defined in</description></item>
/// <item><description>Take a <see cref="System.Text.Json.JsonElement"/> parameter containing the old component data</description></item>
/// </list>
/// </para>
/// <para>
/// For multi-version migrations (e.g., v1 → v4), define separate migration methods for each
/// version step. The migration pipeline will chain them automatically: v1 → v2 → v3 → v4.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Component(Serializable = true, Version = 3)]
/// public partial struct Position : IComponent
/// {
///     public float X;
///     public float Y;
///     public float Z;  // Added in v2
///     public float W;  // Added in v3
///
///     /// &lt;summary&gt;
///     /// Migrates from v1 (X, Y only) to v2 (adds Z).
///     /// &lt;/summary&gt;
///     [MigrateFrom(1)]
///     private static Position MigrateFromV1(JsonElement oldData)
///     {
///         return new Position
///         {
///             X = oldData.GetProperty("x").GetSingle(),
///             Y = oldData.GetProperty("y").GetSingle(),
///             Z = 0f  // Default value for new field
///         };
///     }
///
///     /// &lt;summary&gt;
///     /// Migrates from v2 (X, Y, Z) to v3 (adds W).
///     /// &lt;/summary&gt;
///     [MigrateFrom(2)]
///     private static Position MigrateFromV2(JsonElement oldData)
///     {
///         return new Position
///         {
///             X = oldData.GetProperty("x").GetSingle(),
///             Y = oldData.GetProperty("y").GetSingle(),
///             Z = oldData.GetProperty("z").GetSingle(),
///             W = 1f  // Default value for new field
///         };
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class MigrateFromAttribute : Attribute
{
    /// <summary>
    /// Gets the source version that this migration handles.
    /// </summary>
    /// <remarks>
    /// The migration method will be invoked when deserializing component data with this version.
    /// The method should return component data compatible with version <c>FromVersion + 1</c>.
    /// </remarks>
    public int FromVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateFromAttribute"/> class.
    /// </summary>
    /// <param name="fromVersion">
    /// The source version that this migration handles. Must be at least 1 and less than
    /// the current <see cref="ComponentAttribute.Version"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fromVersion"/> is less than 1.
    /// </exception>
    public MigrateFromAttribute(int fromVersion)
    {
        if (fromVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fromVersion), fromVersion,
                "Migration version must be at least 1.");
        }

        FromVersion = fromVersion;
    }
}
