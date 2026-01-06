namespace KeenEyes.Testing.Fixtures;

/// <summary>
/// Provides preset entity configurations for common test scenarios.
/// </summary>
/// <remarks>
/// <para>
/// EntityPresets offers fluent factory methods for creating commonly-used
/// entity configurations in tests, reducing boilerplate and improving test
/// readability.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Create a player with standard components
/// var player = EntityPresets.Player(world)
///     .WithName("Player1")
///     .AtPosition(100, 50)
///     .WithHealth(100)
///     .Build();
///
/// // Create multiple enemies
/// var enemies = EntityPresets.CreateEnemies(world, count: 5)
///     .WithHealth(50)
///     .WithDamage(10)
///     .Build();
/// </code>
/// </example>
public static class EntityPresets
{
    /// <summary>
    /// Creates a player entity builder with standard player components.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <returns>A fluent entity preset builder.</returns>
    public static EntityPresetBuilder Player(World world)
    {
        return new EntityPresetBuilder(world)
            .WithTag<PlayerTag>()
            .WithHealth(100)
            .WithSpeed(10f)
            .AtPosition(0, 0);
    }

    /// <summary>
    /// Creates an enemy entity builder with standard enemy components.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <returns>A fluent entity preset builder.</returns>
    public static EntityPresetBuilder Enemy(World world)
    {
        return new EntityPresetBuilder(world)
            .WithTag<EnemyTag>()
            .WithHealth(50)
            .WithDamage(10)
            .WithSpeed(5f)
            .AtPosition(0, 0);
    }

    /// <summary>
    /// Creates a projectile entity builder with standard projectile components.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <returns>A fluent entity preset builder.</returns>
    public static EntityPresetBuilder Projectile(World world)
    {
        return new EntityPresetBuilder(world)
            .WithTag<ProjectileTag>()
            .WithDamage(25)
            .WithSpeed(20f)
            .WithLifetime(5f)
            .AtPosition(0, 0);
    }

    /// <summary>
    /// Creates a pickup entity builder with standard pickup components.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <returns>A fluent entity preset builder.</returns>
    public static EntityPresetBuilder Pickup(World world)
    {
        return new EntityPresetBuilder(world)
            .WithTag<PickupTag>()
            .AtPosition(0, 0);
    }

    /// <summary>
    /// Creates a basic moving entity with position and velocity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <returns>A fluent entity preset builder.</returns>
    public static EntityPresetBuilder MovingEntity(World world)
    {
        return new EntityPresetBuilder(world)
            .AtPosition(0, 0)
            .WithVelocity(0, 0);
    }

    /// <summary>
    /// Creates multiple player entities.
    /// </summary>
    /// <param name="world">The world to create entities in.</param>
    /// <param name="count">Number of players to create.</param>
    /// <returns>A batch entity builder.</returns>
    public static BatchEntityBuilder CreatePlayers(World world, int count)
    {
        return new BatchEntityBuilder(world, count, () => Player(world));
    }

    /// <summary>
    /// Creates multiple enemy entities.
    /// </summary>
    /// <param name="world">The world to create entities in.</param>
    /// <param name="count">Number of enemies to create.</param>
    /// <returns>A batch entity builder.</returns>
    public static BatchEntityBuilder CreateEnemies(World world, int count)
    {
        return new BatchEntityBuilder(world, count, () => Enemy(world));
    }

    /// <summary>
    /// Creates multiple projectile entities.
    /// </summary>
    /// <param name="world">The world to create entities in.</param>
    /// <param name="count">Number of projectiles to create.</param>
    /// <returns>A batch entity builder.</returns>
    public static BatchEntityBuilder CreateProjectiles(World world, int count)
    {
        return new BatchEntityBuilder(world, count, () => Projectile(world));
    }
}

/// <summary>
/// Fluent builder for creating preset entities.
/// </summary>
public sealed class EntityPresetBuilder
{
    private readonly World world;
    private string? name;
    private float posX;
    private float posY;
    private float velX;
    private float velY;
    private int health = -1;
    private int maxHealth = -1;
    private int damage = -1;
    private float speed = -1;
    private float lifetime = -1;
    private int team = -1;
    private readonly List<Action<EntityBuilder>> tagActions = [];
    private bool hasVelocity;

