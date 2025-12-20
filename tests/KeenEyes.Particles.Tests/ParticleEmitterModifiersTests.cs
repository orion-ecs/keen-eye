using System.Numerics;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticleEmitterModifiers struct.
/// </summary>
public class ParticleEmitterModifiersTests
{
    #region None Static Property Tests

    [Fact]
    public void None_HasNoGravity()
    {
        var modifiers = ParticleEmitterModifiers.None;

        Assert.False(modifiers.HasGravity);
        Assert.Equal(0f, modifiers.GravityX);
        Assert.Equal(0f, modifiers.GravityY);
        Assert.Equal(0f, modifiers.Drag);
    }

    [Fact]
    public void None_HasNoVelocityOverLifetime()
    {
        var modifiers = ParticleEmitterModifiers.None;

        Assert.False(modifiers.HasVelocityOverLifetime);
        // Velocity curve should be constant 1
        Assert.Equal(1f, modifiers.VelocityCurve.Evaluate(0.5f), 4);
    }

    [Fact]
    public void None_HasNoSizeOverLifetime()
    {
        var modifiers = ParticleEmitterModifiers.None;

        Assert.False(modifiers.HasSizeOverLifetime);
        // Size curve should be constant 1
        Assert.Equal(1f, modifiers.SizeCurve.Evaluate(0.5f), 4);
    }

    [Fact]
    public void None_HasNoColorOverLifetime()
    {
        var modifiers = ParticleEmitterModifiers.None;

        Assert.False(modifiers.HasColorOverLifetime);
        // Color gradient should be constant white
        Assert.Equal(Vector4.One, modifiers.ColorGradient.Evaluate(0.5f));
    }

    [Fact]
    public void None_HasNoRotationOverLifetime()
    {
        var modifiers = ParticleEmitterModifiers.None;

        Assert.False(modifiers.HasRotationOverLifetime);
        Assert.Equal(0f, modifiers.RotationSpeed);
        // Rotation curve should be constant 1
        Assert.Equal(1f, modifiers.RotationCurve.Evaluate(0.5f), 4);
    }

    #endregion

    #region WithGravity Static Method Tests

    [Fact]
    public void WithGravity_SetsHasGravityTrue()
    {
        var modifiers = ParticleEmitterModifiers.WithGravity(100f);

        Assert.True(modifiers.HasGravity);
    }

    [Fact]
    public void WithGravity_SetsGravityY()
    {
        var modifiers = ParticleEmitterModifiers.WithGravity(150f);

        Assert.Equal(150f, modifiers.GravityY);
    }

    [Fact]
    public void WithGravity_GravityXRemainsZero()
    {
        var modifiers = ParticleEmitterModifiers.WithGravity(100f);

        Assert.Equal(0f, modifiers.GravityX);
    }

    [Fact]
    public void WithGravity_WithDrag_SetsDrag()
    {
        var modifiers = ParticleEmitterModifiers.WithGravity(100f, drag: 0.5f);

        Assert.Equal(0.5f, modifiers.Drag);
    }

    [Fact]
    public void WithGravity_DefaultDrag_IsZero()
    {
        var modifiers = ParticleEmitterModifiers.WithGravity(100f);

        Assert.Equal(0f, modifiers.Drag);
    }

    [Fact]
    public void WithGravity_OtherModifiersRemainDisabled()
    {
        var modifiers = ParticleEmitterModifiers.WithGravity(100f);

        Assert.False(modifiers.HasVelocityOverLifetime);
        Assert.False(modifiers.HasSizeOverLifetime);
        Assert.False(modifiers.HasColorOverLifetime);
        Assert.False(modifiers.HasRotationOverLifetime);
    }

    #endregion

    #region WithFadeOut Static Method Tests

    [Fact]
    public void WithFadeOut_SetsHasColorOverLifetimeTrue()
    {
        var color = new Vector4(1f, 0.5f, 0f, 1f);
        var modifiers = ParticleEmitterModifiers.WithFadeOut(color);

        Assert.True(modifiers.HasColorOverLifetime);
    }

    [Fact]
    public void WithFadeOut_SetsColorGradient()
    {
        var color = new Vector4(1f, 0.5f, 0f, 1f);
        var modifiers = ParticleEmitterModifiers.WithFadeOut(color);

        // Start should have full alpha
        var start = modifiers.ColorGradient.Evaluate(0f);
        Assert.Equal(1f, start.W, 2);

        // End should have zero alpha
        var end = modifiers.ColorGradient.Evaluate(1f);
        Assert.Equal(0f, end.W, 2);
    }

    [Fact]
    public void WithFadeOut_PreservesRgbComponents()
    {
        var color = new Vector4(0.8f, 0.6f, 0.4f, 1f);
        var modifiers = ParticleEmitterModifiers.WithFadeOut(color);

        // RGB should be preserved throughout
        var mid = modifiers.ColorGradient.Evaluate(0.5f);
        Assert.Equal(0.8f, mid.X, 2);
        Assert.Equal(0.6f, mid.Y, 2);
        Assert.Equal(0.4f, mid.Z, 2);
    }

    [Fact]
    public void WithFadeOut_OtherModifiersRemainDisabled()
    {
        var color = new Vector4(1f, 1f, 1f, 1f);
        var modifiers = ParticleEmitterModifiers.WithFadeOut(color);

        Assert.False(modifiers.HasGravity);
        Assert.False(modifiers.HasVelocityOverLifetime);
        Assert.False(modifiers.HasSizeOverLifetime);
        Assert.False(modifiers.HasRotationOverLifetime);
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void Default_HasAllModifiersDisabled()
    {
        ParticleEmitterModifiers modifiers = default;

        Assert.False(modifiers.HasGravity);
        Assert.False(modifiers.HasVelocityOverLifetime);
        Assert.False(modifiers.HasSizeOverLifetime);
        Assert.False(modifiers.HasColorOverLifetime);
        Assert.False(modifiers.HasRotationOverLifetime);
    }

    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var original = ParticleEmitterModifiers.None;
        var modified = original with { HasGravity = true, GravityY = 50f };

        Assert.False(original.HasGravity);
        Assert.True(modified.HasGravity);
        Assert.Equal(50f, modified.GravityY);
    }

    #endregion
}
