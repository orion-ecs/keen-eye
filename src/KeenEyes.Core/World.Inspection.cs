using KeenEyes.Capabilities;

namespace KeenEyes;

public sealed partial class World
{
    #region IInspectionCapability Implementation

    // GetName and HasComponent are already implemented in World.Entities.cs
    // and match the IInspectionCapability interface signatures.

    /// <inheritdoc />
    public IEnumerable<RegisteredComponentInfo> GetRegisteredComponents()
    {
        foreach (var info in Components.All)
        {
            yield return new RegisteredComponentInfo(
                Type: info.Type,
                Name: info.Type.Name,
                Size: info.Size,
                IsTag: info.IsTag);
        }
    }

    #endregion
}
