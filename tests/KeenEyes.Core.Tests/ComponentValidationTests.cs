namespace KeenEyes.Tests;

/// <summary>
/// Tests for component validation: RequiresComponent, ConflictsWith, and custom validators.
/// </summary>
public partial class ComponentValidationTests
{
    static ComponentValidationTests()
    {
        // Register constraints for test components (AOT-compatible)
        RegisterTestConstraints();
    }

    #region Test Components

    // Base component with no constraints
    [Component]
    public partial struct Transform
    {
        public float X, Y;
    }

    // Component that requires Transform
    [Component]
    [RequiresComponent(typeof(Transform))]
    public partial struct Renderable
    {
        public string TextureId;
    }

    // Component that requires both Transform and Renderable
    [Component]
    [RequiresComponent(typeof(Transform))]
    [RequiresComponent(typeof(Renderable))]
    public partial struct Sprite
    {
        public int Layer;
    }

    // Component that conflicts with DynamicBody
    [Component]
    [ConflictsWith(typeof(DynamicBody))]
    public partial struct StaticBody
    {
        public bool IsKinematic;
    }

    // Component that conflicts with StaticBody (mutual conflict)
    [Component]
    [ConflictsWith(typeof(StaticBody))]
    public partial struct DynamicBody
    {
        public float Mass;
    }

    // Component with no constraints
    [Component]
    public partial struct Health
    {
        public int Current;
        public int Max;
    }

    // Component with both requires and conflicts
    [Component]
    [RequiresComponent(typeof(Transform))]
    [ConflictsWith(typeof(StaticBody))]
    public partial struct RigidBody
    {
        public float Mass;
        public float Drag;
    }

    /// <summary>
    /// Registers constraint provider for test components.
    /// Required for AOT-compatible validation since reflection was removed.
    /// </summary>
    private static void RegisterTestConstraints()
    {
        ComponentValidationManager.RegisterConstraintProvider(TryGetTestConstraints);
    }

    /// <summary>
    /// Provides validation constraints for test component types.
    /// </summary>
    private static bool TryGetTestConstraints(Type componentType, out Type[] required, out Type[] conflicts)
    {
        required = [];
        conflicts = [];

        if (componentType == typeof(Renderable))
        {
            required = [typeof(Transform)];
            return true;
        }
        if (componentType == typeof(Sprite))
        {
            required = [typeof(Transform), typeof(Renderable)];
            return true;
        }
        if (componentType == typeof(StaticBody))
        {
            conflicts = [typeof(DynamicBody)];
            return true;
        }
        if (componentType == typeof(DynamicBody))
        {
            conflicts = [typeof(StaticBody)];
            return true;
        }
        if (componentType == typeof(RigidBody))
        {
            required = [typeof(Transform)];
            conflicts = [typeof(StaticBody)];
            return true;
        }

        return false;
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

    #region Custom Validator in EntityBuilder Tests

    [Fact]
    public void EntityBuilder_WithCustomValidator_ValidatorPasses_Succeeds()
    {
        using var world = new World();

        // Register validator that checks Current <= Max
        world.RegisterValidator<Health>((w, e, h) => h.Current <= h.Max);

        // Build entity with valid Health component
        var entity = world.Spawn()
            .With(new Health { Current = 50, Max = 100 })
            .Build();

        Assert.True(world.Has<Health>(entity));
        var health = world.Get<Health>(entity);
        Assert.Equal(50, health.Current);
    }

    [Fact]
    public void EntityBuilder_WithCustomValidator_ValidatorFails_ThrowsValidationException()
    {
        using var world = new World();

        // Register validator that checks Current <= Max
        world.RegisterValidator<Health>((w, e, h) => h.Current <= h.Max);

        // Build entity with invalid Health component (Current > Max)
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Spawn()
                .With(new Health { Current = 150, Max = 100 })
                .Build());

        Assert.Equal(typeof(Health), ex.ComponentType);
        Assert.Contains("Custom validation failed", ex.Message);
    }

    #endregion

