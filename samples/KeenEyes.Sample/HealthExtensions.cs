namespace KeenEyes.Sample;

// =============================================================================
// HEALTH COMPONENT EXTENSION PROPERTIES
// =============================================================================
// This file demonstrates the C# 13 extension property feature to add computed
// properties to components without violating ECS principles.
//
// By using extension properties, we keep the Health component as pure data
// (fields only) while still providing convenient access to derived values.
//
// This pattern is recommended for simple, readonly computations that don't
// justify creating a dedicated system. For complex logic, use systems instead.
// =============================================================================

/// <summary>
/// Extension properties for the Health component.
/// </summary>
public static class HealthExtensions
{
    /// <summary>
    /// Extension block providing computed properties for Health components.
    /// </summary>
    extension(Health health)
    {
        /// <summary>
        /// Gets the health percentage (0-1).
        /// Returns 0 if Max is 0 or negative to avoid division by zero.
        /// </summary>
        /// <remarks>
        /// This is a computed property that derives a value from the component's
        /// data fields. It contains no mutable state and performs a simple calculation.
        ///
        /// This pattern demonstrates when computed properties are acceptable in ECS:
        /// - The property has no side effects (no state mutation)
        /// - The calculation is trivial (no complex logic)
        /// - It's purely derived from existing fields
        /// - Performance: inlined, no allocation
        ///
        /// For more complex logic (e.g., applying damage, healing, status effects),
        /// use a dedicated system instead to keep components as pure data.
        /// </remarks>
        /// <example>
        /// <code>
        /// var health = new Health { Current = 75, Max = 100 };
        /// var percentage = health.Percentage; // 0.75
        ///
        /// // Use in queries:
        /// foreach (var entity in world.Query&lt;Health&gt;())
        /// {
        ///     ref readonly var health = ref world.Get&lt;Health&gt;(entity);
        ///     if (health.Percentage &lt; 0.25f)
        ///     {
        ///         // Low health logic
        ///     }
        /// }
        /// </code>
        /// </example>
        // S2325 suppressed: Extension member properties use the 'health' parameter
#pragma warning disable S2325
        public float Percentage => health.Max > 0 ? health.Current / health.Max : 0;
#pragma warning restore S2325
    }
}
