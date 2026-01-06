namespace KeenEyes.Testing.Snapshots;

/// <summary>
/// Compares two snapshots and produces a detailed diff of changes.
/// </summary>
/// <remarks>
/// <para>
/// SnapshotComparer can compare both world snapshots and entity snapshots,
/// producing detailed reports of all differences including added/removed
/// entities, added/removed components, and changed field values.
/// </para>
/// </remarks>
public static class SnapshotComparer
{
    /// <summary>
    /// Compares two world snapshots and returns the differences.
    /// </summary>
    /// <param name="expected">The expected (baseline) snapshot.</param>
    /// <param name="actual">The actual (current) snapshot.</param>
    /// <returns>A comparison result with all differences.</returns>
    public static SnapshotComparison Compare(WorldSnapshot expected, WorldSnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var differences = new List<SnapshotDifference>();

        // Find added entities
        foreach (var entityId in actual.EntityIds.Except(expected.EntityIds))
        {
            differences.Add(new SnapshotDifference
            {
                Type = DifferenceType.EntityAdded,
                EntityId = entityId,
                Message = $"Entity {entityId} was added"
            });
        }

        // Find removed entities
        foreach (var entityId in expected.EntityIds.Except(actual.EntityIds))
        {
            differences.Add(new SnapshotDifference
            {
                Type = DifferenceType.EntityRemoved,
                EntityId = entityId,
                Message = $"Entity {entityId} was removed"
            });
        }

        // Compare common entities
        foreach (var entityId in expected.EntityIds.Intersect(actual.EntityIds))
        {
            var expectedEntity = expected.GetEntity(entityId)!;
            var actualEntity = actual.GetEntity(entityId)!;

            differences.AddRange(CompareEntities(expectedEntity, actualEntity));
        }

        return new SnapshotComparison
        {
            Expected = expected,
            Actual = actual,
            Differences = differences
        };
    }

    /// <summary>
    /// Compares two entity snapshots and returns the differences.
    /// </summary>
    /// <param name="expected">The expected (baseline) entity snapshot.</param>
    /// <param name="actual">The actual (current) entity snapshot.</param>
    /// <returns>A list of differences between the two entities.</returns>
    public static IReadOnlyList<SnapshotDifference> CompareEntities(EntitySnapshot expected, EntitySnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var differences = new List<SnapshotDifference>();
        var entityId = expected.EntityId;

        // Check version change
        if (expected.Version != actual.Version)
        {
            differences.Add(new SnapshotDifference
            {
                Type = DifferenceType.VersionChanged,
                EntityId = entityId,
                Message = $"Entity {entityId} version changed from {expected.Version} to {actual.Version}"
            });
        }

        // Check name change
        if (expected.Name != actual.Name)
        {
            differences.Add(new SnapshotDifference
            {
                Type = DifferenceType.NameChanged,
                EntityId = entityId,
                ExpectedValue = expected.Name,
                ActualValue = actual.Name,
                Message = $"Entity {entityId} name changed from '{expected.Name ?? "(none)"}' to '{actual.Name ?? "(none)"}'"
            });
        }

        // Find added components
        foreach (var componentType in actual.ComponentTypes.Except(expected.ComponentTypes))
        {
            differences.Add(new SnapshotDifference
            {
                Type = DifferenceType.ComponentAdded,
                EntityId = entityId,
                ComponentType = componentType,
                Message = $"Entity {entityId}: Component {componentType} was added"
            });
        }

        // Find removed components
        foreach (var componentType in expected.ComponentTypes.Except(actual.ComponentTypes))
        {
            differences.Add(new SnapshotDifference
            {
                Type = DifferenceType.ComponentRemoved,
                EntityId = entityId,
                ComponentType = componentType,
                Message = $"Entity {entityId}: Component {componentType} was removed"
            });
        }

        // Compare common components
        foreach (var componentType in expected.ComponentTypes.Intersect(actual.ComponentTypes))
        {
            var expectedComponent = expected.GetComponent(componentType)!;
            var actualComponent = actual.GetComponent(componentType)!;

            differences.AddRange(CompareComponents(entityId, componentType, expectedComponent, actualComponent));
        }

        return differences;
    }

