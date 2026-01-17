using KeenEyes.TestBridge.UI;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IUIController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteUIController(TestBridgeClient client) : IUIController
{
    #region Statistics

    /// <inheritdoc />
    public async Task<UIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<UIStatisticsSnapshot>(
            "ui.getStatistics",
            null,
            cancellationToken) ?? new UIStatisticsSnapshot
            {
                TotalElementCount = 0,
                VisibleElementCount = 0,
                InteractableCount = 0,
                FocusedElementId = null
            };
    }

    #endregion

    #region Focus Management

    /// <inheritdoc />
    public async Task<int?> GetFocusedElementAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<int?>(
            "ui.getFocused",
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetFocusAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ui.setFocus",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ClearFocusAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ui.clearFocus",
            null,
            cancellationToken);
    }

    #endregion

    #region Element Inspection

    /// <inheritdoc />
    public async Task<UIElementSnapshot?> GetElementAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<UIElementSnapshot?>(
            "ui.getElement",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UIElementSnapshot>> GetElementTreeAsync(int? rootEntityId = null, CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<UIElementSnapshot[]>(
            "ui.getTree",
            rootEntityId.HasValue ? new { rootEntityId } : null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetRootElementsAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "ui.getRoots",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<UIBoundsSnapshot?> GetElementBoundsAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<UIBoundsSnapshot?>(
            "ui.getBounds",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UIStyleSnapshot?> GetElementStyleAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<UIStyleSnapshot?>(
            "ui.getStyle",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UIInteractionSnapshot?> GetInteractionStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<UIInteractionSnapshot?>(
            "ui.getInteraction",
            new { entityId },
            cancellationToken);
    }

    #endregion

    #region Hit Testing

    /// <inheritdoc />
    public async Task<int?> HitTestAsync(float x, float y, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<int?>(
            "ui.hitTest",
            new { x, y },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> HitTestAllAsync(float x, float y, CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "ui.hitTestAll",
            new { x, y },
            cancellationToken);
        return result ?? [];
    }

    #endregion

    #region Element Search

    /// <inheritdoc />
    public async Task<int?> FindElementByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<int?>(
            "ui.findByName",
            new { name },
            cancellationToken);
    }

    #endregion

    #region Interaction Simulation

    /// <inheritdoc />
    public async Task<bool> SimulateClickAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ui.simulateClick",
            new { entityId },
            cancellationToken);
    }

    #endregion
}
