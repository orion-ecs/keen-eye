namespace KeenEyes;

/// <summary>
/// Interface for systems that explicitly declare their component dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide component read/write dependencies
/// that enable parallel system execution. The scheduler uses these dependencies
/// to determine which systems can run concurrently.
/// </para>
/// <para>
/// If a system does not implement this interface, its dependencies may be
/// inferred from registered queries or assumed to be unsafe for parallelization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MovementSystem : SystemBase, ISystemDependencyProvider
/// {
///     public void GetDependencies(ISystemDependencyBuilder builder)
///     {
///         builder
///             .Reads&lt;Position&gt;()
///             .Writes&lt;Velocity&gt;();
///     }
///
///     public override void Update(float deltaTime)
///     {
///         // System implementation
///     }
/// }
/// </code>
/// </example>
public interface ISystemDependencyProvider
{
    /// <summary>
    /// Declares the component dependencies for this system.
    /// </summary>
    /// <param name="builder">The dependency builder to configure.</param>
    void GetDependencies(ISystemDependencyBuilder builder);
}

/// <summary>
/// Fluent builder for declaring system component dependencies.
/// </summary>
public interface ISystemDependencyBuilder
{
    /// <summary>
    /// Declares that the system reads the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type that is read.</typeparam>
    /// <returns>This builder for chaining.</returns>
    ISystemDependencyBuilder Reads<T>() where T : struct, IComponent;

    /// <summary>
    /// Declares that the system writes the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type that is written.</typeparam>
    /// <returns>This builder for chaining.</returns>
    ISystemDependencyBuilder Writes<T>() where T : struct, IComponent;

    /// <summary>
    /// Declares that the system reads and writes the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type that is both read and written.</typeparam>
    /// <returns>This builder for chaining.</returns>
    ISystemDependencyBuilder ReadWrites<T>() where T : struct, IComponent;
}
