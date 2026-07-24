using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Toggles all juice on and off with the J key — the A/B readability demo.
/// Every juice system checks <see cref="JuiceConfig.Enabled"/>; flipping one
/// bool turns NOVAFALL back into Phase A's flat build, live, mid-run.
/// </summary>
/// <remarks>
/// Edge-detects the key (down this frame, up last frame) so one press toggles
/// exactly once. Without an input context (headless mode) it does nothing.
/// </remarks>
public sealed class JuiceToggleSystem : SystemBase
{
    private bool wasDown;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (!World.TryGetExtension<IInputContext>(out var input))
        {
            return;
        }

        var isDown = input.Keyboard.IsKeyDown(Key.J);
        if (isDown && !wasDown)
        {
            ref var juice = ref World.GetSingleton<JuiceConfig>();
            juice.Enabled = !juice.Enabled;
            Console.WriteLine(juice.Enabled
                ? "Juice ON — trail, particles, shake, palette, audio layers."
                : "Juice OFF — flat Phase A readability baseline.");
        }

        wasDown = isDown;
    }
}