    #region ValidationMode.DebugOnly Tests

    [Fact]
    public void ValidationMode_DebugOnly_ValidatesInDebugBuild()
    {
        using var world = new World();
        world.ValidationMode = ValidationMode.DebugOnly;

        var entity = world.Spawn().Build();

        // In debug build, this should validate and throw
        // In release build, this should skip validation
#if DEBUG
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new Renderable { TextureId = "test.png" }));
#else
        // In release, validation is skipped so this should succeed
        world.Add(entity, new Renderable { TextureId = "test.png" });
        Assert.True(world.Has<Renderable>(entity));
#endif
    }

    #endregion

    #region Additional ComponentValidationException Tests

    [Fact]
    public void ComponentValidationException_WithInnerException_ContainsAllProperties()
    {
        var innerEx = new InvalidOperationException("Inner error");
        var ex = new ComponentValidationException("Test message", typeof(Transform), innerEx);

        Assert.Equal(typeof(Transform), ex.ComponentType);
        Assert.Equal("Test message", ex.Message);
        Assert.Same(innerEx, ex.InnerException);
        Assert.Null(ex.Entity);
    }

    #endregion

    #region RegisterValidator Edge Cases

    [Fact]
    public void RegisterValidator_WithNullValidator_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.RegisterValidator<Health>(null!));
    }

    #endregion

    #region HasComponent Edge Cases

    [Fact]
    public void HasComponent_WithNullType_ThrowsArgumentNullException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.Throws<ArgumentNullException>(() =>
            world.HasComponent(entity, null!));
    }

    [Fact]
    public void HasComponent_WithUnregisteredType_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        // Use a type that's not registered as a component
        Assert.False(world.HasComponent(entity, typeof(string)));
    }

    #endregion

    #region Validation Caching Tests

    [Fact]
    public void Validation_CachesValidationInfo_ForSameComponentType()
    {
        using var world = new World();

        // Create multiple entities with the same constrained component
        // This exercises the validation cache (GetOrCreateValidationInfo)
        var entity1 = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform { X = 1, Y = 1 })
            .Build();

        // Add Renderable (requires Transform) to both - second call should use cache
        world.Add(entity1, new Renderable { TextureId = "a.png" });
        world.Add(entity2, new Renderable { TextureId = "b.png" });

        Assert.True(world.Has<Renderable>(entity1));
        Assert.True(world.Has<Renderable>(entity2));
    }

    [Fact]
    public void Validation_ComponentWithNoConstraints_CachesEmptyInfo()
    {
        using var world = new World();

        // Add Health (no constraints) multiple times to exercise cache
        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        world.Add(entity1, new Health { Current = 100, Max = 100 });
        world.Add(entity2, new Health { Current = 50, Max = 100 });

        Assert.True(world.Has<Health>(entity1));
        Assert.True(world.Has<Health>(entity2));
    }

    #endregion

    #region Multiple Validators Tests

    [Fact]
    public void Validation_MultipleComponentsWithConstraints_AllValidated()
    {
        using var world = new World();

        // Build entity with multiple constrained components
        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .With(new Renderable { TextureId = "test.png" })
            .With(new Sprite { Layer = 1 })  // Requires both Transform and Renderable
            .Build();

        Assert.True(world.Has<Transform>(entity));
        Assert.True(world.Has<Renderable>(entity));
        Assert.True(world.Has<Sprite>(entity));
    }

    [Fact]
    public void Validation_MultipleCustomValidators_AllExecuted()
    {
        using var world = new World();
        var healthValidated = false;
        var transformValidated = false;

        world.RegisterValidator<Health>((w, e, h) =>
        {
            healthValidated = true;
            return true;
        });

        world.RegisterValidator<Transform>((w, e, t) =>
        {
            transformValidated = true;
            return true;
        });

        var entity = world.Spawn()
            .With(new Transform { X = 0, Y = 0 })
            .With(new Health { Current = 100, Max = 100 })
            .Build();

        Assert.True(healthValidated);
        Assert.True(transformValidated);
    }

    #endregion
}
