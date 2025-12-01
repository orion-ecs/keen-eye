using System;

namespace KeenEye;

/// <summary>
/// Marks a struct as a query definition. Source generators will produce
/// an efficient enumerator for iterating matching archetypes.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class QueryAttribute : Attribute;

/// <summary>
/// Marks a field in a query struct as a filter requirement.
/// The entity must have this component, but it won't be accessible in the query.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class WithAttribute : Attribute;

/// <summary>
/// Marks a field in a query struct as an exclusion filter.
/// The entity must NOT have this component.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class WithoutAttribute : Attribute;

/// <summary>
/// Marks a field in a query struct as optional.
/// The component will be default if not present on the entity.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class OptionalAttribute : Attribute;
