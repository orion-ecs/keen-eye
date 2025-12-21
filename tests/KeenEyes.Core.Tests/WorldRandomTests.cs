namespace KeenEyes.Core.Tests;

/// <summary>
/// Tests for World.Random functionality.
/// </summary>
public class WorldRandomTests
{
    [Fact]
    public void NextInt_ReturnsValueInRange()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            int value = world.NextInt(10);
            Assert.InRange(value, 0, 9);
        }
    }

    [Fact]
    public void NextInt_WithRange_ReturnsValueInRange()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            int value = world.NextInt(5, 15);
            Assert.InRange(value, 5, 14);
        }
    }

    [Fact]
    public void NextFloat_ReturnsValueInRange()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            float value = world.NextFloat();
            Assert.InRange(value, 0.0f, 1.0f);
            Assert.True(value < 1.0f);
        }
    }

    [Fact]
    public void NextDouble_ReturnsValueInRange()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            double value = world.NextDouble();
            Assert.InRange(value, 0.0, 1.0);
            Assert.True(value < 1.0);
        }
    }

    [Fact]
    public void NextBool_ReturnsBooleanValues()
    {
        using var world = new World();

        bool hasTrue = false;
        bool hasFalse = false;

        for (int i = 0; i < 100; i++)
        {
            if (world.NextBool())
            {
                hasTrue = true;
            }
            else
            {
                hasFalse = true;
            }

            if (hasTrue && hasFalse)
            {
                break;
            }
        }

        Assert.True(hasTrue, "Should eventually return true");
        Assert.True(hasFalse, "Should eventually return false");
    }

    [Fact]
    public void NextBool_WithProbability_ReturnsValues()
    {
        using var world = new World();

        // Test with 100% probability
        for (int i = 0; i < 10; i++)
        {
            Assert.True(world.NextBool(1.0f));
        }

        // Test with 0% probability
        for (int i = 0; i < 10; i++)
        {
            Assert.False(world.NextBool(0.0f));
        }
    }

    [Fact]
    public void NextBool_WithInvalidProbability_Throws()
    {
        using var world = new World();

        Assert.Throws<ArgumentOutOfRangeException>(() => world.NextBool(-0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => world.NextBool(1.1f));
    }

    [Fact]
    public void World_WithSeed_ProducesDeterministicSequence()
    {
        using var world1 = new World(seed: 12345);
        using var world2 = new World(seed: 12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(world1.NextInt(1000), world2.NextInt(1000));
        }
    }

    [Fact]
    public void World_WithDifferentSeeds_ProducesDifferentSequences()
    {
        using var world1 = new World(seed: 12345);
        using var world2 = new World(seed: 54321);

        // At least some values should be different
        int differences = 0;
        for (int i = 0; i < 100; i++)
        {
            if (world1.NextInt(1000) != world2.NextInt(1000))
            {
                differences++;
            }
        }

        Assert.True(differences > 50, "Worlds with different seeds should produce mostly different sequences");
    }
}
