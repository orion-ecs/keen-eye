using System.Numerics;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Unit tests for the <see cref="GhostFrame"/> struct.
/// </summary>
public class GhostFrameTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        var elapsedTime = TimeSpan.FromSeconds(1.5);

        // Act
        var frame = new GhostFrame(position, rotation, elapsedTime);

        // Assert
        Assert.Equal(position, frame.Position);
        Assert.Equal(rotation, frame.Rotation);
        Assert.Equal(elapsedTime, frame.ElapsedTime);
    }

    [Fact]
    public void Constructor_DefaultScale_IsOne()
    {
        // Act
        var frame = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero);

        // Assert
        Assert.Equal(Vector3.One, frame.Scale);
    }

    [Fact]
    public void Constructor_DefaultDistance_IsZero()
    {
        // Act
        var frame = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero);

        // Assert
        Assert.Equal(0f, frame.Distance);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity;
        var scale = new Vector3(2, 2, 2);
        var elapsedTime = TimeSpan.FromSeconds(1);
        var distance = 10f;

        // Act
        var frame = GhostFrame.Create(position, rotation, scale, elapsedTime, distance);

        // Assert
        Assert.Equal(position, frame.Position);
        Assert.Equal(rotation, frame.Rotation);
        Assert.Equal(scale, frame.Scale);
        Assert.Equal(elapsedTime, frame.ElapsedTime);
        Assert.Equal(distance, frame.Distance);
    }

    #endregion

    #region Lerp Tests

    [Fact]
    public void Lerp_AtZero_ReturnsFirstFrame()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
        {
            Scale = Vector3.One,
            Distance = 0f
        };
        var frameB = new GhostFrame(new Vector3(10, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1))
        {
            Scale = new Vector3(2, 2, 2),
            Distance = 10f
        };

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, 0f);

        // Assert
        Assert.Equal(Vector3.Zero, result.Position);
        Assert.Equal(Vector3.One, result.Scale);
        Assert.Equal(0f, result.Distance);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsSecondFrame()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
        {
            Scale = Vector3.One,
            Distance = 0f
        };
        var frameB = new GhostFrame(new Vector3(10, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1))
        {
            Scale = new Vector3(2, 2, 2),
            Distance = 10f
        };

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, 1f);

        // Assert
        Assert.Equal(new Vector3(10, 0, 0), result.Position);
        Assert.Equal(new Vector3(2, 2, 2), result.Scale);
        Assert.Equal(10f, result.Distance);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsInterpolatedFrame()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
        {
            Scale = Vector3.One,
            Distance = 0f
        };
        var frameB = new GhostFrame(new Vector3(10, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1))
        {
            Scale = new Vector3(2, 2, 2),
            Distance = 10f
        };

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, 0.5f);

        // Assert
        Assert.Equal(new Vector3(5, 0, 0), result.Position);
        Assert.Equal(new Vector3(1.5f, 1.5f, 1.5f), result.Scale);
        Assert.Equal(5f, result.Distance);
    }

    [Fact]
    public void Lerp_InterpolatesRotation()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero);
        var frameB = new GhostFrame(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI), TimeSpan.FromSeconds(1));

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, 0.5f);

        // Assert - should be approximately 90 degrees
        Assert.NotEqual(Quaternion.Identity, result.Rotation);
        Assert.NotEqual(frameB.Rotation, result.Rotation);
    }

    [Fact]
    public void Lerp_InterpolatesElapsedTime()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero);
        var frameB = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.FromSeconds(2));

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, 0.5f);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), result.ElapsedTime);
    }

    [Fact]
    public void Lerp_ClampsNegativeT()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero);
        var frameB = new GhostFrame(new Vector3(10, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1));

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, -1f);

        // Assert
        Assert.Equal(Vector3.Zero, result.Position);
    }

    [Fact]
    public void Lerp_ClampsAboveOneT()
    {
        // Arrange
        var frameA = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero);
        var frameB = new GhostFrame(new Vector3(10, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1));

        // Act
        var result = GhostFrame.Lerp(frameA, frameB, 2f);

        // Assert
        Assert.Equal(new Vector3(10, 0, 0), result.Position);
    }

    #endregion

    #region Property Init Tests

    [Fact]
    public void Distance_CanBeSet()
    {
        // Act
        var frame = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
        {
            Distance = 100f
        };

        // Assert
        Assert.Equal(100f, frame.Distance);
    }

    [Fact]
    public void Scale_CanBeSet()
    {
        // Act
        var frame = new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
        {
            Scale = new Vector3(2, 3, 4)
        };

        // Assert
        Assert.Equal(new Vector3(2, 3, 4), frame.Scale);
    }

    #endregion
}