    private static List<SnapshotDifference> CompareComponents(
        int entityId,
        string componentType,
        Dictionary<string, object?> expected,
        Dictionary<string, object?> actual)
    {
        var differences = new List<SnapshotDifference>();

        // Find all field names
        var allFields = expected.Keys.Union(actual.Keys).ToHashSet();

        foreach (var fieldName in allFields)
        {
            var hasExpected = expected.TryGetValue(fieldName, out var expectedValue);
            var hasActual = actual.TryGetValue(fieldName, out var actualValue);

            if (!hasExpected && hasActual)
            {
                differences.Add(new SnapshotDifference
                {
                    Type = DifferenceType.FieldAdded,
                    EntityId = entityId,
                    ComponentType = componentType,
                    FieldName = fieldName,
                    ActualValue = actualValue,
                    Message = $"Entity {entityId}.{componentType}.{fieldName} was added with value {FormatValue(actualValue)}"
                });
            }
            else if (hasExpected && !hasActual)
            {
                differences.Add(new SnapshotDifference
                {
                    Type = DifferenceType.FieldRemoved,
                    EntityId = entityId,
                    ComponentType = componentType,
                    FieldName = fieldName,
                    ExpectedValue = expectedValue,
                    Message = $"Entity {entityId}.{componentType}.{fieldName} was removed (was {FormatValue(expectedValue)})"
                });
            }
            else if (!ValuesEqual(expectedValue, actualValue))
            {
                differences.Add(new SnapshotDifference
                {
                    Type = DifferenceType.FieldChanged,
                    EntityId = entityId,
                    ComponentType = componentType,
                    FieldName = fieldName,
                    ExpectedValue = expectedValue,
                    ActualValue = actualValue,
                    Message = $"Entity {entityId}.{componentType}.{fieldName} changed from {FormatValue(expectedValue)} to {FormatValue(actualValue)}"
                });
            }
        }

        return differences;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        // Handle numeric comparisons
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Convert.ToDouble(a).Equals(Convert.ToDouble(b));
        }

        return a.Equals(b);
    }

    private static bool IsNumeric(object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string str)
        {
            return $"\"{str}\"";
        }

        if (value is float f)
        {
            return f.ToString("G9");
        }

        if (value is double d)
        {
            return d.ToString("G17");
        }

        return value.ToString() ?? "null";
    }
}

/// <summary>
/// Represents the result of comparing two world snapshots.
/// </summary>
public sealed class SnapshotComparison
{
    /// <summary>
    /// Gets the expected (baseline) snapshot.
    /// </summary>
    public required WorldSnapshot Expected { get; init; }

    /// <summary>
    /// Gets the actual (current) snapshot.
    /// </summary>
    public required WorldSnapshot Actual { get; init; }

    /// <summary>
    /// Gets the list of differences found.
    /// </summary>
    public required IReadOnlyList<SnapshotDifference> Differences { get; init; }

    /// <summary>
    /// Gets whether the snapshots are identical.
    /// </summary>
    public bool AreEqual => Differences.Count == 0;

    /// <summary>
    /// Gets the number of differences found.
    /// </summary>
    public int DifferenceCount => Differences.Count;

    /// <summary>
    /// Gets differences of a specific type.
    /// </summary>
    public IEnumerable<SnapshotDifference> GetDifferences(DifferenceType type) =>
        Differences.Where(d => d.Type == type);

    /// <summary>
    /// Gets differences for a specific entity.
    /// </summary>
    public IEnumerable<SnapshotDifference> GetDifferencesForEntity(int entityId) =>
        Differences.Where(d => d.EntityId == entityId);

    /// <summary>
    /// Returns a formatted report of all differences.
    /// </summary>
    public string GetReport()
    {
        if (AreEqual)
        {
            return "Snapshots are identical.";
        }

        var lines = new List<string>
        {
            $"Found {DifferenceCount} difference(s):",
            ""
        };

        foreach (var diff in Differences)
        {
            lines.Add($"  - {diff.Message}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Returns a string representation of this comparison.
    /// </summary>
    public override string ToString()
    {
        return AreEqual
            ? "SnapshotComparison { AreEqual = true }"
            : $"SnapshotComparison {{ AreEqual = false, DifferenceCount = {DifferenceCount} }}";
    }
}

/// <summary>
/// Represents a single difference between two snapshots.
/// </summary>
public sealed class SnapshotDifference
{
    /// <summary>
    /// Gets the type of difference.
    /// </summary>
    public required DifferenceType Type { get; init; }

    /// <summary>
    /// Gets the entity ID involved in this difference.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the component type involved, if applicable.
    /// </summary>
    public string? ComponentType { get; init; }

    /// <summary>
    /// Gets the field name involved, if applicable.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Gets the expected value, if applicable.
    /// </summary>
    public object? ExpectedValue { get; init; }

    /// <summary>
    /// Gets the actual value, if applicable.
    /// </summary>
    public object? ActualValue { get; init; }

    /// <summary>
    /// Gets a human-readable message describing this difference.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Returns a string representation of this difference.
    /// </summary>
    public override string ToString() => Message;
}

/// <summary>
/// Specifies the type of difference between snapshots.
/// </summary>
public enum DifferenceType
{
    /// <summary>
    /// An entity was added.
    /// </summary>
    EntityAdded,

    /// <summary>
    /// An entity was removed.
    /// </summary>
    EntityRemoved,

    /// <summary>
    /// An entity's version changed.
    /// </summary>
    VersionChanged,

    /// <summary>
    /// An entity's name changed.
    /// </summary>
    NameChanged,

    /// <summary>
    /// A component was added to an entity.
    /// </summary>
    ComponentAdded,

    /// <summary>
    /// A component was removed from an entity.
    /// </summary>
    ComponentRemoved,

    /// <summary>
    /// A field was added to a component.
    /// </summary>
    FieldAdded,

    /// <summary>
    /// A field was removed from a component.
    /// </summary>
    FieldRemoved,

    /// <summary>
    /// A field value changed.
    /// </summary>
    FieldChanged
}
