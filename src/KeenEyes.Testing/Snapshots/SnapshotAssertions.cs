namespace KeenEyes.Testing.Snapshots;

/// <summary>
/// Fluent assertion extensions for snapshot testing.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a fluent API for asserting world and entity
/// states using snapshots, making tests more readable and maintainable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var expected = WorldSnapshot.Create(world);
/// world.Update(1.0f);
/// var actual = WorldSnapshot.Create(world);
///
/// actual.ShouldEqual(expected);
/// actual.ShouldHaveEntityCount(expected.EntityCount);
/// </code>
/// </example>
public static class SnapshotAssertions
{
    /// <summary>
    /// Asserts that the actual snapshot matches the expected snapshot.
    /// </summary>
    /// <param name="actual">The actual snapshot.</param>
    /// <param name="expected">The expected snapshot.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the snapshots differ.</exception>
    public static WorldSnapshot ShouldEqual(
        this WorldSnapshot actual,
        WorldSnapshot expected,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);

        var comparison = SnapshotComparer.Compare(expected, actual);

        if (!comparison.AreEqual)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshots to be equal{reason}.{Environment.NewLine}{comparison.GetReport()}");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the actual snapshot has the expected entity count.
    /// </summary>
    /// <param name="actual">The actual snapshot.</param>
    /// <param name="expectedCount">The expected entity count.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static WorldSnapshot ShouldHaveEntityCount(
        this WorldSnapshot actual,
        int expectedCount,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(actual);

        if (actual.EntityCount != expectedCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshot to have {expectedCount} entities{reason}, but found {actual.EntityCount}.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the snapshot contains an entity with the specified ID.
    /// </summary>
    /// <param name="actual">The actual snapshot.</param>
    /// <param name="entityId">The expected entity ID.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity is not found.</exception>
    public static WorldSnapshot ShouldContainEntity(
        this WorldSnapshot actual,
        int entityId,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(actual);

        if (actual.GetEntity(entityId) == null)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshot to contain entity {entityId}{reason}, but it was not found.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the snapshot does not contain an entity with the specified ID.
    /// </summary>
    /// <param name="actual">The actual snapshot.</param>
    /// <param name="entityId">The entity ID that should not exist.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the entity is found.</exception>
    public static WorldSnapshot ShouldNotContainEntity(
        this WorldSnapshot actual,
        int entityId,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(actual);

        if (actual.GetEntity(entityId) != null)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshot to not contain entity {entityId}{reason}, but it was found.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the snapshot contains entities with the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual snapshot.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no entities have the component.</exception>
    public static WorldSnapshot ShouldHaveEntitiesWithComponent<T>(
        this WorldSnapshot actual,
        string? because = null) where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(actual);

        var entities = actual.EntitiesWithComponent<T>().ToList();

        if (entities.Count == 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshot to have entities with component {typeof(T).Name}{reason}, but none were found.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the entity snapshot has the specified component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual entity snapshot.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual entity snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the component is not found.</exception>
    public static EntitySnapshot ShouldHaveComponent<T>(
        this EntitySnapshot actual,
        string? because = null) where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(actual);

        if (!actual.HasComponent<T>())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity {actual.EntityId} to have component {typeof(T).Name}{reason}, but it was not found.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the entity snapshot does not have the specified component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual entity snapshot.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual entity snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the component is found.</exception>
    public static EntitySnapshot ShouldNotHaveComponent<T>(
        this EntitySnapshot actual,
        string? because = null) where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(actual);

        if (actual.HasComponent<T>())
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected entity {actual.EntityId} to not have component {typeof(T).Name}{reason}, but it was found.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the entity snapshot equals another entity snapshot.
    /// </summary>
    /// <param name="actual">The actual entity snapshot.</param>
    /// <param name="expected">The expected entity snapshot.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual entity snapshot for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the snapshots differ.</exception>
    public static EntitySnapshot ShouldEqual(
        this EntitySnapshot actual,
        EntitySnapshot expected,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);

        var differences = SnapshotComparer.CompareEntities(expected, actual);

        if (differences.Count > 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            var diffMessages = string.Join(Environment.NewLine, differences.Select(d => $"  - {d.Message}"));
            throw new AssertionException(
                $"Expected entity snapshots to be equal{reason}.{Environment.NewLine}{diffMessages}");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the comparison result shows no differences.
    /// </summary>
    /// <param name="comparison">The comparison result.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The comparison for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when differences exist.</exception>
    public static SnapshotComparison ShouldBeEqual(
        this SnapshotComparison comparison,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        if (!comparison.AreEqual)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshots to be equal{reason}.{Environment.NewLine}{comparison.GetReport()}");
        }

        return comparison;
    }

    /// <summary>
    /// Asserts that the comparison result shows differences.
    /// </summary>
    /// <param name="comparison">The comparison result.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The comparison for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no differences exist.</exception>
    public static SnapshotComparison ShouldNotBeEqual(
        this SnapshotComparison comparison,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        if (comparison.AreEqual)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected snapshots to not be equal{reason}, but they were identical.");
        }

        return comparison;
    }

    /// <summary>
    /// Asserts that the comparison has exactly the expected number of differences.
    /// </summary>
    /// <param name="comparison">The comparison result.</param>
    /// <param name="expectedCount">The expected difference count.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The comparison for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static SnapshotComparison ShouldHaveDifferenceCount(
        this SnapshotComparison comparison,
        int expectedCount,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        if (comparison.DifferenceCount != expectedCount)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected {expectedCount} difference(s){reason}, but found {comparison.DifferenceCount}.{Environment.NewLine}{comparison.GetReport()}");
        }

        return comparison;
    }
}
