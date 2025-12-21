using System.Numerics;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Comprehensive tests for PhysicsConfig validation and defaults.
/// </summary>
public class PhysicsConfigTests
{
    #region Default Configuration Tests

    [Fact]
    public void PhysicsConfig_Default_HasCorrectValues()
    {
        var config = PhysicsConfig.Default;

        Assert.Equal(1f / 60f, config.FixedTimestep);
        Assert.Equal(3, config.MaxStepsPerFrame);
        Assert.Equal(new Vector3(0, -9.81f, 0), config.Gravity);
        Assert.Equal(8, config.VelocityIterations);
        Assert.Equal(1, config.SubstepCount);
        Assert.True(config.EnableInterpolation);
        Assert.Equal(1024, config.InitialBodyCapacity);
        Assert.Equal(1024, config.InitialStaticCapacity);
        Assert.Equal(2048, config.InitialConstraintCapacity);
    }

    [Fact]
    public void PhysicsConfig_DefaultConstructor_MatchesDefaultProperty()
    {
        var config = new PhysicsConfig();
        var defaultConfig = PhysicsConfig.Default;

        Assert.Equal(defaultConfig.FixedTimestep, config.FixedTimestep);
        Assert.Equal(defaultConfig.MaxStepsPerFrame, config.MaxStepsPerFrame);
        Assert.Equal(defaultConfig.Gravity, config.Gravity);
        Assert.Equal(defaultConfig.VelocityIterations, config.VelocityIterations);
        Assert.Equal(defaultConfig.SubstepCount, config.SubstepCount);
        Assert.Equal(defaultConfig.EnableInterpolation, config.EnableInterpolation);
        Assert.Equal(defaultConfig.InitialBodyCapacity, config.InitialBodyCapacity);
        Assert.Equal(defaultConfig.InitialStaticCapacity, config.InitialStaticCapacity);
        Assert.Equal(defaultConfig.InitialConstraintCapacity, config.InitialConstraintCapacity);
    }

    #endregion

    #region Validation Tests - FixedTimestep

