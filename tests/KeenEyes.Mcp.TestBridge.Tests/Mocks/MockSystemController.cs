using KeenEyes.TestBridge.Systems;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ISystemController for testing MCP tools.
/// </summary>
internal sealed class MockSystemController : ISystemController
{
    public List<SystemSnapshot> Systems { get; } = [];

    private static SystemSnapshot Make(string name, bool enabled) => new()
    {
        Name = name,
        TypeName = name,
        Enabled = enabled,
        Phase = "Update",
        Order = 0
    };

    public Task<IReadOnlyList<SystemSnapshot>> GetSystemsAsync()
        => Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems);

    public Task<int> GetCountAsync() => Task.FromResult(Systems.Count);

    public Task<SystemSnapshot?> GetSystemAsync(string name)
        => Task.FromResult(Systems.Find(s => s.Name == name));

    public Task<SystemSnapshot> EnableSystemAsync(string name) => Task.FromResult(Make(name, true));

    public Task<SystemSnapshot> DisableSystemAsync(string name) => Task.FromResult(Make(name, false));

    public Task<SystemSnapshot> ToggleSystemAsync(string name) => Task.FromResult(Make(name, true));

    public Task<IReadOnlyList<SystemSnapshot>> GetSystemsByPhaseAsync(string phase)
        => Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems.FindAll(s => s.Phase == phase));

    public Task<IReadOnlyList<SystemSnapshot>> GetEnabledSystemsAsync()
        => Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems.FindAll(s => s.Enabled));

    public Task<IReadOnlyList<SystemSnapshot>> GetDisabledSystemsAsync()
        => Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems.FindAll(s => !s.Enabled));
}
