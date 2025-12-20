using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Systems;

/// <summary>
/// System that renders all particles.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the Render phase and draws all active particles
/// using the 2D renderer. Particles are grouped by blend mode for efficient
/// batching.
/// </para>
/// <para>
/// If a particle has a valid texture, it uses <see cref="I2DRenderer.DrawTextureRotated"/>.
/// Otherwise, it falls back to <see cref="I2DRenderer.FillCircle"/>.
/// </para>
/// </remarks>
public sealed class ParticleRenderSystem : SystemBase
{
    private ParticleManager? manager;
    private I2DRenderer? renderer;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<ParticleManager>(out var pm))
        {
            manager = pm;
        }

        if (World.TryGetExtension<I2DRenderer>(out var r))
        {
            renderer = r;
        }
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        var pm = manager;
        if (pm == null)
        {
            if (!World.TryGetExtension(out pm) || pm is null)
            {
                return;
            }
            manager = pm;
        }

        var r = renderer;
        if (r == null)
        {
            if (!World.TryGetExtension(out r) || r is null)
            {
                return;
            }
            renderer = r;
        }

        // Group emitters by blend mode for efficient batching
        // We'll render transparent first, then additive (so glow effects overlay)
        var transparentEmitters = new List<(Entity, ParticlePool, ParticleEmitter)>();
        var additiveEmitters = new List<(Entity, ParticlePool, ParticleEmitter)>();
        var multiplyEmitters = new List<(Entity, ParticlePool, ParticleEmitter)>();
        var premultipliedEmitters = new List<(Entity, ParticlePool, ParticleEmitter)>();

        foreach (var entity in World.Query<ParticleEmitter>())
        {
            ref readonly var emitter = ref World.Get<ParticleEmitter>(entity);
            var pool = pm.GetPool(entity);
            if (pool == null || pool.ActiveCount == 0)
            {
                continue;
            }

            switch (emitter.BlendMode)
            {
                case BlendMode.Transparent:
                    transparentEmitters.Add((entity, pool, emitter));
                    break;
                case BlendMode.Additive:
                    additiveEmitters.Add((entity, pool, emitter));
                    break;
                case BlendMode.Multiply:
                    multiplyEmitters.Add((entity, pool, emitter));
                    break;
                case BlendMode.Premultiplied:
                    premultipliedEmitters.Add((entity, pool, emitter));
                    break;
            }
        }

        // Render in order: multiply -> transparent -> premultiplied -> additive
        // (This is a common ordering but can be adjusted based on desired visual results)
        RenderBatch(r, multiplyEmitters);
        RenderBatch(r, transparentEmitters);
        RenderBatch(r, premultipliedEmitters);
        RenderBatch(r, additiveEmitters);
    }

    private static void RenderBatch(I2DRenderer renderer, List<(Entity, ParticlePool, ParticleEmitter)> emitters)
    {
        if (emitters.Count == 0)
        {
            return;
        }

        // Calculate total particles for batch hint
        var totalParticles = 0;
        foreach (var (_, pool, _) in emitters)
        {
            totalParticles += pool.ActiveCount;
        }

        renderer.Begin();
        renderer.SetBatchHint(totalParticles);

        foreach (var (_, pool, emitter) in emitters)
        {
            RenderPool(renderer, pool, emitter.Texture);
        }

        renderer.End();
    }

    private static void RenderPool(I2DRenderer renderer, ParticlePool pool, TextureHandle texture)
    {
        var hasTexture = texture.IsValid;

        for (var i = 0; i < pool.Capacity; i++)
        {
            if (!pool.Alive[i])
            {
                continue;
            }

            var color = new Vector4(
                pool.ColorsR[i],
                pool.ColorsG[i],
                pool.ColorsB[i],
                pool.ColorsA[i]);

            var size = pool.Sizes[i];
            var halfSize = size / 2f;

            if (hasTexture)
            {
                var destRect = new Rectangle(
                    pool.PositionsX[i] - halfSize,
                    pool.PositionsY[i] - halfSize,
                    size,
                    size);

                renderer.DrawTextureRotated(
                    texture,
                    in destRect,
                    pool.Rotations[i],
                    new Vector2(0.5f, 0.5f),
                    color);
            }
            else
            {
                // Fallback: draw as filled circle
                renderer.FillCircle(
                    pool.PositionsX[i],
                    pool.PositionsY[i],
                    halfSize,
                    color,
                    segments: 8);
            }
        }
    }
}
