using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Marks a method as a hot path that runs frequently (e.g., every frame).
/// </summary>
/// <remarks>
/// <para>
/// Methods marked with this attribute are analyzed for performance anti-patterns
/// such as LINQ usage, allocations, and other operations that should be avoided
/// in frequently-executed code paths.
/// </para>
/// <para>
/// The analyzer automatically treats the following as hot paths without requiring this attribute:
/// <list type="bullet">
/// <item><description><see cref="SystemBase.Update(float)"/> method overrides</description></item>
/// <item><description><c>OnBeforeUpdate</c> and <c>OnAfterUpdate</c> method overrides</description></item>
/// </list>
/// </para>
/// <para>
/// Use this attribute for custom methods that are called frequently but are not
/// automatically recognized as hot paths.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GameSystem : SystemBase
/// {
///     public override void Update(float deltaTime)
///     {
///         // Automatically detected as hot path - no attribute needed
///         ProcessEntities(deltaTime);
///     }
///
///     [HotPath]
///     private void ProcessEntities(float deltaTime)
///     {
///         // Also analyzed as hot path due to attribute
///         foreach (var entity in World.Query&lt;Position, Velocity&gt;())
///         {
///             // LINQ would trigger warning here
///         }
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class HotPathAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a description of why this method is a hot path.
    /// </summary>
    /// <remarks>
    /// Use this to document the expected call frequency or performance requirements
    /// for future maintainers.
    /// </remarks>
    /// <example>
    /// <code>
    /// [HotPath(Reason = "Called every frame for each active particle")]
    /// private void UpdateParticle(ref Particle particle, float deltaTime)
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public string? Reason { get; set; }
}
