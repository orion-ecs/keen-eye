using System.Text;
using System.Text.Json;

namespace KeenEyes.Serialization;

/// <summary>
/// Represents the result of a component migration operation, including timing diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// This class captures detailed information about a migration execution:
/// <list type="bullet">
/// <item><description>Whether the migration succeeded</description></item>
/// <item><description>The migrated data (if successful)</description></item>
/// <item><description>Total execution time</description></item>
/// <item><description>Per-step timing breakdown</description></item>
/// <item><description>The migration chain that was executed</description></item>
/// </list>
/// </para>
/// <para>
/// Use this class when you need detailed diagnostics about migration performance,
/// especially for profiling or debugging slow migrations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = migrator.MigrateWithDiagnostics("MyComponent", data, 1, 4);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Migration took {result.TotalElapsed.TotalMilliseconds}ms");
///     foreach (var timing in result.StepTimings)
///     {
///         Console.WriteLine($"  {timing.Step}: {timing.Elapsed.TotalMilliseconds}ms");
///     }
/// }
/// </code>
/// </example>
public sealed class MigrationResult
{
    /// <summary>
    /// Gets whether the migration completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the migrated data, or <c>null</c> if migration failed.
    /// </summary>
    public JsonElement? Data { get; init; }

    /// <summary>
    /// Gets the component type name that was migrated.
    /// </summary>
    public string? ComponentTypeName { get; init; }

    /// <summary>
    /// Gets the source version of the migration.
    /// </summary>
    public int FromVersion { get; init; }

    /// <summary>
    /// Gets the target version of the migration.
    /// </summary>
    public int ToVersion { get; init; }

    /// <summary>
    /// Gets the total elapsed time for the entire migration chain.
    /// </summary>
    public TimeSpan TotalElapsed { get; init; }

    /// <summary>
    /// Gets the timing information for each step in the migration chain.
    /// </summary>
    public IReadOnlyList<MigrationStepTiming> StepTimings { get; init; } = [];

    /// <summary>
    /// Gets the error message if migration failed, or <c>null</c> if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the version at which the migration failed, or <c>null</c> if successful.
    /// </summary>
    public int? FailedAtVersion { get; init; }

    /// <summary>
    /// Creates a successful migration result.
    /// </summary>
    /// <param name="data">The migrated data.</param>
    /// <param name="componentTypeName">The component type name.</param>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="totalElapsed">The total elapsed time.</param>
    /// <param name="stepTimings">The per-step timing information.</param>
    /// <returns>A successful migration result.</returns>
    public static MigrationResult Succeeded(
        JsonElement data,
        string componentTypeName,
        int fromVersion,
        int toVersion,
        TimeSpan totalElapsed,
        IReadOnlyList<MigrationStepTiming> stepTimings)
    {
        return new MigrationResult
        {
            Success = true,
            Data = data,
            ComponentTypeName = componentTypeName,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            TotalElapsed = totalElapsed,
            StepTimings = stepTimings
        };
    }

    /// <summary>
    /// Creates a failed migration result.
    /// </summary>
    /// <param name="componentTypeName">The component type name.</param>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="totalElapsed">The total elapsed time before failure.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="failedAtVersion">The version at which migration failed.</param>
    /// <param name="stepTimings">The per-step timing information (for steps that completed).</param>
    /// <returns>A failed migration result.</returns>
    public static MigrationResult Failed(
        string componentTypeName,
        int fromVersion,
        int toVersion,
        TimeSpan totalElapsed,
        string errorMessage,
        int? failedAtVersion = null,
        IReadOnlyList<MigrationStepTiming>? stepTimings = null)
    {
        return new MigrationResult
        {
            Success = false,
            ComponentTypeName = componentTypeName,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            TotalElapsed = totalElapsed,
            ErrorMessage = errorMessage,
            FailedAtVersion = failedAtVersion,
            StepTimings = stepTimings ?? []
        };
    }

    /// <summary>
    /// Creates a result indicating no migration was needed (versions equal).
    /// </summary>
    /// <param name="data">The original data (unchanged).</param>
    /// <param name="componentTypeName">The component type name.</param>
    /// <param name="version">The version (both from and to).</param>
    /// <returns>A successful result with no migration steps.</returns>
    public static MigrationResult NoMigrationNeeded(
        JsonElement data,
        string componentTypeName,
        int version)
    {
        return new MigrationResult
        {
            Success = true,
            Data = data,
            ComponentTypeName = componentTypeName,
            FromVersion = version,
            ToVersion = version,
            TotalElapsed = TimeSpan.Zero,
            StepTimings = []
        };
    }

    /// <summary>
    /// Returns a diagnostic string representation of the migration result.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Migration Result for {ComponentTypeName}:");
        sb.AppendLine($"  Status: {(Success ? "Success" : "Failed")}");
        sb.AppendLine($"  Version: v{FromVersion} → v{ToVersion}");
        sb.AppendLine($"  Total Time: {TotalElapsed.TotalMilliseconds:F2}ms");

        if (StepTimings.Count > 0)
        {
            sb.AppendLine("  Steps:");
            foreach (var timing in StepTimings)
            {
                sb.AppendLine($"    {timing}");
            }
        }

        if (!Success && ErrorMessage is not null)
        {
            sb.AppendLine($"  Error: {ErrorMessage}");
            if (FailedAtVersion.HasValue)
            {
                sb.AppendLine($"  Failed at: v{FailedAtVersion}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Represents timing information for a single migration step.
/// </summary>
/// <param name="Step">The migration step (from → to version).</param>
/// <param name="Elapsed">The time elapsed for this step.</param>
public readonly record struct MigrationStepTiming(MigrationStep Step, TimeSpan Elapsed)
{
    /// <summary>
    /// Returns a string representation of the step timing.
    /// </summary>
    public override string ToString() => $"{Step}: {Elapsed.TotalMilliseconds:F2}ms";
}
