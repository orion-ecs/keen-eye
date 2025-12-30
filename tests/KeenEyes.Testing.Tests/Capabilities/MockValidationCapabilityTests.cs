using KeenEyes.Capabilities;
using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockValidationCapabilityTests
{
    #region ValidationMode

    [Fact]
    public void ValidationMode_InitiallyEnabled()
    {
        var capability = new MockValidationCapability();

        Assert.Equal(ValidationMode.Enabled, capability.ValidationMode);
    }

    [Fact]
    public void ValidationMode_CanBeSet()
    {
        var capability = new MockValidationCapability
        {
            ValidationMode = ValidationMode.Disabled
        };

        Assert.Equal(ValidationMode.Disabled, capability.ValidationMode);
    }

    #endregion

    #region RegisterValidator

    [Fact]
    public void RegisterValidator_RegistersValidator()
    {
        var capability = new MockValidationCapability();
        ComponentValidator<TestComponent> validator = (world, entity, component) => true;

        capability.RegisterValidator(validator);

        Assert.True(capability.HasValidator<TestComponent>());
    }

    [Fact]
    public void RegisterValidator_TracksType()
    {
        var capability = new MockValidationCapability();
        ComponentValidator<TestComponent> validator = (world, entity, component) => true;

        capability.RegisterValidator(validator);

        Assert.Single(capability.RegisteredValidatorTypes);
        Assert.Equal(typeof(TestComponent), capability.RegisteredValidatorTypes[0]);
    }

    [Fact]
    public void RegisterValidator_WithNull_ThrowsArgumentNullException()
    {
        var capability = new MockValidationCapability();

        Assert.Throws<ArgumentNullException>(() => capability.RegisterValidator<TestComponent>(null!));
    }

    #endregion

    #region UnregisterValidator

    [Fact]
    public void UnregisterValidator_WhenRegistered_ReturnsTrue()
    {
        var capability = new MockValidationCapability();
        ComponentValidator<TestComponent> validator = (world, entity, component) => true;
        capability.RegisterValidator(validator);

        var result = capability.UnregisterValidator<TestComponent>();

        Assert.True(result);
        Assert.False(capability.HasValidator<TestComponent>());
    }

    [Fact]
    public void UnregisterValidator_WhenNotRegistered_ReturnsFalse()
    {
        var capability = new MockValidationCapability();

        var result = capability.UnregisterValidator<TestComponent>();

        Assert.False(result);
    }

    #endregion

    #region GetValidator

    [Fact]
    public void GetValidator_WhenRegistered_ReturnsValidator()
    {
        var capability = new MockValidationCapability();
        ComponentValidator<TestComponent> validator = (world, entity, component) => component.Value > 0;
        capability.RegisterValidator(validator);

        var retrieved = capability.GetValidator<TestComponent>();

        Assert.NotNull(retrieved);
        // Test the validator works
        var entity = new Entity(1, 0);
        var component = new TestComponent { Value = 5 };
        Assert.True(retrieved(null!, entity, component));
    }

    [Fact]
    public void GetValidator_WhenNotRegistered_ReturnsNull()
    {
        var capability = new MockValidationCapability();

        var validator = capability.GetValidator<TestComponent>();

        Assert.Null(validator);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllValidators()
    {
        var capability = new MockValidationCapability();
        ComponentValidator<TestComponent> validator = (world, entity, component) => true;
        capability.RegisterValidator(validator);

        capability.Clear();

        Assert.False(capability.HasValidator<TestComponent>());
        Assert.Empty(capability.RegisteredValidatorTypes);
    }

    #endregion

    private struct TestComponent : IComponent
    {
        public int Value;
    }
}
