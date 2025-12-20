using System.Numerics;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticleEffects factory class.
/// </summary>
public class ParticleEffectsTests
{
    #region Fire Effect Tests

    [Fact]
    public void Fire_ReturnsValidEmitter()
    {
        var emitter = ParticleEffects.Fire();

        Assert.True(emitter.EmissionRate > 0);
        Assert.True(emitter.LifetimeMin > 0);
        Assert.True(emitter.LifetimeMax >= emitter.LifetimeMin);
        Assert.True(emitter.StartSizeMin > 0);
        Assert.True(emitter.StartSpeedMin > 0);
        Assert.Equal(BlendMode.Additive, emitter.BlendMode);
        Assert.True(emitter.IsPlaying);
    }

    [Fact]
    public void Fire_HasUpwardConeShape()
    {
        var emitter = ParticleEffects.Fire();

        Assert.Equal(EmissionShapeType.Cone, emitter.Shape.Type);
        // Fire should emit upward (negative Y in screen coords)
        Assert.True(emitter.Shape.Direction.Y < 0);
    }

    [Fact]
    public void FireModifiers_HasGravity()
    {
        var modifiers = ParticleEffects.FireModifiers();

        Assert.True(modifiers.HasGravity);
        // Fire drifts upward
        Assert.True(modifiers.GravityY < 0);
    }

    [Fact]
    public void FireModifiers_HasColorOverLifetime()
    {
        var modifiers = ParticleEffects.FireModifiers();

        Assert.True(modifiers.HasColorOverLifetime);
    }

    [Fact]
    public void FireModifiers_HasSizeOverLifetime()
    {
        var modifiers = ParticleEffects.FireModifiers();

        Assert.True(modifiers.HasSizeOverLifetime);
    }

    #endregion

    #region Smoke Effect Tests

    [Fact]
    public void Smoke_ReturnsValidEmitter()
    {
        var emitter = ParticleEffects.Smoke();

        Assert.True(emitter.EmissionRate > 0);
        Assert.True(emitter.LifetimeMin > 0);
        Assert.Equal(BlendMode.Transparent, emitter.BlendMode);
        Assert.True(emitter.IsPlaying);
    }

    [Fact]
    public void Smoke_HasLongerLifetimeThanFire()
    {
        var fire = ParticleEffects.Fire();
        var smoke = ParticleEffects.Smoke();

        Assert.True(smoke.LifetimeMax > fire.LifetimeMax);
    }

    [Fact]
    public void SmokeModifiers_HasRotation()
    {
        var modifiers = ParticleEffects.SmokeModifiers();

        Assert.True(modifiers.HasRotationOverLifetime);
        Assert.True(modifiers.RotationSpeed > 0);
    }

    [Fact]
    public void SmokeModifiers_ExpandsOverLifetime()
    {
        var modifiers = ParticleEffects.SmokeModifiers();

        Assert.True(modifiers.HasSizeOverLifetime);
        // Smoke should expand - size at end should be larger than start
        var startSize = modifiers.SizeCurve.Evaluate(0f);
        var endSize = modifiers.SizeCurve.Evaluate(1f);
        Assert.True(endSize > startSize);
    }

    #endregion

    #region Explosion Effect Tests

    [Fact]
    public void Explosion_IsBurstEmitter()
    {
        var emitter = ParticleEffects.Explosion();

        Assert.Equal(0f, emitter.EmissionRate); // No continuous emission
        Assert.True(emitter.BurstCount > 0);
        Assert.Equal(0f, emitter.BurstInterval); // One-shot
    }

    [Fact]
    public void Explosion_HasRadialShape()
    {
        var emitter = ParticleEffects.Explosion();

        Assert.Equal(EmissionShapeType.Sphere, emitter.Shape.Type);
    }

    [Fact]
    public void Explosion_HasHighSpeed()
    {
        var emitter = ParticleEffects.Explosion();
        var fire = ParticleEffects.Fire();

        Assert.True(emitter.StartSpeedMin > fire.StartSpeedMin);
    }

    [Fact]
    public void ExplosionModifiers_HasVelocityOverLifetime()
    {
        var modifiers = ParticleEffects.ExplosionModifiers();

        Assert.True(modifiers.HasVelocityOverLifetime);
        // Velocity should slow down over time
        var startVel = modifiers.VelocityCurve.Evaluate(0f);
        var endVel = modifiers.VelocityCurve.Evaluate(1f);
        Assert.True(endVel < startVel);
    }

    [Fact]
    public void ExplosionModifiers_HasHighDrag()
    {
        var modifiers = ParticleEffects.ExplosionModifiers();

        Assert.True(modifiers.HasGravity);
        Assert.True(modifiers.Drag > 1f); // High drag
    }

    #endregion

    #region Magic Sparkles Effect Tests

    [Fact]
    public void MagicSparkles_ReturnsValidEmitter()
    {
        var emitter = ParticleEffects.MagicSparkles();

        Assert.True(emitter.EmissionRate > 0);
        Assert.Equal(BlendMode.Additive, emitter.BlendMode);
        Assert.Equal(EmissionShapeType.Sphere, emitter.Shape.Type);
    }

    [Fact]
    public void MagicSparklesModifiers_HasFastRotation()
    {
        var modifiers = ParticleEffects.MagicSparklesModifiers();
        var smoke = ParticleEffects.SmokeModifiers();

        Assert.True(modifiers.HasRotationOverLifetime);
        Assert.True(modifiers.RotationSpeed > smoke.RotationSpeed);
    }

