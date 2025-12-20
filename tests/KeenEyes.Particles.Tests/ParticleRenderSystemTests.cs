using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;
using KeenEyes.Particles.Systems;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for ParticleRenderSystem coverage.
/// </summary>
public class ParticleRenderSystemTests : IDisposable
{
    private World? world;
    private Mock2DRenderer? mockRenderer;

    public void Dispose()
    {
        mockRenderer?.Dispose();
        world?.Dispose();
    }

    private void SetupWorldWithMockRenderer()
    {
        world = new World();
        mockRenderer = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(mockRenderer);
        world.InstallPlugin(new ParticlesPlugin());
    }

    #region Basic Rendering Tests

    [Fact]
    public void RenderSystem_WithMockRenderer_DrawsParticles()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(5, 1f) with
        {
            BlendMode = BlendMode.Transparent
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        // Should have drawn 5 particles as circles (no texture)
        var circleCommands = mockRenderer!.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Equal(5, circleCommands.Count);
    }

    [Fact]
    public void RenderSystem_WithoutTexture_UsesFillCircle()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(3, 1f) with
        {
            Texture = TextureHandle.Invalid, // No texture - must use Invalid, not default
            StartSizeMin = 20f,
            StartSizeMax = 20f,
            StartColor = new Vector4(1f, 0f, 0f, 1f)
        };
        world!.Spawn()
            .With(new Transform2D(new Vector2(100f, 100f), 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var circleCommands = mockRenderer!.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Equal(3, circleCommands.Count);

        // All circles should have the correct color (red)
        foreach (var cmd in circleCommands)
        {
            Assert.Equal(1f, cmd.Color.X); // Red
            Assert.Equal(0f, cmd.Color.Y); // Green
            Assert.Equal(0f, cmd.Color.Z); // Blue
        }
    }

    [Fact]
    public void RenderSystem_SetsBatchHint_WithCorrectCount()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(10, 1f);
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        // Batch hint should be set
        Assert.NotNull(mockRenderer!.BatchHint);
        Assert.Equal(10, mockRenderer.BatchHint);
    }

