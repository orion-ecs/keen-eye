using System.Reflection;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Guards the ordinal coupling between the generator's private SystemPhase mirror enum
/// and the public <see cref="KeenEyes.SystemPhase"/>. The generator casts the attribute's
/// raw integer value to its private enum and embeds the resulting member name in generated
/// code, so any reorder or rename of either enum must fail this test.
/// </summary>
public class SystemPhaseParityTests
{
    [Fact]
    public void SystemGeneratorSystemPhase_MatchesAbstractionsSystemPhase_ByNameAndValue()
    {
        var generatorPhaseEnum = typeof(SystemGenerator).GetNestedType("SystemPhase", BindingFlags.NonPublic);

        Assert.NotNull(generatorPhaseEnum);
        Assert.True(generatorPhaseEnum!.IsEnum);

        var expectedByName = Enum.GetValues<KeenEyes.SystemPhase>()
            .ToDictionary(value => value.ToString(), value => (int)value);

        var actualNames = Enum.GetNames(generatorPhaseEnum);

        Assert.Equal(expectedByName.Count, actualNames.Length);

        foreach (var name in actualNames)
        {
            Assert.True(
                expectedByName.TryGetValue(name, out var expectedValue),
                $"Generator SystemPhase member '{name}' does not exist on KeenEyes.SystemPhase.");

            var actualValue = Convert.ToInt32(Enum.Parse(generatorPhaseEnum, name));

            Assert.True(
                expectedValue == actualValue,
                $"Generator SystemPhase.{name} has value {actualValue} but KeenEyes.SystemPhase.{name} has value {expectedValue}. " +
                "The two enums must stay in ordinal lockstep or generated code will silently target the wrong phase.");
        }
    }
}
