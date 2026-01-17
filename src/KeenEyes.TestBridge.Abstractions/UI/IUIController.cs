namespace KeenEyes.TestBridge.UI;

/// <summary>
/// Controller interface for UI debugging and inspection operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides access to UI state including element hierarchy,
/// focus management, hit testing, and interaction simulation. It enables
/// inspection and manipulation of UI components for debugging and testing.
/// </para>
/// <para>
/// <strong>Note:</strong> Requires the UIPlugin to be installed on the world
/// for full functionality.
/// </para>
/// </remarks>
public interface IUIController
{
    #region Statistics

    /// <summary>
    /// Gets statistics about UI element usage in the world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics about UI elements.</returns>
    Task<UIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Focus Management

    /// <summary>
    /// Gets the currently focused UI element.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity ID of the focused element, or null if none.</returns>
    Task<int?> GetFocusedElementAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets focus to a specific UI element.
    /// </summary>
    /// <param name="entityId">The entity ID to focus.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if focus was set successfully.</returns>
    Task<bool> SetFocusAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears focus from the currently focused element.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if focus was cleared.</returns>
    Task<bool> ClearFocusAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Element Inspection

    /// <summary>
    /// Gets information about a specific UI element.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The element snapshot, or null if the entity is not a UI element.</returns>
    Task<UIElementSnapshot?> GetElementAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the entire UI element tree starting from a root (or all roots if null).
    /// </summary>
    /// <param name="rootEntityId">The root entity ID, or null for all roots.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of UI elements in hierarchical order.</returns>
    Task<IReadOnlyList<UIElementSnapshot>> GetElementTreeAsync(int? rootEntityId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all root UI elements (canvases).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of root element entity IDs.</returns>
    Task<IReadOnlyList<int>> GetRootElementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the bounds of a UI element.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The element bounds, or null if the entity has no UIRect.</returns>
    Task<UIBoundsSnapshot?> GetElementBoundsAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the style of a UI element.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The element style, or null if the entity has no UIStyle.</returns>
    Task<UIStyleSnapshot?> GetElementStyleAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the interaction state of a UI element.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The interaction state, or null if the entity has no UIInteractable.</returns>
    Task<UIInteractionSnapshot?> GetInteractionStateAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion

    #region Hit Testing

    /// <summary>
    /// Finds the topmost UI element at a screen position.
    /// </summary>
    /// <param name="x">The X screen coordinate.</param>
    /// <param name="y">The Y screen coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity ID at the position, or null if none.</returns>
    Task<int?> HitTestAsync(float x, float y, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all UI elements at a screen position.
    /// </summary>
    /// <param name="x">The X screen coordinate.</param>
    /// <param name="y">The Y screen coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs at the position, sorted topmost first.</returns>
    Task<IReadOnlyList<int>> HitTestAllAsync(float x, float y, CancellationToken cancellationToken = default);

    #endregion

    #region Element Search

    /// <summary>
    /// Finds a UI element by name.
    /// </summary>
    /// <param name="name">The element name to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity ID of the first matching element, or null if not found.</returns>
    Task<int?> FindElementByNameAsync(string name, CancellationToken cancellationToken = default);

    #endregion

    #region Interaction Simulation

    /// <summary>
    /// Simulates a click on a UI element.
    /// </summary>
    /// <param name="entityId">The entity ID to click.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the click was simulated successfully.</returns>
    Task<bool> SimulateClickAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion
}
