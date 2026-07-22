using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;
using KeenEyes.Particles.Systems;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for age-driven texture-sheet (sprite) animation in <see cref="ParticleRenderSystem"/>.
/// </summary>
public class ParticleTextureSheetTests : IDisposable
{
    private World? world;
    private Mock2DRenderer? mockRenderer;

    public void Dispose()
    {
        mockRenderer?.Dispose();
        world?.Dispose();
    }

    /// <summary>
    /// Sets up a render-only world with a single manually-controlled particle at the
    /// given normalized age, so the exact sprite-sheet frame can be asserted.
    /// </summary>
    private ParticleManager SetupSingleParticle(ParticleEmitter emitter, float normalizedAge)
    {
        world = new World();

        var manager = new ParticleManager(world, new ParticlesConfig());
        world.SetExtension(manager);

        mockRenderer = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(mockRenderer);

        world.AddSystem(new ParticleRenderSystem(), SystemPhase.Render);

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        ref var stored = ref world.Get<ParticleEmitter>(entity);
        manager.RegisterEmitter(entity, in stored);

        var pool = manager.GetPool(entity)!;
        var idx = pool.Allocate();
        pool.Sizes[idx] = 16f;
        pool.ColorsA[idx] = 1f;
        pool.NormalizedAges[idx] = normalizedAge;

        return manager;
    }

    #region Frame Progression Tests

    [Theory]
    [InlineData(0.0f, 0f)]    // Frame 0 -> column 0
    [InlineData(0.25f, 0.25f)] // Frame 1 -> column 1
    [InlineData(0.5f, 0.5f)]  // Frame 2 -> column 2
    [InlineData(0.75f, 0.75f)] // Frame 3 -> column 3
    [InlineData(0.99f, 0.75f)] // Still frame 3 (clamped by truncation)
    [InlineData(1.0f, 0.75f)]  // Age at end clamps to the final frame
    public void SpriteSheet_HorizontalStrip_FrameAdvancesWithAge(float normalizedAge, float expectedU)
    {
        var emitter = ParticleEmitter.Default with
        {
            Texture = new TextureHandle(1),
            TextureSheetColumns = 4,
            TextureSheetRows = 1
        };

        SetupSingleParticle(emitter, normalizedAge);
        world!.Update(1f / 60f);

        var cmd = Assert.Single(mockRenderer!.Commands.OfType<DrawTextureRotatedRegionCommand>());
        Assert.True(cmd.SourceRect.X.ApproximatelyEquals(expectedU, 0.0001f),
            $"Expected frame U {expectedU}, got {cmd.SourceRect.X}");
        Assert.True(cmd.SourceRect.Y.IsApproximatelyZero(0.0001f));
        Assert.True(cmd.SourceRect.Width.ApproximatelyEquals(0.25f, 0.0001f));
        Assert.True(cmd.SourceRect.Height.ApproximatelyEquals(1f, 0.0001f));
    }

    [Theory]
    [InlineData(0.0f, 0f, 0f)]    // Frame 0 -> col 0, row 0
    [InlineData(0.25f, 0.5f, 0f)] // Frame 1 -> col 1, row 0
    [InlineData(0.5f, 0f, 0.5f)]  // Frame 2 -> col 0, row 1
    [InlineData(0.75f, 0.5f, 0.5f)] // Frame 3 -> col 1, row 1
    public void SpriteSheet_Grid_FrameMapsToColumnAndRow(float normalizedAge, float expectedU, float expectedV)
    {
        var emitter = ParticleEmitter.Default with
        {
            Texture = new TextureHandle(1),
            TextureSheetColumns = 2,
            TextureSheetRows = 2
        };

        SetupSingleParticle(emitter, normalizedAge);
        world!.Update(1f / 60f);

        var cmd = Assert.Single(mockRenderer!.Commands.OfType<DrawTextureRotatedRegionCommand>());
        Assert.True(cmd.SourceRect.X.ApproximatelyEquals(expectedU, 0.0001f),
            $"Expected frame U {expectedU}, got {cmd.SourceRect.X}");
        Assert.True(cmd.SourceRect.Y.ApproximatelyEquals(expectedV, 0.0001f),
            $"Expected frame V {expectedV}, got {cmd.SourceRect.Y}");
        Assert.True(cmd.SourceRect.Width.ApproximatelyEquals(0.5f, 0.0001f));
        Assert.True(cmd.SourceRect.Height.ApproximatelyEquals(0.5f, 0.0001f));
    }

    #endregion

    #region Single Frame Tests

    [Fact]
    public void SpriteSheet_SingleFrame_UsesPlainRotatedTexture()
    {
        var emitter = ParticleEmitter.Default with
        {
            Texture = new TextureHandle(1),
            TextureSheetColumns = 1,
            TextureSheetRows = 1
        };

        SetupSingleParticle(emitter, 0.5f);
        world!.Update(1f / 60f);

        // A 1x1 sheet is not animated: it must use the plain rotated-texture path (no source rect).
        Assert.Single(mockRenderer!.Commands.OfType<DrawTextureRotatedCommand>());
        Assert.Empty(mockRenderer.Commands.OfType<DrawTextureRotatedRegionCommand>());
    }

    [Fact]
    public void SpriteSheet_UnsetGrid_BehavesAsSingleFrame()
    {
        // Zero columns/rows (struct default) must behave exactly like a plain textured particle.
        var emitter = ParticleEmitter.Default with
        {
            Texture = new TextureHandle(1),
            TextureSheetColumns = 0,
            TextureSheetRows = 0
        };

        SetupSingleParticle(emitter, 0.5f);
        world!.Update(1f / 60f);

        Assert.Single(mockRenderer!.Commands.OfType<DrawTextureRotatedCommand>());
        Assert.Empty(mockRenderer.Commands.OfType<DrawTextureRotatedRegionCommand>());
    }

    #endregion
}
