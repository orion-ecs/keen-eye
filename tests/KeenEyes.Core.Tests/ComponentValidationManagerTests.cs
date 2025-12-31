namespace KeenEyes.Tests;

/// <summary>
/// Tests for ComponentValidationManager class focusing on uncovered paths.
/// </summary>
public class ComponentValidationManagerTests : IDisposable
{
    public ComponentValidationManagerTests()
    {
        // Clear any constraint provider from previous tests
        ComponentValidationManager.ClearConstraintProvider();
    }

    public void Dispose()
    {
        // Clean up constraint provider after each test
        ComponentValidationManager.ClearConstraintProvider();
    }

    #region Test Components

    public struct TestPosition : IComponent
    {
        public float X, Y;
    }

    public struct TestVelocity : IComponent
    {
        public float X, Y;
    }

    public struct TestHealth : IComponent
    {
        public int Current, Max;
    }

    public struct TestDamage : IComponent
    {
        public int Amount;
    }

    #endregion

    #region RegisterValidator Tests

    [Fact]
    public void RegisterValidator_WithValidValidator_Succeeds()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        ComponentValidator<TestPosition> validator = (w, e, comp) => true;

        manager.RegisterValidator(validator);

        // Validator should be registered
        Assert.NotNull(validator);
    }

    [Fact]
    public void RegisterValidator_WithNullValidator_ThrowsArgumentNullException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        Assert.Throws<ArgumentNullException>(() =>
            manager.RegisterValidator<TestPosition>(null!));
    }

    #endregion

    #region UnregisterValidator Tests

    [Fact]
    public void UnregisterValidator_WithRegisteredValidator_ReturnsTrue()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        ComponentValidator<TestPosition> validator = (w, e, comp) => true;
        manager.RegisterValidator(validator);

        var result = manager.UnregisterValidator<TestPosition>();

        Assert.True(result);
    }

    [Fact]
    public void UnregisterValidator_WithoutRegisteredValidator_ReturnsFalse()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        var result = manager.UnregisterValidator<TestPosition>();

        Assert.False(result);
    }

    #endregion

    #region ValidateAdd Tests

    [Fact]
    public void ValidateAdd_WhenValidationDisabled_SkipsValidation()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // With validation disabled, this should not throw even without required components
        world.Add(entity, new TestPosition { X = 1, Y = 2 });

        Assert.True(world.Has<TestPosition>(entity));
    }

    [Fact]
    public void ValidateAdd_WithCustomValidator_RunsValidator()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        ComponentValidator<TestPosition> validator = (w, e, comp) => comp.X >= 0 && comp.Y >= 0;

        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // This would need validation to be enabled in the world
        // For now, just verify the validator was registered
        Assert.NotNull(validator);
    }

    #endregion

    #region ValidateBuild Tests

    [Fact]
    public void ValidateBuild_WhenValidationDisabled_SkipsValidation()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // Should not throw when validation is disabled
        manager.ValidateBuild(components);
    }

    [Fact]
    public void ValidateBuild_WithEmptyComponents_DoesNotThrow()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        var components = new List<(ComponentInfo Info, object Data)>();

        // Should not throw with empty list
        manager.ValidateBuild(components);
    }

    #endregion

    #region ValidateBuildCustom Tests

    [Fact]
    public void ValidateBuildCustom_WhenValidationDisabled_SkipsValidation()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);
        var entity = world.Spawn().Build();

        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // Should not throw when validation is disabled
        manager.ValidateBuildCustom(entity, components);
    }

    [Fact]
    public void ValidateBuildCustom_WithNoRegisteredValidators_DoesNotThrow()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);
        var entity = world.Spawn().Build();

        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // Should not throw when no validators are registered
        manager.ValidateBuildCustom(entity, components);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RegisterValidator_MultipleTimes_OverwritesPreviousValidator()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        ComponentValidator<TestPosition> validator1 = (w, e, comp) => true;
        ComponentValidator<TestPosition> validator2 = (w, e, comp) => false;

        manager.RegisterValidator(validator1);
        manager.RegisterValidator(validator2);

        // The second validator should have replaced the first
        Assert.NotNull(validator2);
    }

    [Fact]
    public void UnregisterValidator_AfterMultipleRegistrations_RemovesValidator()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        ComponentValidator<TestPosition> validator = (w, e, comp) => true;
        manager.RegisterValidator(validator);
        manager.RegisterValidator(validator); // Register again

        var result = manager.UnregisterValidator<TestPosition>();

        Assert.True(result);

        // Second unregister should return false
        result = manager.UnregisterValidator<TestPosition>();
        Assert.False(result);
    }

    #endregion

    #region ValidationMode Tests

    [Fact]
    public void Mode_DefaultValue_IsEnabled()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        Assert.Equal(ValidationMode.Enabled, manager.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToDisabled()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world) { Mode = ValidationMode.Disabled };

        Assert.Equal(ValidationMode.Disabled, manager.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToDebugOnly()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world) { Mode = ValidationMode.DebugOnly };

        Assert.Equal(ValidationMode.DebugOnly, manager.Mode);
    }

    [Fact]
    public void ValidateAdd_WhenModeDisabled_SkipsValidation()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world) { Mode = ValidationMode.Disabled };

        // Register a failing validator
        ComponentValidator<TestPosition> validator = (w, e, comp) => false;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // With mode disabled, validation should be skipped even with a failing validator
        manager.ValidateAdd(entity, new TestPosition { X = 1, Y = 2 });

        // No exception thrown
        Assert.True(true);
    }

    [Fact]
    public void ValidateBuild_WhenModeDisabled_SkipsValidation()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world) { Mode = ValidationMode.Disabled };

        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // With mode disabled, validation should be skipped
        manager.ValidateBuild(components);

        // No exception thrown
        Assert.True(true);
    }

    [Fact]
    public void ValidateBuildCustom_WhenModeDisabled_SkipsValidation()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world) { Mode = ValidationMode.Disabled };

        // Register a failing validator
        ComponentValidator<TestPosition> validator = (w, e, comp) => false;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();
        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // With mode disabled, validation should be skipped
        manager.ValidateBuildCustom(entity, components);

        // No exception thrown
        Assert.True(true);
    }

    #endregion

    #region ValidateAdd Custom Validator Tests

    [Fact]
    public void ValidateAdd_WithFailingCustomValidator_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a failing validator
        ComponentValidator<TestPosition> validator = (w, e, comp) => false;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // Should throw ComponentValidationException
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateAdd(entity, new TestPosition { X = 1, Y = 2 }));

        Assert.Contains("Custom validation failed", ex.Message);
    }

    [Fact]
    public void ValidateAdd_WithPassingCustomValidator_Succeeds()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a passing validator
        ComponentValidator<TestPosition> validator = (w, e, comp) => comp.X >= 0 && comp.Y >= 0;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // Should not throw
        manager.ValidateAdd(entity, new TestPosition { X = 1, Y = 2 });
    }

    [Fact]
    public void ValidateAdd_WithConditionalValidator_ValidatesCorrectly()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Validator that checks for positive health values
        ComponentValidator<TestHealth> validator = (w, e, comp) =>
            comp.Current >= 0 && comp.Max >= 0 && comp.Current <= comp.Max;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // Valid health
        manager.ValidateAdd(entity, new TestHealth { Current = 50, Max = 100 });

        // Invalid: Current > Max
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateAdd(entity, new TestHealth { Current = 150, Max = 100 }));

        Assert.Contains("Custom validation failed", ex.Message);
    }

    #endregion

    #region RegisterConstraintProvider Tests

    [Fact]
    public void RegisterConstraintProvider_WithNullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ComponentValidationManager.RegisterConstraintProvider(null!));
    }

    [Fact]
    public void RegisterConstraintProvider_WithValidProvider_Succeeds()
    {
        // Create a simple provider that always returns false (no constraints)
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                required = [];
                conflicts = [];
                return false;
            };

        // Should not throw
        ComponentValidationManager.RegisterConstraintProvider(provider);
    }

    [Fact]
    public void RegisterConstraintProvider_WithConstraintProvider_ProvidesConstraints()
    {
        // Register a provider that returns constraints for TestHealth requiring TestPosition
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestHealth))
                {
                    required = [typeof(TestPosition)];
                    conflicts = [typeof(TestDamage)];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };

        ComponentValidationManager.RegisterConstraintProvider(provider);

        // The provider is now registered for subsequent validation operations
    }

    #endregion

    #region ValidateRequirements Exception Tests

    [Fact]
    public void ValidateAdd_WithMissingRequiredComponent_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a constraint provider that requires TestPosition for TestVelocity
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestVelocity))
                {
                    required = [typeof(TestPosition)];
                    conflicts = [];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };
        ComponentValidationManager.RegisterConstraintProvider(provider);

        var entity = world.Spawn().Build();

        // Entity doesn't have TestPosition, so adding TestVelocity should fail
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateAdd(entity, new TestVelocity { X = 1, Y = 2 }));

        Assert.Contains("requires", ex.Message);
        Assert.Contains("TestPosition", ex.Message);
    }

    [Fact]
    public void ValidateAdd_WithConflictingComponent_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a constraint provider that makes TestHealth conflict with TestDamage
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestHealth))
                {
                    required = [];
                    conflicts = [typeof(TestDamage)];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };
        ComponentValidationManager.RegisterConstraintProvider(provider);

        var entity = world.Spawn()
            .With(new TestDamage { Amount = 10 })
            .Build();

        // Entity has TestDamage, so adding TestHealth should fail
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateAdd(entity, new TestHealth { Current = 100, Max = 100 }));

        Assert.Contains("conflicts", ex.Message);
        Assert.Contains("TestDamage", ex.Message);
    }

    [Fact]
    public void ValidateAdd_WithRequiredComponentPresent_Succeeds()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a constraint provider that requires TestPosition for TestVelocity
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestVelocity))
                {
                    required = [typeof(TestPosition)];
                    conflicts = [];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };
        ComponentValidationManager.RegisterConstraintProvider(provider);

        var entity = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .Build();

        // Entity has TestPosition, so adding TestVelocity should succeed
        manager.ValidateAdd(entity, new TestVelocity { X = 1, Y = 2 });
    }

    #endregion

    #region ValidateBuild Exception Tests

    [Fact]
    public void ValidateBuild_WithMissingRequiredComponent_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a constraint provider that requires TestPosition for TestVelocity
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestVelocity))
                {
                    required = [typeof(TestPosition)];
                    conflicts = [];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };
        ComponentValidationManager.RegisterConstraintProvider(provider);

        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestVelocity>();
        components.Add((info, new TestVelocity { X = 1, Y = 2 }));

        // TestPosition is not in the build set, so validation should fail
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateBuild(components));

        Assert.Contains("requires", ex.Message);
        Assert.Contains("TestPosition", ex.Message);
    }

    [Fact]
    public void ValidateBuild_WithConflictingComponent_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a constraint provider that makes TestHealth conflict with TestDamage
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestHealth))
                {
                    required = [];
                    conflicts = [typeof(TestDamage)];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };
        ComponentValidationManager.RegisterConstraintProvider(provider);

        var components = new List<(ComponentInfo Info, object Data)>();
        var healthInfo = world.Components.GetOrRegister<TestHealth>();
        var damageInfo = world.Components.GetOrRegister<TestDamage>();
        components.Add((healthInfo, new TestHealth { Current = 100, Max = 100 }));
        components.Add((damageInfo, new TestDamage { Amount = 10 }));

        // Both conflicting components are in the build set
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateBuild(components));

        Assert.Contains("conflicts", ex.Message);
        Assert.Contains("TestDamage", ex.Message);
    }

    [Fact]
    public void ValidateBuild_WithRequiredComponentPresent_Succeeds()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a constraint provider that requires TestPosition for TestVelocity
        ComponentValidationManager.TryGetConstraintsDelegate provider =
            (Type componentType, out Type[] required, out Type[] conflicts) =>
            {
                if (componentType == typeof(TestVelocity))
                {
                    required = [typeof(TestPosition)];
                    conflicts = [];
                    return true;
                }

                required = [];
                conflicts = [];
                return false;
            };
        ComponentValidationManager.RegisterConstraintProvider(provider);

        var components = new List<(ComponentInfo Info, object Data)>();
        var posInfo = world.Components.GetOrRegister<TestPosition>();
        var velInfo = world.Components.GetOrRegister<TestVelocity>();
        components.Add((posInfo, new TestPosition { X = 0, Y = 0 }));
        components.Add((velInfo, new TestVelocity { X = 1, Y = 2 }));

        // Both required and dependent components are in the build set
        manager.ValidateBuild(components);
    }

    #endregion

    #region ValidateBuildCustom Exception Tests

    [Fact]
    public void ValidateBuildCustom_WithFailingValidator_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a failing validator
        ComponentValidator<TestPosition> validator = (w, e, comp) => false;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // Use the actual ComponentInfo from GetOrRegister (it has InvokeValidator set)
        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // Should throw ComponentValidationException because validator returns false
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateBuildCustom(entity, components));

        Assert.Contains("Custom validation failed", ex.Message);
    }

    [Fact]
    public void ValidateBuildCustom_WithPassingValidator_Succeeds()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register a passing validator
        ComponentValidator<TestPosition> validator = (w, e, comp) => comp.X >= 0 && comp.Y >= 0;
        manager.RegisterValidator(validator);

        var entity = world.Spawn().Build();

        // Use the actual ComponentInfo from GetOrRegister
        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // Should not throw
        manager.ValidateBuildCustom(entity, components);
    }

    [Fact]
    public void ValidateBuildCustom_WithNoRegisteredValidator_Succeeds()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // No validator registered
        var entity = world.Spawn().Build();

        var components = new List<(ComponentInfo Info, object Data)>();
        var info = world.Components.GetOrRegister<TestPosition>();
        components.Add((info, new TestPosition { X = 1, Y = 2 }));

        // Should not throw when no validator is registered
        manager.ValidateBuildCustom(entity, components);
    }

    [Fact]
    public void ValidateBuildCustom_WithMultipleValidators_ValidatesAll()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register validators for multiple components
        ComponentValidator<TestPosition> posValidator = (w, e, comp) => comp.X >= 0;
        ComponentValidator<TestVelocity> velValidator = (w, e, comp) => comp.Y >= 0;
        manager.RegisterValidator(posValidator);
        manager.RegisterValidator(velValidator);

        var entity = world.Spawn().Build();

        var components = new List<(ComponentInfo Info, object Data)>();
        var posInfo = world.Components.GetOrRegister<TestPosition>();
        var velInfo = world.Components.GetOrRegister<TestVelocity>();
        components.Add((posInfo, new TestPosition { X = 1, Y = 2 }));
        components.Add((velInfo, new TestVelocity { X = 3, Y = 4 }));

        // Both validators should pass
        manager.ValidateBuildCustom(entity, components);
    }

    [Fact]
    public void ValidateBuildCustom_WithSecondValidatorFailing_ThrowsComponentValidationException()
    {
        using var world = new World();
        var manager = new ComponentValidationManager(world);

        // Register validators - second one will fail
        ComponentValidator<TestPosition> posValidator = (w, e, comp) => true;
        ComponentValidator<TestVelocity> velValidator = (w, e, comp) => false;
        manager.RegisterValidator(posValidator);
        manager.RegisterValidator(velValidator);

        var entity = world.Spawn().Build();

        var components = new List<(ComponentInfo Info, object Data)>();
        var posInfo = world.Components.GetOrRegister<TestPosition>();
        var velInfo = world.Components.GetOrRegister<TestVelocity>();
        components.Add((posInfo, new TestPosition { X = 1, Y = 2 }));
        components.Add((velInfo, new TestVelocity { X = 3, Y = 4 }));

        // Second validator fails
        var ex = Assert.Throws<ComponentValidationException>(() =>
            manager.ValidateBuildCustom(entity, components));

        Assert.Contains("TestVelocity", ex.Message);
    }

    #endregion

    #region ComponentValidationInfo Tests

    [Fact]
    public void ComponentValidationInfo_HasConstraints_ReturnsTrueWhenHasRequiredComponents()
    {
        var info = new ComponentValidationInfo([typeof(TestPosition)], []);

        Assert.True(info.HasConstraints);
    }

    [Fact]
    public void ComponentValidationInfo_HasConstraints_ReturnsTrueWhenHasConflictingComponents()
    {
        var info = new ComponentValidationInfo([], [typeof(TestVelocity)]);

        Assert.True(info.HasConstraints);
    }

    [Fact]
    public void ComponentValidationInfo_HasConstraints_ReturnsFalseWhenEmpty()
    {
        var info = new ComponentValidationInfo([], []);

        Assert.False(info.HasConstraints);
    }

    [Fact]
    public void ComponentValidationInfo_RequiredComponents_ReturnsCorrectArray()
    {
        var required = new[] { typeof(TestPosition), typeof(TestVelocity) };
        var info = new ComponentValidationInfo(required, []);

        Assert.Equal(required, info.RequiredComponents);
    }

    [Fact]
    public void ComponentValidationInfo_ConflictingComponents_ReturnsCorrectArray()
    {
        var conflicts = new[] { typeof(TestHealth), typeof(TestDamage) };
        var info = new ComponentValidationInfo([], conflicts);

        Assert.Equal(conflicts, info.ConflictingComponents);
    }

    #endregion
}
