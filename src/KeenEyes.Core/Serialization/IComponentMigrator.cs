using System.Text.Json;

namespace KeenEyes.Serialization;

/// <summary>
/// Interface for AOT-compatible component migration.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by generated code when components define migration methods
/// using the <c>[MigrateFrom(version)]</c> attribute. The source generator creates a strongly-typed
/// implementation that chains migration methods automatically.
/// </para>
/// <para>
/// The migration pipeline invokes migrations sequentially for multi-version upgrades.
/// For example, migrating from v1 to v4 calls: v1→v2, v2→v3, v3→v4.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use generated migrator during snapshot restoration
/// var migrator = new ComponentMigrator();  // Generated class
///
/// if (migrator.CanMigrate("MyComponent", fromVersion: 1, toVersion: 3))
/// {
///     var migratedData = migrator.Migrate("MyComponent", oldData, fromVersion: 1, toVersion: 3);
///     // Use migratedData to deserialize the component
/// }
/// </code>
/// </example>
public interface IComponentMigrator
{
    /// <summary>
    /// Checks if a migration path exists for a component type.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <param name="fromVersion">The source version of the component data.</param>
    /// <param name="toVersion">The target version to migrate to.</param>
    /// <returns>
    /// <c>true</c> if all migration steps from <paramref name="fromVersion"/> to
    /// <paramref name="toVersion"/> are available; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks that a complete migration chain exists. For example, to migrate
    /// from v1 to v3, migrations for v1→v2 and v2→v3 must both be defined.
    /// </para>
    /// <para>
    /// Returns <c>false</c> if:
    /// <list type="bullet">
    /// <item><description>The component type is not registered</description></item>
    /// <item><description>No migration is defined for any version step</description></item>
    /// <item><description><paramref name="fromVersion"/> >= <paramref name="toVersion"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    bool CanMigrate(string typeName, int fromVersion, int toVersion);

    /// <summary>
    /// Checks if a migration path exists for a component type.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <param name="fromVersion">The source version of the component data.</param>
    /// <param name="toVersion">The target version to migrate to.</param>
    /// <returns>
    /// <c>true</c> if all migration steps from <paramref name="fromVersion"/> to
    /// <paramref name="toVersion"/> are available; <c>false</c> otherwise.
    /// </returns>
    bool CanMigrate(Type type, int fromVersion, int toVersion);

    /// <summary>
    /// Migrates component data from one version to another.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <param name="data">The JSON element containing the component data at <paramref name="fromVersion"/>.</param>
    /// <param name="fromVersion">The source version of the component data.</param>
    /// <param name="toVersion">The target version to migrate to.</param>
    /// <returns>
    /// A <see cref="JsonElement"/> containing the migrated component data at <paramref name="toVersion"/>,
    /// or <c>null</c> if migration is not possible.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For multi-version migrations, this method chains the individual migration steps.
    /// For example, migrating from v1 to v3 internally calls the v1→v2 migration,
    /// serializes the result to JSON, then calls the v2→v3 migration.
    /// </para>
    /// <para>
    /// The returned <see cref="JsonElement"/> is suitable for deserialization using
    /// <see cref="IComponentSerializer.Deserialize"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ComponentVersionException">
    /// Thrown when a migration step fails or when no migration path exists.
    /// </exception>
    JsonElement? Migrate(string typeName, JsonElement data, int fromVersion, int toVersion);

    /// <summary>
    /// Migrates component data from one version to another.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <param name="data">The JSON element containing the component data at <paramref name="fromVersion"/>.</param>
    /// <param name="fromVersion">The source version of the component data.</param>
    /// <param name="toVersion">The target version to migrate to.</param>
    /// <returns>
    /// A <see cref="JsonElement"/> containing the migrated component data at <paramref name="toVersion"/>,
    /// or <c>null</c> if migration is not possible.
    /// </returns>
    /// <exception cref="ComponentVersionException">
    /// Thrown when a migration step fails or when no migration path exists.
    /// </exception>
    JsonElement? Migrate(Type type, JsonElement data, int fromVersion, int toVersion);

    /// <summary>
    /// Gets all registered migration source versions for a component type.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <returns>
    /// An enumerable of version numbers that have migrations defined, or an empty enumerable
    /// if the type has no migrations or is not registered.
    /// </returns>
    /// <remarks>
    /// This method is useful for diagnostics and determining which versions can be migrated.
    /// For example, if a component at v3 has migrations defined for v1 and v2, this returns [1, 2].
    /// </remarks>
    IEnumerable<int> GetMigrationVersions(string typeName);

    /// <summary>
    /// Gets all registered migration source versions for a component type.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <returns>
    /// An enumerable of version numbers that have migrations defined, or an empty enumerable
    /// if the type has no migrations or is not registered.
    /// </returns>
    IEnumerable<int> GetMigrationVersions(Type type);
}
