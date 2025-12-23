namespace KeenEyes.Core.Tests;

/// <summary>
/// Tests for World.ValidationMode property and validation-related methods.
/// </summary>
public sealed class WorldValidationTests
{
    private readonly struct TestComponent : IComponent
    {
        public int Value { get; init; }
    }

    [Fact]
    public void ValidationMode_Get_ReturnsCurrentMode()
    {
        using var world = new World();

        // Default should be Enabled
        Assert.Equal(ValidationMode.Enabled, world.ValidationMode);
    }

    [Fact]
    public void ValidationMode_Set_UpdatesMode()
    {
        using var world = new World();

        world.ValidationMode = ValidationMode.Disabled;
        Assert.Equal(ValidationMode.Disabled, world.ValidationMode);

        world.ValidationMode = ValidationMode.DebugOnly;
        Assert.Equal(ValidationMode.DebugOnly, world.ValidationMode);

        world.ValidationMode = ValidationMode.Enabled;
        Assert.Equal(ValidationMode.Enabled, world.ValidationMode);
    }

    [Fact]
    public void RegisterValidator_WithValidValidator_Succeeds()
    {
        using var world = new World();

        // Register a validator that checks Value is positive
        world.RegisterValidator<TestComponent>((w, e, c) => c.Value > 0);

        var entity = world.Spawn().Build();

        // Valid component should succeed
        world.Add(entity, new TestComponent { Value = 10 });
        Assert.True(world.Has<TestComponent>(entity));
    }

    [Fact]
    public void RegisterValidator_WithFailingValidator_ThrowsComponentValidationException()
    {
        using var world = new World();

        // Register a validator that checks Value is positive
        world.RegisterValidator<TestComponent>((w, e, c) => c.Value > 0);

        var entity = world.Spawn().Build();

        // Invalid component should throw
        var ex = Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new TestComponent { Value = -5 }));

        Assert.Contains("TestComponent", ex.Message);
    }

    [Fact]
    public void UnregisterValidator_RemovesPreviouslyRegisteredValidator()
    {
        using var world = new World();

        // Register a strict validator
        world.RegisterValidator<TestComponent>((w, e, c) => c.Value > 0);

        var entity = world.Spawn().Build();

        // Should throw with validator
        Assert.Throws<ComponentValidationException>(() =>
            world.Add(entity, new TestComponent { Value = -5 }));

        // Unregister the validator
        var removed = world.UnregisterValidator<TestComponent>();
        Assert.True(removed);

        // Should now succeed without validator
        world.Add(entity, new TestComponent { Value = -5 });
        Assert.True(world.Has<TestComponent>(entity));
    }

    [Fact]
    public void UnregisterValidator_WhenNoValidatorRegistered_ReturnsFalse()
    {
        using var world = new World();

        var removed = world.UnregisterValidator<TestComponent>();
        Assert.False(removed);
    }

    [Fact]
    public void ValidationMode_Disabled_SkipsCustomValidation()
    {
        using var world = new World();

        // Register a strict validator
        world.RegisterValidator<TestComponent>((w, e, c) => c.Value > 0);

        // Disable validation
        world.ValidationMode = ValidationMode.Disabled;

        var entity = world.Spawn().Build();

        // Should succeed even with invalid component when validation is disabled
        world.Add(entity, new TestComponent { Value = -5 });
        Assert.True(world.Has<TestComponent>(entity));
    }
}
