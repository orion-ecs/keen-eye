namespace KeenEyes.Tests;

/// <summary>
/// Shared test component types for use across multiple test files.
/// These complement the existing TestPosition, TestVelocity, TestHealth, TestRotation
/// types defined in GetComponentTests.cs and QueryTests.cs.
/// </summary>

/// <summary>
/// A scale component for testing.
/// </summary>
public struct TestScale : IComponent
{
    public float X;
    public float Y;
}

#region Tag Components

/// <summary>
/// An enemy tag for testing.
/// </summary>
public struct TestEnemyTag : ITagComponent;

/// <summary>
/// An active tag for testing.
/// </summary>
public struct TestActiveTag : ITagComponent;

/// <summary>
/// A frozen tag for testing.
/// </summary>
public struct TestFrozenTag : ITagComponent;

#endregion
