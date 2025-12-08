namespace KeenEyes.Tests;

/// <summary>
/// Tests for component validation: RequiresComponent, ConflictsWith, and custom validators.
/// </summary>
public class ComponentValidationTests
{
    #region Test Components

    // Base component with no constraints
    public struct Transform : IComponent
    {
        public float X, Y;
    }

    // Component that requires Transform
    [RequiresComponent(typeof(Transform))]
    public struct Renderable : IComponent
    {
        public string TextureId;
    }

    // Component that requires both Transform and Renderable
    [RequiresComponent(typeof(Transform))]
    [RequiresComponent(typeof(Renderable))]
    public struct Sprite : IComponent
    {
        public int Layer;
    }

    // Component that conflicts with DynamicBody
    [ConflictsWith(typeof(DynamicBody))]
    public struct StaticBody : IComponent
    {
        public bool IsKinematic;
    }

    // Component that conflicts with StaticBody (mutual conflict)
    [ConflictsWith(typeof(StaticBody))]
    public struct DynamicBody : IComponent
    {
        public float Mass;
    }

    // Component with no constraints
    public struct Health : IComponent
    {
        public int Current;
        public int Max;
    }

    // Component with both requires and conflicts
    [RequiresComponent(typeof(Transform))]
    [ConflictsWith(typeof(StaticBody))]
    public struct RigidBody : IComponent
    {
        public float Mass;
        public float Drag;
    }

    #endregion

    #region RequiresComponent Tests

    [Fact]
    public void Add_WithRequiredComponentPresent_Succeeds()
    {
        using var world = new World();

        // First add the required component
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        // Should succeed because Transform is present
        world.Add(entity, new Renderable { TextureId = "sprite.png" });

        Assert.True(world.Has<Renderable>(entity));
    }

