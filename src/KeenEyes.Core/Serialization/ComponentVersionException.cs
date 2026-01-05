namespace KeenEyes.Serialization;

/// <summary>
/// Exception thrown when a component version mismatch is detected during deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when:
/// <list type="bullet">
/// <item><description>
/// The serialized data contains a newer version of a component than the current runtime version.
/// This typically means the save file was created with a newer version of the application.
/// </description></item>
/// <item><description>
/// A component migration is required but no migration path is available.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// For cases where the serialized version is older than the current version, the migration
/// pipeline will attempt to automatically migrate the data to the current version.
/// </para>
/// </remarks>
public class ComponentVersionException : InvalidOperationException
{
    /// <summary>
    /// Gets the name of the component that had a version mismatch.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the version of the component that was serialized.
    /// </summary>
    public int SerializedVersion { get; }

    /// <summary>
    /// Gets the current version of the component in the runtime.
    /// </summary>
    public int CurrentVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentVersionException"/> class.
    /// </summary>
    /// <param name="componentName">The name of the component that had a version mismatch.</param>
    /// <param name="serializedVersion">The version of the component that was serialized.</param>
    /// <param name="currentVersion">The current version of the component in the runtime.</param>
    public ComponentVersionException(string componentName, int serializedVersion, int currentVersion)
        : base(CreateMessage(componentName, serializedVersion, currentVersion))
    {
        ComponentName = componentName;
        SerializedVersion = serializedVersion;
        CurrentVersion = currentVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentVersionException"/> class.
    /// </summary>
    /// <param name="componentName">The name of the component that had a version mismatch.</param>
    /// <param name="serializedVersion">The version of the component that was serialized.</param>
    /// <param name="currentVersion">The current version of the component in the runtime.</param>
    /// <param name="message">A custom error message.</param>
    public ComponentVersionException(string componentName, int serializedVersion, int currentVersion, string message)
        : base(message)
    {
        ComponentName = componentName;
        SerializedVersion = serializedVersion;
        CurrentVersion = currentVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentVersionException"/> class.
    /// </summary>
    /// <param name="componentName">The name of the component that had a version mismatch.</param>
    /// <param name="serializedVersion">The version of the component that was serialized.</param>
    /// <param name="currentVersion">The current version of the component in the runtime.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ComponentVersionException(string componentName, int serializedVersion, int currentVersion, Exception innerException)
        : base(CreateMessage(componentName, serializedVersion, currentVersion), innerException)
    {
        ComponentName = componentName;
        SerializedVersion = serializedVersion;
        CurrentVersion = currentVersion;
    }

    private static string CreateMessage(string componentName, int serializedVersion, int currentVersion)
    {
        if (serializedVersion > currentVersion)
        {
            return $"Cannot load {componentName} v{serializedVersion} (current version is v{currentVersion}). " +
                   "The save file was created with a newer version of the application. " +
                   "Update the application to load this save file.";
        }

        return $"Cannot migrate {componentName} from v{serializedVersion} to v{currentVersion}. " +
               "No migration path is available for this version.";
    }
}
