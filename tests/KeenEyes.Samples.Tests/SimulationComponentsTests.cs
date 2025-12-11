using KeenEyes.Sample.Simulation;

namespace KeenEyes.Samples.Tests;

/// <summary>
/// Tests verifying that Simulation sample components follow ECS principles.
/// </summary>
public class SimulationComponentsTests
{
    #region Health Component Tests

    /// <summary>
    /// Verifies that the Health component contains only data fields (no computed properties).
    /// This ensures we follow ECS principles where components are pure data.
    /// </summary>
    [Fact]
    public void Health_IsStructWithFieldsOnly_FollowsECSPrinciples()
    {
        var healthType = typeof(Health);

        // Verify it's a struct
        Assert.True(healthType.IsValueType, "Health should be a struct");

        // Get all instance properties (excluding auto-generated properties for fields)
        var properties = healthType.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        // Health should have no properties (computed or otherwise)
        // All data should be stored in fields
        Assert.Empty(properties);
    }

    /// <summary>
    /// Verifies that the Health component can be instantiated and used as pure data.
    /// </summary>
    [Fact]
    public void Health_CanBeCreatedAndUsedAsPureData()
    {
        var health = new Health
        {
            Current = 75,
            Max = 100
        };

        Assert.Equal(75, health.Current);
        Assert.Equal(100, health.Max);
    }

    /// <summary>
    /// Verifies health state checks work by directly accessing fields.
    /// This demonstrates the correct pattern: logic in systems, not components.
    /// </summary>
    [Fact]
    public void Health_StateChecks_WorkByAccessingFieldsDirectly()
    {
        var aliveHealth = new Health { Current = 50, Max = 100 };
        var deadHealth = new Health { Current = 0, Max = 100 };
        var negativeHealth = new Health { Current = -10, Max = 100 };

        // Alive check: Current > 0
        Assert.True(aliveHealth.Current > 0);
        Assert.False(deadHealth.Current > 0);
        Assert.False(negativeHealth.Current > 0);

        // Health percentage: Current / Max
        Assert.Equal(0.5f, aliveHealth.Max > 0 ? (float)aliveHealth.Current / aliveHealth.Max : 0);
        Assert.Equal(0f, deadHealth.Max > 0 ? (float)deadHealth.Current / deadHealth.Max : 0);
    }

    #endregion

    #region Other Component Structure Tests

    /// <summary>
    /// Verifies Position component follows pure data pattern.
    /// </summary>
    [Fact]
    public void Position_IsStructWithFieldsOnly()
    {
        var type = typeof(Position);
        Assert.True(type.IsValueType);

        var properties = type.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        Assert.Empty(properties);
    }

    /// <summary>
    /// Verifies Velocity component follows pure data pattern.
    /// </summary>
    [Fact]
    public void Velocity_IsStructWithFieldsOnly()
    {
        var type = typeof(Velocity);
        Assert.True(type.IsValueType);

        var properties = type.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        Assert.Empty(properties);
    }

    /// <summary>
    /// Verifies Damage component follows pure data pattern.
    /// </summary>
    [Fact]
    public void Damage_IsStructWithFieldsOnly()
    {
        var type = typeof(Damage);
        Assert.True(type.IsValueType);

        var properties = type.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        Assert.Empty(properties);
    }

    #endregion
}
