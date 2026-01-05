using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Systems;

/// <summary>
/// System that processes asynchronous path requests.
/// </summary>
/// <remarks>
/// <para>
/// This system runs early in the Update phase to process completed path requests
/// before agent movement systems run. It:
/// </para>
/// <list type="bullet">
/// <item><description>Updates the navigation provider to process pending requests</description></item>
/// <item><description>Checks for completed path requests and updates agent components</description></item>
/// <item><description>Initializes agent navigation state when paths are ready</description></item>
/// </list>
/// </remarks>
internal sealed class PathRequestSystem : SystemBase
{
    private NavigationContext? context;
    private INavigationProvider? provider;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension<NavigationContext>(out context))
        {
            throw new InvalidOperationException("PathRequestSystem requires NavigationContext extension.");
        }

        if (!World.TryGetExtension<INavigationProvider>(out provider))
        {
            throw new InvalidOperationException("PathRequestSystem requires INavigationProvider extension.");
        }
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (context == null || provider == null)
        {
            return;
        }

        // Update the navigation provider to process pending path requests
        provider.Update(deltaTime);

        // Process completed requests and update agent components
        context.ProcessPendingRequests();
    }
}
