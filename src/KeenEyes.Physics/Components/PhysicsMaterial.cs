namespace KeenEyes.Physics.Components;

/// <summary>
/// Component that defines the physical material properties for collision response.
/// </summary>
/// <remarks>
/// <para>
/// This component is optional. If not present, default material properties will be used.
/// Material properties affect how objects interact during collisions.
/// </para>
/// </remarks>
/// <remarks>
/// Creates a physics material with the specified properties.
/// </remarks>
/// <param name="friction">Friction coefficient (0-1).</param>
/// <param name="restitution">Restitution/bounciness coefficient (0-1).</param>
/// <param name="linearDamping">Linear damping coefficient.</param>
/// <param name="angularDamping">Angular damping coefficient.</param>
public struct PhysicsMaterial(float friction = 0.5f, float restitution = 0.3f, float linearDamping = 0.01f, float angularDamping = 0.01f) : IComponent
{
    /// <summary>
    /// Friction coefficient (0 = frictionless, 1 = high friction).
    /// </summary>
    /// <remarks>
    /// The effective friction between two colliding objects is typically computed
    /// as the geometric mean of their individual friction coefficients.
    /// </remarks>
    public float Friction = friction;

    /// <summary>
    /// Restitution coefficient (0 = no bounce, 1 = perfect bounce).
    /// </summary>
    /// <remarks>
    /// Also known as bounciness. A value of 0 means all kinetic energy is absorbed
    /// on impact, while 1 means no energy is lost (perfect elastic collision).
    /// </remarks>
    public float Restitution = restitution;

    /// <summary>
    /// Linear damping coefficient that slows down linear velocity over time.
    /// </summary>
    /// <remarks>
    /// Acts like air resistance. A value of 0 means no damping.
    /// Higher values cause the body to slow down faster.
    /// </remarks>
    public float LinearDamping = linearDamping;

    /// <summary>
    /// Angular damping coefficient that slows down angular velocity over time.
    /// </summary>
    /// <remarks>
    /// Acts like rotational air resistance. A value of 0 means no damping.
    /// Higher values cause the body to stop rotating faster.
    /// </remarks>
    public float AngularDamping = angularDamping;

    /// <summary>
    /// Default material with moderate friction and low bounce.
    /// </summary>
    public static PhysicsMaterial Default => new();

    /// <summary>
    /// Rubber-like material with high friction and high bounce.
    /// </summary>
    public static PhysicsMaterial Rubber => new(friction: 0.8f, restitution: 0.8f);

    /// <summary>
    /// Ice-like material with very low friction and no bounce.
    /// </summary>
    public static PhysicsMaterial Ice => new(friction: 0.05f, restitution: 0.1f);

    /// <summary>
    /// Metal-like material with moderate friction and moderate bounce.
    /// </summary>
    public static PhysicsMaterial Metal => new(friction: 0.4f, restitution: 0.5f);

    /// <summary>
    /// Wood-like material with moderate friction and low bounce.
    /// </summary>
    public static PhysicsMaterial Wood => new(friction: 0.6f, restitution: 0.2f);
}