    [Fact]
    public void MagicSparklesModifiers_HasColorShift()
    {
        var modifiers = ParticleEffects.MagicSparklesModifiers();

        Assert.True(modifiers.HasColorOverLifetime);
        // Color should change over lifetime (not just fade)
        var startColor = modifiers.ColorGradient.Evaluate(0f);
        var midColor = modifiers.ColorGradient.Evaluate(0.5f);
        // Start and mid should be different colors (not just alpha)
        Assert.NotEqual(startColor.X, midColor.X, 2);
    }

    #endregion

    #region Blood Splatter Effect Tests

    [Fact]
    public void BloodSplatter_IsBurstEmitter()
    {
        var emitter = ParticleEffects.BloodSplatter();

        Assert.Equal(0f, emitter.EmissionRate);
        Assert.True(emitter.BurstCount > 0);
    }

    [Fact]
    public void BloodSplatter_HasConeShape()
    {
        var emitter = ParticleEffects.BloodSplatter();

        Assert.Equal(EmissionShapeType.Cone, emitter.Shape.Type);
    }

    [Fact]
    public void BloodSplatterModifiers_HasStrongGravity()
    {
        var modifiers = ParticleEffects.BloodSplatterModifiers();

        Assert.True(modifiers.HasGravity);
        Assert.True(modifiers.GravityY > 100f); // Strong downward
    }

    #endregion

    #region Rain Effect Tests

    [Fact]
    public void Rain_HasHighEmissionRate()
    {
        var emitter = ParticleEffects.Rain();

        Assert.True(emitter.EmissionRate >= 100f);
    }

    [Fact]
    public void Rain_HasBoxShape()
    {
        var emitter = ParticleEffects.Rain();

        Assert.Equal(EmissionShapeType.Box, emitter.Shape.Type);
        Assert.True(emitter.Shape.Size.X > 0);
    }

    [Fact]
    public void Rain_HasNoRotation()
    {
        var emitter = ParticleEffects.Rain();

        Assert.Equal(0f, emitter.StartRotationMin);
        Assert.Equal(0f, emitter.StartRotationMax);
    }

    [Fact]
    public void RainModifiers_HasStrongDownwardGravity()
    {
        var modifiers = ParticleEffects.RainModifiers();

        Assert.True(modifiers.HasGravity);
        Assert.True(modifiers.GravityY > 400f);
    }

    #endregion

    #region Snow Effect Tests

    [Fact]
    public void Snow_HasLowerEmissionThanRain()
    {
        var rain = ParticleEffects.Rain();
        var snow = ParticleEffects.Snow();

        Assert.True(snow.EmissionRate < rain.EmissionRate);
    }

    [Fact]
    public void Snow_HasLongerLifetime()
    {
        var rain = ParticleEffects.Rain();
        var snow = ParticleEffects.Snow();

        Assert.True(snow.LifetimeMax > rain.LifetimeMax);
    }

    [Fact]
    public void SnowModifiers_HasGentleGravity()
    {
        var snowMods = ParticleEffects.SnowModifiers();
        var rainMods = ParticleEffects.RainModifiers();

        Assert.True(snowMods.GravityY < rainMods.GravityY);
    }

    [Fact]
    public void SnowModifiers_HasRotation()
    {
        var modifiers = ParticleEffects.SnowModifiers();

        Assert.True(modifiers.HasRotationOverLifetime);
    }

    #endregion

    #region General Validation Tests

    [Fact]
    public void AllEmitters_HaveValidLifetimes()
    {
        var emitters = new[]
        {
            ParticleEffects.Fire(),
            ParticleEffects.Smoke(),
            ParticleEffects.Explosion(),
            ParticleEffects.MagicSparkles(),
            ParticleEffects.BloodSplatter(),
            ParticleEffects.Rain(),
            ParticleEffects.Snow()
        };

        foreach (var emitter in emitters)
        {
            Assert.True(emitter.LifetimeMin > 0, "LifetimeMin should be positive");
            Assert.True(emitter.LifetimeMax >= emitter.LifetimeMin, "LifetimeMax should >= LifetimeMin");
        }
    }

    [Fact]
    public void AllEmitters_HaveValidSizes()
    {
        var emitters = new[]
        {
            ParticleEffects.Fire(),
            ParticleEffects.Smoke(),
            ParticleEffects.Explosion(),
            ParticleEffects.MagicSparkles(),
            ParticleEffects.BloodSplatter(),
            ParticleEffects.Rain(),
            ParticleEffects.Snow()
        };

        foreach (var emitter in emitters)
        {
            Assert.True(emitter.StartSizeMin > 0, "StartSizeMin should be positive");
            Assert.True(emitter.StartSizeMax >= emitter.StartSizeMin, "StartSizeMax should >= StartSizeMin");
        }
    }

    [Fact]
    public void AllEmitters_HaveValidSpeeds()
    {
        var emitters = new[]
        {
            ParticleEffects.Fire(),
            ParticleEffects.Smoke(),
            ParticleEffects.Explosion(),
            ParticleEffects.MagicSparkles(),
            ParticleEffects.BloodSplatter(),
            ParticleEffects.Rain(),
            ParticleEffects.Snow()
        };

        foreach (var emitter in emitters)
        {
            Assert.True(emitter.StartSpeedMin >= 0, "StartSpeedMin should be non-negative");
            Assert.True(emitter.StartSpeedMax >= emitter.StartSpeedMin, "StartSpeedMax should >= StartSpeedMin");
        }
    }

    #endregion
}