    [Fact]
    public void RenderSystem_CallsBeginAndEnd()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(1, 1f);
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        Assert.True(mockRenderer!.BeginCount > 0);
        Assert.True(mockRenderer.EndCount > 0);
        Assert.Equal(mockRenderer.BeginCount, mockRenderer.EndCount);
    }

    #endregion

    #region Blend Mode Tests

    [Fact]
    public void RenderSystem_TransparentBlend_DrawsParticles()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(5, 1f) with
        {
            BlendMode = BlendMode.Transparent
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        Assert.Equal(5, mockRenderer!.Commands.OfType<FillCircleCommand>().Count());
    }

    [Fact]
    public void RenderSystem_AdditiveBlend_DrawsParticles()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(3, 1f) with
        {
            BlendMode = BlendMode.Additive
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        Assert.Equal(3, mockRenderer!.Commands.OfType<FillCircleCommand>().Count());
    }

    [Fact]
    public void RenderSystem_MultiplyBlend_DrawsParticles()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(4, 1f) with
        {
            BlendMode = BlendMode.Multiply
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        Assert.Equal(4, mockRenderer!.Commands.OfType<FillCircleCommand>().Count());
    }

    [Fact]
    public void RenderSystem_PremultipliedBlend_DrawsParticles()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(6, 1f) with
        {
            BlendMode = BlendMode.Premultiplied
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        Assert.Equal(6, mockRenderer!.Commands.OfType<FillCircleCommand>().Count());
    }

    [Fact]
    public void RenderSystem_MultipleBlendModes_AllDrawn()
    {
        SetupWorldWithMockRenderer();

        // Create emitters with different blend modes
        world!.Spawn()
            .With(new Transform2D(new Vector2(0, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(2, 1f) with { BlendMode = BlendMode.Transparent })
            .Build();

        world.Spawn()
            .With(new Transform2D(new Vector2(100, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(3, 1f) with { BlendMode = BlendMode.Additive })
            .Build();

        world.Spawn()
            .With(new Transform2D(new Vector2(200, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(4, 1f) with { BlendMode = BlendMode.Multiply })
            .Build();

        world.Spawn()
            .With(new Transform2D(new Vector2(300, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f) with { BlendMode = BlendMode.Premultiplied })
            .Build();

        world.Update(1f / 60f);

        // Total: 2 + 3 + 4 + 5 = 14 circles
        Assert.Equal(14, mockRenderer!.Commands.OfType<FillCircleCommand>().Count());
    }

    [Fact]
    public void RenderSystem_BlendModeOrdering_MultiplyFirst_AdditiveLast()
    {
        SetupWorldWithMockRenderer();

        // Create emitters in reverse order to verify ordering is correct
        world!.Spawn()
            .With(new Transform2D(new Vector2(0, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(1, 1f) with
            {
                BlendMode = BlendMode.Additive,
                StartColor = new Vector4(0, 0, 1, 1) // Blue = Additive
            })
            .Build();

        world.Spawn()
            .With(new Transform2D(new Vector2(100, 0), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(1, 1f) with
            {
                BlendMode = BlendMode.Multiply,
                StartColor = new Vector4(1, 0, 0, 1) // Red = Multiply
            })
            .Build();

        world.Update(1f / 60f);

        var circles = mockRenderer!.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Equal(2, circles.Count);

        // Multiply (red) should be rendered before Additive (blue)
        // Order: Multiply -> Transparent -> Premultiplied -> Additive
        Assert.Equal(1f, circles[0].Color.X); // First is red (Multiply)
        Assert.Equal(1f, circles[1].Color.Z); // Second is blue (Additive)
    }

    #endregion

    #region Textured Particle Tests

    [Fact]
    public void RenderSystem_WithTexture_UsesDrawTextureRotated()
    {
        SetupWorldWithMockRenderer();

        // Create a valid texture handle
        var texture = new TextureHandle(1);
        var emitter = ParticleEmitter.Burst(3, 1f) with
        {
            Texture = texture,
            StartSizeMin = 32f,
            StartSizeMax = 32f
        };
        world!.Spawn()
            .With(new Transform2D(new Vector2(100f, 100f), 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        // Should use DrawTextureRotated instead of FillCircle
        var textureCommands = mockRenderer!.Commands.OfType<DrawTextureRotatedCommand>().ToList();
        Assert.Equal(3, textureCommands.Count);

        // No circle commands
        var circleCommands = mockRenderer.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Empty(circleCommands);
    }

    [Fact]
    public void RenderSystem_WithTexture_CorrectDestRect()
    {
        SetupWorldWithMockRenderer();

        var texture = new TextureHandle(1);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            Texture = texture,
            Shape = EmissionShape.Point,
            StartSizeMin = 40f,
            StartSizeMax = 40f,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var position = new Vector2(200f, 150f);
        world!.Spawn()
            .With(new Transform2D(position, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var textureCommands = mockRenderer!.Commands.OfType<DrawTextureRotatedCommand>().ToList();
        Assert.Single(textureCommands);

        var cmd = textureCommands[0];
        // Size should be 40
        Assert.Equal(40f, cmd.DestRect.Width);
        Assert.Equal(40f, cmd.DestRect.Height);
    }

    [Fact]
    public void RenderSystem_WithTexture_CorrectOrigin()
    {
        SetupWorldWithMockRenderer();

        var texture = new TextureHandle(1);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            Texture = texture
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var textureCommands = mockRenderer!.Commands.OfType<DrawTextureRotatedCommand>().ToList();
        Assert.Single(textureCommands);

        // Origin should be center (0.5, 0.5)
        Assert.Equal(0.5f, textureCommands[0].Origin.X);
        Assert.Equal(0.5f, textureCommands[0].Origin.Y);
    }

    [Fact]
    public void RenderSystem_WithTexture_AppliesColor()
    {
        SetupWorldWithMockRenderer();

        var texture = new TextureHandle(1);
        var color = new Vector4(0.5f, 0.7f, 0.3f, 0.9f);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            Texture = texture,
            StartColor = color
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var textureCommands = mockRenderer!.Commands.OfType<DrawTextureRotatedCommand>().ToList();
        Assert.Single(textureCommands);

        // Tint should match the particle color
        Assert.Equal(color.X, textureCommands[0].Tint.X, 2);
        Assert.Equal(color.Y, textureCommands[0].Tint.Y, 2);
        Assert.Equal(color.Z, textureCommands[0].Tint.Z, 2);
        Assert.Equal(color.W, textureCommands[0].Tint.W, 2);
    }

    #endregion

    #region No Particles Tests

    [Fact]
    public void RenderSystem_NoActiveParticles_DoesNotDraw()
    {
        SetupWorldWithMockRenderer();

        // Emitter that's not playing
        var emitter = ParticleEmitter.Burst(10, 1f) with { IsPlaying = false };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        // No draw commands should be issued
        Assert.Empty(mockRenderer!.Commands.OfType<FillCircleCommand>());
        Assert.Empty(mockRenderer.Commands.OfType<DrawTextureRotatedCommand>());
    }

    [Fact]
    public void RenderSystem_EmptyPool_SkipsRendering()
    {
        SetupWorldWithMockRenderer();

        // Short lifetime particles that die before render
        var emitter = ParticleEmitter.Burst(5, 0.01f); // Very short lifetime
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Run enough updates to kill all particles
        for (var i = 0; i < 10; i++)
        {
            world.Update(0.1f);
        }

        mockRenderer!.ClearCommands();
        world.Update(1f / 60f);

        // No particles should be drawn after they've all died
        Assert.Empty(mockRenderer.Commands.OfType<FillCircleCommand>());
    }

    #endregion

    #region No Renderer Tests

    [Fact]
    public void RenderSystem_NoRenderer_DoesNotCrash()
    {
        world = new World();
        // Don't set up Mock2DRenderer - no I2DRenderer extension
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(10, 1f);
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Should not throw
        world.Update(1f / 60f);
    }

    #endregion

    #region Lazy Initialization Tests

    [Fact]
    public void RenderSystem_LazyInitManager_AcquiresManagerOnUpdate()
    {
        // Setup: Add system before extensions exist
        world = new World();
        var system = new ParticleRenderSystem();
        world.AddSystem(system, SystemPhase.Render);

        // Now set up the particle manager and renderer AFTER system is initialized
        var config = new ParticlesConfig();
        var manager = new ParticleManager(world, config);
        world.SetExtension(manager);

        mockRenderer = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(mockRenderer);

        // Create an emitter
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Manually register the emitter since we didn't use the plugin
        ref var emitter = ref world.Get<ParticleEmitter>(entity);
        manager.RegisterEmitter(entity, in emitter);

        // Allocate particles manually to render
        var pool = manager.GetPool(entity)!;
        for (var i = 0; i < 5; i++)
        {
            var idx = pool.Allocate();
            pool.Alive[idx] = true;
            pool.Sizes[idx] = 10f;
            pool.ColorsA[idx] = 1f;
        }

        // First update triggers lazy init for both manager and renderer
        world.Update(1f / 60f);

        // Verify render happened (lazy init succeeded)
        Assert.True(mockRenderer.BeginCount > 0);
    }

    [Fact]
    public void RenderSystem_LazyInitRenderer_AcquiresRendererOnUpdate()
    {
        // Setup: Add system before renderer exists
        world = new World();
        var system = new ParticleRenderSystem();

        // Set up particle manager first (before system init)
        var config = new ParticlesConfig();
        var manager = new ParticleManager(world, config);
        world.SetExtension(manager);

        // Add system - OnInitialize will find manager but NOT renderer
        world.AddSystem(system, SystemPhase.Render);

        // Now add renderer after system is initialized
        mockRenderer = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(mockRenderer);

        // Create an emitter and register it
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(3, 1f))
            .Build();
        ref var emitter = ref world.Get<ParticleEmitter>(entity);
        manager.RegisterEmitter(entity, in emitter);

        // Manually spawn particles in the pool for rendering
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        // Allocate particles manually since spawn system isn't registered
        for (var i = 0; i < 3; i++)
        {
            var idx = pool.Allocate();
            pool.Alive[idx] = true;
            pool.Sizes[idx] = 10f;
            pool.ColorsA[idx] = 1f;
        }

        // First update - system should lazy-acquire renderer and render
        world.Update(1f / 60f);

        // Verify Begin/End were called (renderer was acquired)
        Assert.True(mockRenderer.BeginCount > 0);
    }

    [Fact]
    public void RenderSystem_NoManagerExtension_ReturnsEarly()
    {
        // Setup: Add system with no extensions at all
        world = new World();
        var system = new ParticleRenderSystem();
        world.AddSystem(system, SystemPhase.Render);

        // Create entities (no manager registered, so no pool will exist)
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Update should not throw - just returns early
        world.Update(1f / 60f);
    }

    [Fact]
    public void RenderSystem_ManagerExistsNoRenderer_ReturnsEarly()
    {
        // Setup: Add system with manager but no renderer
        world = new World();

        var config = new ParticlesConfig();
        var manager = new ParticleManager(world, config);
        world.SetExtension(manager);

        var system = new ParticleRenderSystem();
        world.AddSystem(system, SystemPhase.Render);

        // Create an emitter and register it
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();
        ref var emitter = ref world.Get<ParticleEmitter>(entity);
        manager.RegisterEmitter(entity, in emitter);

        // Allocate some particles
        var pool = manager.GetPool(entity);
        pool!.Allocate();
        pool.Alive[0] = true;

        // Update should not throw - returns early due to no renderer
        world.Update(1f / 60f);
    }

    #endregion

    #region Particle Properties in Rendering

    [Fact]
    public void RenderSystem_ParticlePosition_ReflectedInDrawCommands()
    {
        SetupWorldWithMockRenderer();

        var position = new Vector2(300f, 400f);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            Shape = EmissionShape.Point,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            StartSizeMin = 20f,
            StartSizeMax = 20f
        };
        world!.Spawn()
            .With(new Transform2D(position, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var circles = mockRenderer!.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Single(circles);

        // Center should be near the spawn position
        Assert.InRange(circles[0].Center.X, position.X - 15f, position.X + 15f);
        Assert.InRange(circles[0].Center.Y, position.Y - 15f, position.Y + 15f);
    }

    [Fact]
    public void RenderSystem_ParticleSize_ReflectedInRadius()
    {
        SetupWorldWithMockRenderer();

        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            StartSizeMin = 50f,
            StartSizeMax = 50f
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var circles = mockRenderer!.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Single(circles);

        // Radius should be half the size
        Assert.InRange(circles[0].Radius, 20f, 30f); // ~25 = 50/2
    }

    [Fact]
    public void RenderSystem_ParticleColor_ReflectedInCommands()
    {
        SetupWorldWithMockRenderer();

        var color = new Vector4(0.2f, 0.4f, 0.6f, 0.8f);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            StartColor = color
        };
        world!.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var circles = mockRenderer!.Commands.OfType<FillCircleCommand>().ToList();
        Assert.Single(circles);

        Assert.Equal(color.X, circles[0].Color.X, 2);
        Assert.Equal(color.Y, circles[0].Color.Y, 2);
        Assert.Equal(color.Z, circles[0].Color.Z, 2);
        Assert.Equal(color.W, circles[0].Color.W, 2);
    }

    #endregion
}
