using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Systems;

/// <summary>
/// System that updates all active particles.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the Update phase after <see cref="ParticleSpawnSystem"/>
/// and handles:
/// <list type="bullet">
///   <item><description>Aging particles and killing expired ones</description></item>
///   <item><description>Applying modifiers (gravity, velocity, color, size, rotation)</description></item>
///   <item><description>Updating particle positions based on velocity</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ParticleUpdateSystem : SystemBase
{
    private ParticleManager? manager;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<ParticleManager>(out var pm))
        {
            manager = pm;
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

        foreach (var entity in World.Query<ParticleEmitter>())
        {
            var pool = pm.GetPool(entity);
            if (pool == null || pool.ActiveCount == 0)
            {
                continue;
            }

            // Get modifiers if present
            var hasModifiers = World.Has<ParticleEmitterModifiers>(entity);
            ParticleEmitterModifiers modifiers = default;
            if (hasModifiers)
            {
                modifiers = World.Get<ParticleEmitterModifiers>(entity);
            }

            // Update all particles
            UpdateParticles(pool, deltaTime, hasModifiers, in modifiers);
        }
    }

    private static void UpdateParticles(ParticlePool pool, float deltaTime, bool hasModifiers, in ParticleEmitterModifiers modifiers)
    {
        for (var i = pool.Capacity - 1; i >= 0; i--)
        {
            if (!pool.Alive[i])
            {
                continue;
            }

            // Update age
            pool.Ages[i] += deltaTime;
            var lifetime = pool.Lifetimes[i];
            pool.NormalizedAges[i] = lifetime > 0 ? pool.Ages[i] / lifetime : 1f;

            // Check death
            if (pool.Ages[i] >= lifetime)
            {
                pool.Release(i);
                continue;
            }

            // Apply modifiers
            if (hasModifiers)
            {
                ApplyModifiers(pool, i, deltaTime, in modifiers);
            }

            // Update position based on velocity
            pool.PositionsX[i] += pool.VelocitiesX[i] * deltaTime;
            pool.PositionsY[i] += pool.VelocitiesY[i] * deltaTime;

            // Update rotation
            pool.Rotations[i] += pool.RotationSpeeds[i] * deltaTime;
        }
    }

    private static void ApplyModifiers(ParticlePool pool, int i, float deltaTime, in ParticleEmitterModifiers modifiers)
    {
        var normalizedAge = pool.NormalizedAges[i];

        // Apply gravity
        if (modifiers.HasGravity)
        {
            pool.VelocitiesX[i] += modifiers.GravityX * deltaTime;
            pool.VelocitiesY[i] += modifiers.GravityY * deltaTime;

            // Apply drag
            if (modifiers.Drag > 0)
            {
                var dragFactor = 1f - modifiers.Drag * deltaTime;
                if (dragFactor < 0)
                {
                    dragFactor = 0;
                }
                pool.VelocitiesX[i] *= dragFactor;
                pool.VelocitiesY[i] *= dragFactor;
            }
        }

        // Apply velocity over lifetime
        if (modifiers.HasVelocityOverLifetime)
        {
            var multiplier = modifiers.VelocityCurve.Evaluate(normalizedAge);
            // Store the base speed and apply multiplier
            // Note: This is a simplified approach - for accurate velocity scaling,
            // we'd need to store the initial velocity magnitude
            pool.VelocitiesX[i] *= multiplier;
            pool.VelocitiesY[i] *= multiplier;
        }

        // Apply size over lifetime
        if (modifiers.HasSizeOverLifetime)
        {
            var multiplier = modifiers.SizeCurve.Evaluate(normalizedAge);
            pool.Sizes[i] = pool.InitialSizes[i] * multiplier;
        }

        // Apply color over lifetime
        if (modifiers.HasColorOverLifetime)
        {
            var color = modifiers.ColorGradient.Evaluate(normalizedAge);
            pool.ColorsR[i] = color.X;
            pool.ColorsG[i] = color.Y;
            pool.ColorsB[i] = color.Z;
            pool.ColorsA[i] = color.W;
        }

        // Apply rotation over lifetime
        if (modifiers.HasRotationOverLifetime)
        {
            var rotationMultiplier = modifiers.RotationCurve.Evaluate(normalizedAge);
            pool.RotationSpeeds[i] = modifiers.RotationSpeed * rotationMultiplier;
        }
    }
}
