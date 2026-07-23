using System.Numerics;
using KeenEyes.Common;
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
/// If a particle has a valid texture, it uses
/// <see cref="I2DRenderer.DrawTextureRotated(TextureHandle, in Rectangle, float, Vector2, Vector4?)"/>
/// (or the sprite-sheet overload when a texture sheet is configured).
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
            if (!World.TryGetExtension<ParticleManager>(out pm) || pm is null)
            {
                return;
            }
            manager = pm;
        }

        var r = renderer;
        if (r == null)
        {
            if (!World.TryGetExtension<I2DRenderer>(out r) || r is null)
            {
                return;
            }
            renderer = r;
        }

        // Group emitters by blend mode for efficient batching
        // We'll render transparent first, then additive (so glow effects overlay)
        var transparentEmitters = new List<RenderEntry>();
        var additiveEmitters = new List<RenderEntry>();
        var multiplyEmitters = new List<RenderEntry>();
        var premultipliedEmitters = new List<RenderEntry>();

        foreach (var entity in World.Query<ParticleEmitter>())
        {
            ref readonly var emitter = ref World.Get<ParticleEmitter>(entity);
            var pool = pm.GetPool(entity);
            if (pool == null || pool.ActiveCount == 0)
            {
                continue;
            }

            // Local-space particles are stored relative to the emitter, so translate them
            // by the emitter's current position at render time. World-space particles use
            // absolute coordinates and need no offset.
            var offset = emitter.Space == ParticleSpace.Local
                ? ResolveEmitterPosition(entity)
                : Vector2.Zero;
            var entry = new RenderEntry(pool, emitter, offset);

            switch (emitter.BlendMode)
            {
                case BlendMode.Transparent:
                    transparentEmitters.Add(entry);
                    break;
                case BlendMode.Additive:
                    additiveEmitters.Add(entry);
                    break;
                case BlendMode.Multiply:
                    multiplyEmitters.Add(entry);
                    break;
                case BlendMode.Premultiplied:
                    premultipliedEmitters.Add(entry);
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

    private static void RenderBatch(I2DRenderer renderer, List<RenderEntry> emitters)
    {
        if (emitters.Count == 0)
        {
            return;
        }

        // Calculate total particles for batch hint
        var totalParticles = 0;
        foreach (var entry in emitters)
        {
            totalParticles += entry.Pool.ActiveCount;
        }

        renderer.Begin();
        renderer.SetBatchHint(totalParticles);

        foreach (var entry in emitters)
        {
            RenderPool(renderer, entry.Pool, in entry.Emitter, entry.Offset);
        }

        renderer.End();
    }

    private static void RenderPool(I2DRenderer renderer, ParticlePool pool, in ParticleEmitter emitter, Vector2 offset)
    {
        var texture = emitter.Texture;
        var hasTexture = texture.IsValid;

        // A texture sheet is only active when the grid describes more than one frame.
        var columns = Math.Max(1, emitter.TextureSheetColumns);
        var rows = Math.Max(1, emitter.TextureSheetRows);
        var frameCount = columns * rows;
        var animated = hasTexture && frameCount > 1;

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
            var posX = pool.PositionsX[i] + offset.X;
            var posY = pool.PositionsY[i] + offset.Y;

            if (hasTexture)
            {
                var destRect = new Rectangle(
                    posX - halfSize,
                    posY - halfSize,
                    size,
                    size);

                if (animated)
                {
                    var sourceRect = FrameSourceRect(pool.NormalizedAges[i], columns, rows, frameCount);
                    renderer.DrawTextureRotated(
                        texture,
                        in destRect,
                        in sourceRect,
                        pool.Rotations[i],
                        new Vector2(0.5f, 0.5f),
                        color);
                }
                else
                {
                    renderer.DrawTextureRotated(
                        texture,
                        in destRect,
                        pool.Rotations[i],
                        new Vector2(0.5f, 0.5f),
                        color);
                }
            }
            else
            {
                // Fallback: draw as filled circle
                renderer.FillCircle(
                    posX,
                    posY,
                    halfSize,
                    color,
                    segments: 8);
            }
        }
    }

    /// <summary>
    /// Computes the UV sub-rectangle for the sprite-sheet frame at the given normalized age.
    /// </summary>
    private static Rectangle FrameSourceRect(float normalizedAge, int columns, int rows, int frameCount)
    {
        var frame = (int)(normalizedAge * frameCount);
        if (frame < 0)
        {
            frame = 0;
        }
        else if (frame >= frameCount)
        {
            frame = frameCount - 1;
        }

        var col = frame % columns;
        var row = frame / columns;
        var frameWidth = 1f / columns;
        var frameHeight = 1f / rows;

        return new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
    }

    private Vector2 ResolveEmitterPosition(Entity entity)
    {
        if (World.Has<Transform2D>(entity))
        {
            ref readonly var transform = ref World.Get<Transform2D>(entity);
            return transform.Position;
        }

        if (World.Has<Transform3D>(entity))
        {
            ref readonly var transform3D = ref World.Get<Transform3D>(entity);
            return new Vector2(transform3D.Position.X, transform3D.Position.Y);
        }

        return Vector2.Zero;
    }

    private readonly struct RenderEntry(ParticlePool pool, ParticleEmitter emitter, Vector2 offset)
    {
        public readonly ParticlePool Pool = pool;
        public readonly ParticleEmitter Emitter = emitter;
        public readonly Vector2 Offset = offset;
    }
}
