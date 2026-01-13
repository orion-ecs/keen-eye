using System.Numerics;

namespace KeenEyes.Animation.Data;

/// <summary>
/// A single keyframe in an animation curve containing a time and value.
/// </summary>
/// <typeparam name="T">The value type (float, Vector2, Vector3, Quaternion).</typeparam>
/// <param name="Time">The time of this keyframe in seconds from the start of the clip.</param>
/// <param name="Value">The value at this keyframe.</param>
public readonly record struct Keyframe<T>(float Time, T Value) where T : struct;

/// <summary>
/// A float keyframe with tangent information for smooth interpolation.
/// </summary>
/// <param name="Time">The time of this keyframe in seconds.</param>
/// <param name="Value">The value at this keyframe.</param>
/// <param name="InTangent">The incoming tangent slope.</param>
/// <param name="OutTangent">The outgoing tangent slope.</param>
public readonly record struct FloatKeyframe(float Time, float Value, float InTangent = 0f, float OutTangent = 0f);

/// <summary>
/// A Vector3 keyframe for position or scale animation.
/// </summary>
/// <param name="Time">The time of this keyframe in seconds.</param>
/// <param name="Value">The value at this keyframe.</param>
public readonly record struct Vector3Keyframe(float Time, Vector3 Value);

/// <summary>
/// A Quaternion keyframe for rotation animation.
/// </summary>
/// <param name="Time">The time of this keyframe in seconds.</param>
/// <param name="Value">The rotation value at this keyframe.</param>
public readonly record struct QuaternionKeyframe(float Time, Quaternion Value);

/// <summary>
/// A Vector3 keyframe with tangent information for cubic spline interpolation.
/// </summary>
/// <param name="Time">The time of this keyframe in seconds.</param>
/// <param name="Value">The value at this keyframe.</param>
/// <param name="InTangent">The incoming tangent vector.</param>
/// <param name="OutTangent">The outgoing tangent vector.</param>
public readonly record struct CubicSplineVector3Keyframe(
    float Time,
    Vector3 Value,
    Vector3 InTangent,
    Vector3 OutTangent);

/// <summary>
/// A Quaternion keyframe with tangent information for cubic spline interpolation.
/// </summary>
/// <param name="Time">The time of this keyframe in seconds.</param>
/// <param name="Value">The rotation value at this keyframe.</param>
/// <param name="InTangent">The incoming tangent quaternion.</param>
/// <param name="OutTangent">The outgoing tangent quaternion.</param>
public readonly record struct CubicSplineQuaternionKeyframe(
    float Time,
    Quaternion Value,
    Quaternion InTangent,
    Quaternion OutTangent);
