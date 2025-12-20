using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="IPersistenceCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock tracks save directory changes and provides methods to verify
/// persistence configuration in tests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mockPersistence = new MockPersistenceCapability();
/// mockContext.SetCapability&lt;IPersistenceCapability&gt;(mockPersistence);
///
/// plugin.Install(mockContext);
///
/// // Verify save directory was configured
/// Assert.True(mockPersistence.WasSaveDirectorySet);
/// Assert.Equal("/path/to/saves", mockPersistence.SaveDirectory);
/// </code>
/// </example>
public sealed class MockPersistenceCapability : IPersistenceCapability
{
    private const string DefaultSaveDirectory = "./saves";
    private string saveDirectory = DefaultSaveDirectory;
    private readonly List<string> saveDirectoryHistory = [];

    /// <summary>
    /// Gets or sets the save directory.
    /// </summary>
    public string SaveDirectory
    {
        get => saveDirectory;
        set
        {
            saveDirectory = value;
            saveDirectoryHistory.Add(value);
        }
    }

    /// <summary>
    /// Gets whether the save directory was set at least once.
    /// </summary>
    public bool WasSaveDirectorySet => saveDirectoryHistory.Count > 0;

    /// <summary>
    /// Gets the number of times the save directory was set.
    /// </summary>
    public int SaveDirectorySetCount => saveDirectoryHistory.Count;

    /// <summary>
    /// Gets the history of save directory values that were set.
    /// </summary>
    public IReadOnlyList<string> SaveDirectoryHistory => saveDirectoryHistory;

    /// <summary>
    /// Resets the mock to its initial state.
    /// </summary>
    public void Reset()
    {
        saveDirectory = DefaultSaveDirectory;
        saveDirectoryHistory.Clear();
    }
}