    [Fact]
    public void Add_WithoutRequiredComponent_ThrowsValidationException()
    {
        using var world = new World();

        // Create entity without Transform
        var entity = world.Spawn().Build();

        // Should fail because Transform is required
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new Renderable { TextureId = "sprite.png" }));

        Assert.Equal(typeof(Renderable), ex.ComponentType);
        Assert.Contains("Transform", ex.Message);
        Assert.Contains("requires", ex.Message);
    }

    [Fact]
    public void Add_WithMultipleRequiredComponents_AllPresent_Succeeds()
    {
        using var world = new World();

        // Add both required components
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .With(new Renderable { TextureId = "base.png" })
            .Build();

        // Should succeed because both Transform and Renderable are present
        world.Add(entity, new Sprite { Layer = 1 });

        Assert.True(world.Has<Sprite>(entity));
    }

    [Fact]
    public void Add_WithMultipleRequiredComponents_OneMissing_ThrowsValidationException()
    {
        using var world = new World();

        // Only add Transform, missing Renderable
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        // Should fail because Renderable is missing
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new Sprite { Layer = 1 }));

        Assert.Equal(typeof(Sprite), ex.ComponentType);
        Assert.Contains("Renderable", ex.Message);
    }

    #endregion

    #region ConflictsWith Tests

    [Fact]
    public void Add_WithNoConflictingComponent_Succeeds()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        // Should succeed because DynamicBody is not present
        world.Add(entity, new StaticBody { IsKinematic = false });

        Assert.True(world.Has<StaticBody>(entity));
    }

    [Fact]
    public void Add_WithConflictingComponent_ThrowsValidationException()
    {
        using var world = new World();

        // First add StaticBody
        var entity = world.Spawn()
            .With(new StaticBody { IsKinematic = false })
            .Build();

        // Should fail because StaticBody conflicts with DynamicBody
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new DynamicBody { Mass = 1.0f }));

        Assert.Equal(typeof(DynamicBody), ex.ComponentType);
        Assert.Contains("StaticBody", ex.Message);
        Assert.Contains("conflicts", ex.Message);
    }

    [Fact]
    public void Add_MutualConflict_BothDirectionsThrow()
    {
        using var world = new World();

        // Create entity with StaticBody
        var entity1 = world.Spawn()
            .With(new StaticBody { IsKinematic = false })
            .Build();

        // Create entity with DynamicBody
        var entity2 = world.Spawn()
            .With(new DynamicBody { Mass = 1.0f })
            .Build();

        // Adding DynamicBody to entity with StaticBody should fail
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity1, new DynamicBody { Mass = 1.0f }));

        // Adding StaticBody to entity with DynamicBody should fail
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity2, new StaticBody { IsKinematic = true }));
    }

    #endregion

    #region Combined Requirements and Conflicts Tests

    [Fact]
    public void Add_WithBothRequiresAndConflicts_RequirementMet_NoConflict_Succeeds()
    {
        using var world = new World();

        // Add Transform (required), no StaticBody (conflicts)
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        // Should succeed
        world.Add(entity, new RigidBody { Mass = 1.0f, Drag = 0.1f });

        Assert.True(world.Has<RigidBody>(entity));
    }

    [Fact]
    public void Add_WithBothRequiresAndConflicts_RequirementNotMet_ThrowsForRequirement()
    {
        using var world = new World();

        // No Transform (required)
        var entity = world.Spawn().Build();

        // Should throw for missing requirement
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new RigidBody { Mass = 1.0f, Drag = 0.1f }));

        Assert.Contains("Transform", ex.Message);
        Assert.Contains("requires", ex.Message);
    }

    [Fact]
    public void Add_WithBothRequiresAndConflicts_HasConflict_ThrowsForConflict()
    {
        using var world = new World();

        // Add Transform (required) and StaticBody (conflicts)
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .With(new StaticBody { IsKinematic = false })
            .Build();

        // Should throw for conflict
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new RigidBody { Mass = 1.0f, Drag = 0.1f }));

        Assert.Contains("StaticBody", ex.Message);
        Assert.Contains("conflicts", ex.Message);
    }

    #endregion

    #region EntityBuilder Validation Tests

    [Fact]
    public void EntityBuilder_WithAllRequiredComponents_Succeeds()
    {
        using var world = new World();

        // Building with all requirements met should succeed
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .With(new Renderable { TextureId = "test.png" })
            .Build();

        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<Transform>(entity));
        Assert.True(world.Has<Renderable>(entity));
    }

    [Fact]
    public void EntityBuilder_WithMissingRequiredComponent_ThrowsValidationException()
    {
        using var world = new World();

        // Renderable requires Transform, but we're not adding it
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Spawn()
                .With(new Renderable { TextureId = "test.png" })
                .Build());

        Assert.Equal(typeof(Renderable), ex.ComponentType);
        Assert.Contains("Transform", ex.Message);
    }

    [Fact]
    public void EntityBuilder_WithConflictingComponents_ThrowsValidationException()
    {
        using var world = new World();

        // StaticBody and DynamicBody conflict
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Spawn()
                .With(new StaticBody { IsKinematic = false })
                .With(new DynamicBody { Mass = 1.0f })
                .Build());

        // Should detect the conflict
        Assert.Contains("conflicts", ex.Message);
    }

    #endregion

    #region ValidationMode Tests

    [Fact]
    public void ValidationMode_Disabled_SkipsAllValidation()
    {
        using var world = new World();
        world.ValidationMode = ValidationMode.Disabled;

        // Without Transform (required), but validation is disabled
        var entity = world.Spawn().Build();

        // Should succeed because validation is disabled
        world.Add(entity, new Renderable { TextureId = "test.png" });

        Assert.True(world.Has<Renderable>(entity));
    }

    [Fact]
    public void ValidationMode_Enabled_ValidatesConstraints()
    {
        using var world = new World();
        world.ValidationMode = ValidationMode.Enabled;

        var entity = world.Spawn().Build();

        // Should fail because validation is enabled
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new Renderable { TextureId = "test.png" }));
    }

    [Fact]
    public void ValidationMode_CanBeChangedAtRuntime()
    {
        using var world = new World();

        // Start with validation enabled
        world.ValidationMode = ValidationMode.Enabled;
        var entity1 = world.Spawn().Build();

        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity1, new Renderable { TextureId = "test.png" }));

        // Disable validation
        world.ValidationMode = ValidationMode.Disabled;
        var entity2 = world.Spawn().Build();

        // Now should succeed
        world.Add(entity2, new Renderable { TextureId = "test.png" });
        Assert.True(world.Has<Renderable>(entity2));
    }

    #endregion

    #region Custom Validator Tests

    [Fact]
    public void RegisterValidator_ValidatorPasses_ComponentAdded()
    {
        using var world = new World();

        // Register validator that always passes
        world.RegisterValidator<Health>((w, e, h) => h.Current >= 0 && h.Current <= h.Max);

        var entity = world.Spawn().Build();
        world.Add(entity, new Health { Current = 50, Max = 100 });

        Assert.True(world.Has<Health>(entity));
    }

    [Fact]
    public void RegisterValidator_ValidatorFails_ThrowsValidationException()
    {
        using var world = new World();

        // Register validator that checks Current <= Max
        world.RegisterValidator<Health>((w, e, h) => h.Current <= h.Max);

        var entity = world.Spawn().Build();

        // Should fail because Current > Max
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new Health { Current = 150, Max = 100 }));

        Assert.Equal(typeof(Health), ex.ComponentType);
        Assert.Contains("Custom validation failed", ex.Message);
    }

    [Fact]
    public void RegisterValidator_MultipleTimes_LastValidatorWins()
    {
        using var world = new World();

        // Register validator that passes
        world.RegisterValidator<Health>((w, e, h) => true);

        // Register different validator that fails
        world.RegisterValidator<Health>((w, e, h) => false);

        var entity = world.Spawn().Build();

        // Should fail because last validator always returns false
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new Health { Current = 50, Max = 100 }));
    }

    [Fact]
    public void UnregisterValidator_RemovesValidator()
    {
        using var world = new World();

        // Register validator that always fails
        world.RegisterValidator<Health>((w, e, h) => false);

        // Unregister it
        var removed = world.UnregisterValidator<Health>();
        Assert.True(removed);

        var entity = world.Spawn().Build();

        // Should succeed because validator is removed
        world.Add(entity, new Health { Current = 50, Max = 100 });
        Assert.True(world.Has<Health>(entity));
    }

    [Fact]
    public void UnregisterValidator_WhenNoValidatorExists_ReturnsFalse()
    {
        using var world = new World();

        var removed = world.UnregisterValidator<Health>();

        Assert.False(removed);
    }

    [Fact]
    public void CustomValidator_CanAccessWorldState()
    {
        using var world = new World();

        // Track how many Health components exist in the world
        world.RegisterValidator<Health>((w, entity, health) =>
        {
            // Count existing Health components
            int count = 0;
            foreach (var _ in w.Query<Health>())
            {
                count++;
            }
            // Only allow up to 3 Health components
            return count < 3;
        });

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();
        var entity4 = world.Spawn().Build();

        // First three should succeed
        world.Add(entity1, new Health { Current = 100, Max = 100 });
        world.Add(entity2, new Health { Current = 100, Max = 100 });
        world.Add(entity3, new Health { Current = 100, Max = 100 });

        // Fourth should fail
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity4, new Health { Current = 100, Max = 100 }));
    }

    #endregion

    #region HasComponent by Type Tests

    [Fact]
    public void HasComponent_WithType_ReturnsTrue_WhenComponentPresent()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        Assert.True(world.HasComponent(entity, typeof(Transform)));
    }

    [Fact]
    public void HasComponent_WithType_ReturnsFalse_WhenComponentAbsent()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        Assert.False(world.HasComponent(entity, typeof(Transform)));
    }

    [Fact]
    public void HasComponent_WithType_ReturnsFalse_WhenEntityNotAlive()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        world.Despawn(entity);

        Assert.False(world.HasComponent(entity, typeof(Transform)));
    }

    #endregion

    #region ComponentValidationException Tests

    [Fact]
    public void ComponentValidationException_ContainsComponentType()
    {
        var ex = new ComponentValidationException("Test message", typeof(Transform));

        Assert.Equal(typeof(Transform), ex.ComponentType);
    }

    [Fact]
    public void ComponentValidationException_ContainsEntity_WhenProvided()
    {
        var entity = new Entity(1, 0);
        var ex = new ComponentValidationException("Test message", typeof(Transform), entity);

        Assert.Equal(typeof(Transform), ex.ComponentType);
        Assert.Equal(entity, ex.Entity);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validation_ComponentWithNoConstraints_AlwaysSucceeds()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        // Health has no constraints
        world.Add(entity, new Health { Current = 50, Max = 100 });

        Assert.True(world.Has<Health>(entity));
    }

    [Fact]
    public void Validation_DisabledMode_AllowsConflictingComponents()
    {
        using var world = new World();
        world.ValidationMode = ValidationMode.Disabled;

        // Add conflicting components - should work because validation is disabled
        var entity = world.Spawn()
            .With(new StaticBody { IsKinematic = false })
            .With(new DynamicBody { Mass = 1.0f })
            .Build();

        Assert.True(world.Has<StaticBody>(entity));
        Assert.True(world.Has<DynamicBody>(entity));
    }

    [Fact]
    public void Validation_DisabledInEntityBuilder_AllowsMissingRequirements()
    {
        using var world = new World();
        world.ValidationMode = ValidationMode.Disabled;

        // Renderable requires Transform, but validation is disabled
        var entity = world.Spawn()
            .With(new Renderable { TextureId = "test.png" })
            .Build();

        Assert.True(world.Has<Renderable>(entity));
        Assert.False(world.Has<Transform>(entity));
    }

    #endregion
}
