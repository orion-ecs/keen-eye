using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Opts engine-provided components into this assembly's generated <c>ComponentSerializer</c>.
/// </summary>
/// <remarks>
/// <para>
/// The serialization source generator only sees components declared with
/// <c>[Component(Serializable = true)]</c> in the compiling project. Components that ship
/// with the engine (for example <c>KeenEyes.Common.Transform3D</c>) are therefore absent
/// from the generated serializer, and features that read them from snapshots - such as
/// replay ghost extraction - would silently receive no data.
/// </para>
/// <para>
/// Applying this attribute makes the generator emit the same AOT-safe serialization code
/// for each listed engine component as it does for project components. Each type must be
/// a struct implementing <see cref="IComponent"/>. Only the listed types are included;
/// nothing is registered implicitly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [assembly: SerializeEngineComponents(typeof(KeenEyes.Common.Transform3D))]
/// </code>
/// </example>
/// <param name="componentTypes">The engine component types to include in the generated serializer.</param>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class SerializeEngineComponentsAttribute(params Type[] componentTypes) : Attribute
{
    /// <summary>
    /// Gets the engine component types to include in the generated serializer.
    /// </summary>
    public Type[] ComponentTypes { get; } = componentTypes;
}
