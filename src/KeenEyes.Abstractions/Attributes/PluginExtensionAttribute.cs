using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Marks a class as a plugin extension that should generate a typed accessor property on World.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a class, the source generator will create a C# 13 extension member
/// that provides direct property access instead of using World.GetExtension.
/// </para>
/// <para>
/// The generated extension allows code like <c>world.Physics</c> instead of
/// <c>world.GetExtension&lt;PhysicsWorld&gt;()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark the extension class
/// [PluginExtension("Physics")]
/// public sealed class PhysicsWorld
/// {
///     public RaycastHit? Raycast(Vector3 origin, Vector3 direction) { ... }
/// }
///
/// // Usage becomes:
/// var hit = world.Physics.Raycast(origin, direction);
/// </code>
/// </example>
/// <param name="propertyName">The name of the property to generate on World.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class PluginExtensionAttribute(string propertyName) : Attribute
{
    /// <summary>
    /// Gets the property name to generate on World.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets or sets whether the extension property is nullable.
    /// When true, the property returns null if the extension is not registered.
    /// When false (default), accessing the property throws if the extension is not registered.
    /// </summary>
    public bool Nullable { get; set; }
}