    [Fact]
    public void Validate_ValidFixedTimestep_ReturnsNull()
    {
        var config = new PhysicsConfig { FixedTimestep = 1f / 60f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_ZeroFixedTimestep_ReturnsError()
    {
        var config = new PhysicsConfig { FixedTimestep = 0f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("FixedTimestep must be positive", error);
    }

    [Fact]
    public void Validate_NegativeFixedTimestep_ReturnsError()
    {
        var config = new PhysicsConfig { FixedTimestep = -0.01f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("FixedTimestep must be positive", error);
    }

    [Fact]
    public void Validate_TooLargeFixedTimestep_ReturnsError()
    {
        var config = new PhysicsConfig { FixedTimestep = 0.11f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("FixedTimestep is too large", error);
    }

    [Fact]
    public void Validate_MaximumAllowedFixedTimestep_ReturnsNull()
    {
        var config = new PhysicsConfig { FixedTimestep = 0.1f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_MinimumFixedTimestep_ReturnsNull()
    {
        var config = new PhysicsConfig { FixedTimestep = 0.0001f };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Validation Tests - MaxStepsPerFrame

    [Fact]
    public void Validate_ZeroMaxStepsPerFrame_ReturnsError()
    {
        var config = new PhysicsConfig { MaxStepsPerFrame = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxStepsPerFrame must be at least 1", error);
    }

    [Fact]
    public void Validate_NegativeMaxStepsPerFrame_ReturnsError()
    {
        var config = new PhysicsConfig { MaxStepsPerFrame = -1 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxStepsPerFrame must be at least 1", error);
    }

    [Fact]
    public void Validate_OneMaxStepsPerFrame_ReturnsNull()
    {
        var config = new PhysicsConfig { MaxStepsPerFrame = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_LargeMaxStepsPerFrame_ReturnsNull()
    {
        var config = new PhysicsConfig { MaxStepsPerFrame = 100 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Validation Tests - VelocityIterations

    [Fact]
    public void Validate_ZeroVelocityIterations_ReturnsError()
    {
        var config = new PhysicsConfig { VelocityIterations = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("VelocityIterations must be at least 1", error);
    }

    [Fact]
    public void Validate_NegativeVelocityIterations_ReturnsError()
    {
        var config = new PhysicsConfig { VelocityIterations = -5 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("VelocityIterations must be at least 1", error);
    }

    [Fact]
    public void Validate_OneVelocityIteration_ReturnsNull()
    {
        var config = new PhysicsConfig { VelocityIterations = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_LargeVelocityIterations_ReturnsNull()
    {
        var config = new PhysicsConfig { VelocityIterations = 50 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Validation Tests - SubstepCount

    [Fact]
    public void Validate_ZeroSubstepCount_ReturnsError()
    {
        var config = new PhysicsConfig { SubstepCount = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("SubstepCount must be at least 1", error);
    }

    [Fact]
    public void Validate_NegativeSubstepCount_ReturnsError()
    {
        var config = new PhysicsConfig { SubstepCount = -2 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("SubstepCount must be at least 1", error);
    }

    [Fact]
    public void Validate_OneSubstep_ReturnsNull()
    {
        var config = new PhysicsConfig { SubstepCount = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_MultipleSubsteps_ReturnsNull()
    {
        var config = new PhysicsConfig { SubstepCount = 10 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Validation Tests - Capacity Settings

    [Fact]
    public void Validate_ZeroInitialBodyCapacity_ReturnsError()
    {
        var config = new PhysicsConfig { InitialBodyCapacity = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialBodyCapacity must be at least 1", error);
    }

    [Fact]
    public void Validate_NegativeInitialBodyCapacity_ReturnsError()
    {
        var config = new PhysicsConfig { InitialBodyCapacity = -10 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialBodyCapacity must be at least 1", error);
    }

    [Fact]
    public void Validate_ZeroInitialStaticCapacity_ReturnsError()
    {
        var config = new PhysicsConfig { InitialStaticCapacity = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialStaticCapacity must be at least 1", error);
    }

    [Fact]
    public void Validate_NegativeInitialStaticCapacity_ReturnsError()
    {
        var config = new PhysicsConfig { InitialStaticCapacity = -5 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialStaticCapacity must be at least 1", error);
    }

    [Fact]
    public void Validate_ZeroInitialConstraintCapacity_ReturnsError()
    {
        var config = new PhysicsConfig { InitialConstraintCapacity = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialConstraintCapacity must be at least 1", error);
    }

    [Fact]
    public void Validate_NegativeInitialConstraintCapacity_ReturnsError()
    {
        var config = new PhysicsConfig { InitialConstraintCapacity = -100 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("InitialConstraintCapacity must be at least 1", error);
    }

    [Fact]
    public void Validate_OneForAllCapacities_ReturnsNull()
    {
        var config = new PhysicsConfig
        {
            InitialBodyCapacity = 1,
            InitialStaticCapacity = 1,
            InitialConstraintCapacity = 1
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_LargeCapacities_ReturnsNull()
    {
        var config = new PhysicsConfig
        {
            InitialBodyCapacity = 100000,
            InitialStaticCapacity = 50000,
            InitialConstraintCapacity = 200000
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Validation Tests - Multiple Errors

    [Fact]
    public void Validate_MultipleInvalidProperties_ReturnsFirstError()
    {
        var config = new PhysicsConfig
        {
            FixedTimestep = -1f,
            MaxStepsPerFrame = 0,
            VelocityIterations = 0
        };

        var error = config.Validate();

        // Should return the first error encountered
        Assert.NotNull(error);
        Assert.Contains("FixedTimestep", error);
    }

    #endregion

    #region Custom Configuration Tests

    [Fact]
    public void PhysicsConfig_CustomGravity_CanBeSet()
    {
        var customGravity = new Vector3(0, -20f, 0);
        var config = new PhysicsConfig { Gravity = customGravity };

        Assert.Equal(customGravity, config.Gravity);
    }

    [Fact]
    public void PhysicsConfig_ZeroGravity_IsValid()
    {
        var config = new PhysicsConfig { Gravity = Vector3.Zero };

        var error = config.Validate();

        Assert.Null(error);
        Assert.Equal(Vector3.Zero, config.Gravity);
    }

    [Fact]
    public void PhysicsConfig_NegativeGravityAllAxes_IsValid()
    {
        var config = new PhysicsConfig { Gravity = new Vector3(-1f, -2f, -3f) };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void PhysicsConfig_DisableInterpolation_IsValid()
    {
        var config = new PhysicsConfig { EnableInterpolation = false };

        var error = config.Validate();

        Assert.Null(error);
        Assert.False(config.EnableInterpolation);
    }

    [Fact]
    public void PhysicsConfig_EnableInterpolation_IsValid()
    {
        var config = new PhysicsConfig { EnableInterpolation = true };

        var error = config.Validate();

        Assert.Null(error);
        Assert.True(config.EnableInterpolation);
    }

    #endregion

    #region High Frequency Physics Tests

    [Fact]
    public void PhysicsConfig_HighFrequencyPhysics_120Hz_IsValid()
    {
        var config = new PhysicsConfig { FixedTimestep = 1f / 120f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void PhysicsConfig_HighFrequencyPhysics_240Hz_IsValid()
    {
        var config = new PhysicsConfig { FixedTimestep = 1f / 240f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void PhysicsConfig_LowFrequencyPhysics_30Hz_IsValid()
    {
        var config = new PhysicsConfig { FixedTimestep = 1f / 30f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void PhysicsConfig_LowFrequencyPhysics_10Hz_IsValid()
    {
        var config = new PhysicsConfig { FixedTimestep = 1f / 10f };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion
}
