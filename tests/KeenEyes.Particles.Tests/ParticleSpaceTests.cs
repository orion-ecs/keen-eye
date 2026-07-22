using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for <see cref="ParticleSpace"/> World/Local simulation behavior.
/// </summary>
public class ParticleSpaceTests : IDisposable
{
    private World? world;
    private Mock2DRenderer? mockRenderer;

    public void Dispose()
    {
        mockRenderer?.Dispose();
        world?.Dispose();
    }

    private static int FirstAlive(ParticlePool pool)
    {
        for (var i = 0; i < pool.Capacity; i++)
        {
            if (pool.Alive[i])
            {
                return i;
            }
        }

        return -1;
    }

    [Fact]
    public void WorldSpace_IsTheDefault()
    {
        Assert.Equal(ParticleSpace.World, default(ParticleEmitter).Space);
        Assert.Equal(ParticleSpace.World, ParticleEmitter.Default.Space);
    }

    [Fact]
    public void WorldSpace_EmitterMovesMidLife_ParticlesStayInWorldCoordinates()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var start = new Vector2(100f, 100f);
        var emitter = ParticleEmitter.Burst(1, 10f) with
        {
            Space = ParticleSpace.World,
            Shape = EmissionShape.Point,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var entity = world.Spawn()
            .With(new Transform2D(start, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity)!;
        var idx = FirstAlive(pool);
        Assert.True(idx >= 0);

        var spawnedX = pool.PositionsX[idx];
        var spawnedY = pool.PositionsY[idx];
        Assert.True(spawnedX.ApproximatelyEquals(start.X, 0.01f));
        Assert.True(spawnedY.ApproximatelyEquals(start.Y, 0.01f));

        // Move the emitter well away from the spawn location.
        ref var transform = ref world.Get<Transform2D>(entity);
        transform.Position = new Vector2(500f, 400f);

        world.Update(1f / 60f);

        // World-space particles are anchored at spawn time and must not follow the emitter.
        Assert.True(pool.PositionsX[idx].ApproximatelyEquals(spawnedX, 0.01f));
        Assert.True(pool.PositionsY[idx].ApproximatelyEquals(spawnedY, 0.01f));
    }

    [Fact]
    public void LocalSpace_ParticlesStoredRelativeToEmitter()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var start = new Vector2(100f, 100f);
        var emitter = ParticleEmitter.Burst(1, 10f) with
        {
            Space = ParticleSpace.Local,
            Shape = EmissionShape.Point,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var entity = world.Spawn()
            .With(new Transform2D(start, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity)!;
        var idx = FirstAlive(pool);
        Assert.True(idx >= 0);

        // Stored positions are local (relative to the emitter), not the world position.
        Assert.True(pool.PositionsX[idx].IsApproximatelyZero(0.01f));
        Assert.True(pool.PositionsY[idx].IsApproximatelyZero(0.01f));
    }

    [Fact]
    public void LocalSpace_EmitterMovesMidLife_RenderedParticlesFollowEmitter()
    {
        world = new World();
        mockRenderer = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(mockRenderer);
        world.InstallPlugin(new ParticlesPlugin());

        var start = new Vector2(100f, 100f);
        var emitter = ParticleEmitter.Burst(1, 10f) with
        {
            Space = ParticleSpace.Local,
            Shape = EmissionShape.Point,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            StartSizeMin = 10f,
            StartSizeMax = 10f
        };
        var entity = world.Spawn()
            .With(new Transform2D(start, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        // The particle renders at the emitter's current world position (local offset + emitter pos).
        var beforeCircle = Assert.Single(mockRenderer.Commands.OfType<FillCircleCommand>());
        Assert.True(beforeCircle.Center.X.ApproximatelyEquals(start.X, 0.5f));
        Assert.True(beforeCircle.Center.Y.ApproximatelyEquals(start.Y, 0.5f));

        // Move the emitter; the same particle should now render at the new position.
        var moved = new Vector2(500f, 400f);
        ref var transform = ref world.Get<Transform2D>(entity);
        transform.Position = moved;

        mockRenderer.ClearCommands();
        world.Update(1f / 60f);

        var afterCircle = Assert.Single(mockRenderer.Commands.OfType<FillCircleCommand>());
        Assert.True(afterCircle.Center.X.ApproximatelyEquals(moved.X, 0.5f));
        Assert.True(afterCircle.Center.Y.ApproximatelyEquals(moved.Y, 0.5f));
    }
}