    internal EntityPresetBuilder(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// Sets the entity name.
    /// </summary>
    public EntityPresetBuilder WithName(string name)
    {
        this.name = name;
        return this;
    }

    /// <summary>
    /// Sets the entity position.
    /// </summary>
    public EntityPresetBuilder AtPosition(float x, float y)
    {
        posX = x;
        posY = y;
        return this;
    }

    /// <summary>
    /// Sets the entity velocity.
    /// </summary>
    public EntityPresetBuilder WithVelocity(float vx, float vy)
    {
        velX = vx;
        velY = vy;
        hasVelocity = true;
        return this;
    }

    /// <summary>
    /// Sets the entity health (both current and max).
    /// </summary>
    public EntityPresetBuilder WithHealth(int value)
    {
        health = value;
        maxHealth = value;
        return this;
    }

    /// <summary>
    /// Sets the entity health with different current and max values.
    /// </summary>
    public EntityPresetBuilder WithHealth(int current, int max)
    {
        health = current;
        maxHealth = max;
        return this;
    }

    /// <summary>
    /// Sets the entity damage.
    /// </summary>
    public EntityPresetBuilder WithDamage(int value)
    {
        damage = value;
        return this;
    }

    /// <summary>
    /// Sets the entity speed.
    /// </summary>
    public EntityPresetBuilder WithSpeed(float value)
    {
        speed = value;
        return this;
    }

    /// <summary>
    /// Sets the entity lifetime.
    /// </summary>
    public EntityPresetBuilder WithLifetime(float seconds)
    {
        lifetime = seconds;
        return this;
    }

    /// <summary>
    /// Sets the entity team.
    /// </summary>
    public EntityPresetBuilder OnTeam(int teamId)
    {
        team = teamId;
        return this;
    }

    /// <summary>
    /// Adds a tag component to the entity.
    /// </summary>
    public EntityPresetBuilder WithTag<T>() where T : struct, ITagComponent
    {
        tagActions.Add(builder => builder.WithTag<T>());
        return this;
    }

    /// <summary>
    /// Builds and returns the entity.
    /// </summary>
    public Entity Build()
    {
        var builder = name != null
            ? world.Spawn(name)
            : world.Spawn();

        // Add position
        builder.With(TestPosition.Create(posX, posY));

        // Add velocity if set
        if (hasVelocity)
        {
            builder.With(TestVelocity.Create(velX, velY));
        }

        // Add health if set
        if (health >= 0)
        {
            builder.With(TestHealth.Create(health, maxHealth));
        }

        // Add damage if set
        if (damage >= 0)
        {
            builder.With(TestDamage.Create(damage));
        }

        // Add speed if set
        if (speed >= 0)
        {
            builder.With(TestSpeed.Create(speed));
        }

        // Add lifetime if set
        if (lifetime >= 0)
        {
            builder.With(TestLifetime.Create(lifetime));
        }

        // Add team if set
        if (team >= 0)
        {
            builder.With(TestTeam.Create(team));
        }

        // Add tags
        foreach (var tagAction in tagActions)
        {
            tagAction(builder);
        }

        return builder.Build();
    }
}

/// <summary>
/// Builder for creating multiple entities with the same base configuration.
/// </summary>
public sealed class BatchEntityBuilder
{
    private readonly int count;
    private readonly Func<EntityPresetBuilder> factory;
    private readonly List<Action<EntityPresetBuilder, int>> modifiers = [];

    internal BatchEntityBuilder(World world, int count, Func<EntityPresetBuilder> factory)
    {
        _ = world; // Unused but kept for API consistency
        this.count = count;
        this.factory = factory;
    }

    /// <summary>
    /// Applies a modification to each entity based on its index.
    /// </summary>
    public BatchEntityBuilder WithModifier(Action<EntityPresetBuilder, int> modifier)
    {
        modifiers.Add(modifier);
        return this;
    }

    /// <summary>
    /// Sets all entities to have the specified health.
    /// </summary>
    public BatchEntityBuilder WithHealth(int value)
    {
        return WithModifier((b, _) => b.WithHealth(value));
    }

    /// <summary>
    /// Sets all entities to have the specified damage.
    /// </summary>
    public BatchEntityBuilder WithDamage(int value)
    {
        return WithModifier((b, _) => b.WithDamage(value));
    }

    /// <summary>
    /// Sets all entities to have the specified speed.
    /// </summary>
    public BatchEntityBuilder WithSpeed(float value)
    {
        return WithModifier((b, _) => b.WithSpeed(value));
    }

    /// <summary>
    /// Positions entities in a grid pattern.
    /// </summary>
    public BatchEntityBuilder InGrid(int columns, float spacing)
    {
        return WithModifier((b, i) =>
        {
            int col = i % columns;
            int row = i / columns;
            b.AtPosition(col * spacing, row * spacing);
        });
    }

    /// <summary>
    /// Positions entities in a line.
    /// </summary>
    public BatchEntityBuilder InLine(float spacing, bool horizontal = true)
    {
        return WithModifier((b, i) =>
        {
            if (horizontal)
            {
                b.AtPosition(i * spacing, 0);
            }
            else
            {
                b.AtPosition(0, i * spacing);
            }
        });
    }

    /// <summary>
    /// Assigns sequential team IDs to entities.
    /// </summary>
    public BatchEntityBuilder WithSequentialTeams()
    {
        return WithModifier((b, i) => b.OnTeam(i));
    }

    /// <summary>
    /// Assigns entities to alternating teams.
    /// </summary>
    public BatchEntityBuilder WithAlternatingTeams(int teamA, int teamB)
    {
        return WithModifier((b, i) => b.OnTeam(i % 2 == 0 ? teamA : teamB));
    }

    /// <summary>
    /// Builds and returns all entities.
    /// </summary>
    public Entity[] Build()
    {
        var entities = new Entity[count];

        for (int i = 0; i < count; i++)
        {
            var builder = factory();

            foreach (var modifier in modifiers)
            {
                modifier(builder, i);
            }

            entities[i] = builder.Build();
        }

        return entities;
    }
}
