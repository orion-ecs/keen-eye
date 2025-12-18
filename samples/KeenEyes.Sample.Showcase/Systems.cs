using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.Showcase;

/// <summary>
/// System that handles bouncing ball physics including gravity,
/// floor/wall collisions, and ball-to-ball collisions.
/// </summary>
public class BallPhysicsSystem : SystemBase
{
    private const float Gravity = 15f;
    private const float FloorY = 0f;
    private const float ArenaHalfSize = 9f; // Arena is 18x18 units
    private const float CeilingY = 20f;

    /// <summary>
    /// Updates all ball positions and handles collisions.
    /// </summary>
    public override void Update(float deltaTime)
    {
        // Collect all balls for ball-to-ball collision
        var balls = new List<(Entity entity, Vector3 position, float radius)>();

        foreach (var entity in World.Query<Transform3D, BallPhysics, BallTag>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var physics = ref World.Get<BallPhysics>(entity);

            // Apply gravity
            physics.Velocity.Y -= Gravity * deltaTime;

            // Update position
            transform.Position += physics.Velocity * deltaTime;

            // Floor collision
            if (transform.Position.Y - physics.Radius < FloorY)
            {
                transform.Position = new Vector3(
                    transform.Position.X,
                    FloorY + physics.Radius,
                    transform.Position.Z);
                physics.Velocity.Y = -physics.Velocity.Y * physics.Bounciness;

                // Add some energy loss to horizontal movement on bounce
                physics.Velocity.X *= 0.98f;
                physics.Velocity.Z *= 0.98f;
            }

            // Ceiling collision
            if (transform.Position.Y + physics.Radius > CeilingY)
            {
                transform.Position = new Vector3(
                    transform.Position.X,
                    CeilingY - physics.Radius,
                    transform.Position.Z);
                physics.Velocity.Y = -physics.Velocity.Y * physics.Bounciness;
            }

            // Wall collisions (X axis)
            if (transform.Position.X - physics.Radius < -ArenaHalfSize)
            {
                transform.Position = new Vector3(
                    -ArenaHalfSize + physics.Radius,
                    transform.Position.Y,
                    transform.Position.Z);
                physics.Velocity.X = -physics.Velocity.X * physics.Bounciness;
            }
            else if (transform.Position.X + physics.Radius > ArenaHalfSize)
            {
                transform.Position = new Vector3(
                    ArenaHalfSize - physics.Radius,
                    transform.Position.Y,
                    transform.Position.Z);
                physics.Velocity.X = -physics.Velocity.X * physics.Bounciness;
            }

            // Wall collisions (Z axis)
            if (transform.Position.Z - physics.Radius < -ArenaHalfSize)
            {
                transform.Position = new Vector3(
                    transform.Position.X,
                    transform.Position.Y,
                    -ArenaHalfSize + physics.Radius);
                physics.Velocity.Z = -physics.Velocity.Z * physics.Bounciness;
            }
            else if (transform.Position.Z + physics.Radius > ArenaHalfSize)
            {
                transform.Position = new Vector3(
                    transform.Position.X,
                    transform.Position.Y,
                    ArenaHalfSize - physics.Radius);
                physics.Velocity.Z = -physics.Velocity.Z * physics.Bounciness;
            }

            balls.Add((entity, transform.Position, physics.Radius));
        }

        // Ball-to-ball collision detection and response
        for (int i = 0; i < balls.Count; i++)
        {
            for (int j = i + 1; j < balls.Count; j++)
            {
                var (entityA, posA, radiusA) = balls[i];
                var (entityB, posB, radiusB) = balls[j];

                var diff = posB - posA;
                var dist = diff.Length();
                var minDist = radiusA + radiusB;

                if (dist < minDist && dist > 0.001f)
                {
                    // Balls are overlapping
                    var normal = diff / dist;

                    // Separate balls
                    var overlap = minDist - dist;
                    var separation = normal * (overlap / 2f);

                    ref var transformA = ref World.Get<Transform3D>(entityA);
                    ref var transformB = ref World.Get<Transform3D>(entityB);
                    ref var physicsA = ref World.Get<BallPhysics>(entityA);
                    ref var physicsB = ref World.Get<BallPhysics>(entityB);

                    transformA.Position -= separation;
                    transformB.Position += separation;

                    // Exchange velocity components along collision normal (elastic collision)
                    var relVel = physicsB.Velocity - physicsA.Velocity;
                    var velAlongNormal = Vector3.Dot(relVel, normal);

                    // Only resolve if balls are approaching
                    if (velAlongNormal < 0)
                    {
                        var restitution = (physicsA.Bounciness + physicsB.Bounciness) / 2f;
                        var impulse = -(1f + restitution) * velAlongNormal / 2f;
                        var impulseVec = impulse * normal;

                        physicsA.Velocity -= impulseVec;
                        physicsB.Velocity += impulseVec;
                    }
                }
            }
        }
    }
}

/// <summary>
/// System that handles first-person camera controls with WASD movement
/// and mouse look.
/// </summary>
public class CameraControlSystem : SystemBase
{
    private IInputContext? input;
    private Vector2 accumulatedMouseDelta;
    private bool eventSubscribed;
    private float logTimer;
    private const float LogInterval = 0.5f; // Log every 0.5 seconds
    private bool lastCapturedState;

    /// <summary>
    /// Called when the system initializes.
    /// </summary>
    protected override void OnInitialize()
    {
        Console.WriteLine("[CameraControl] System initialized");
    }

    /// <summary>
    /// Updates camera position and rotation based on input.
    /// </summary>
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        // Subscribe to mouse move events once input is available
        if (!eventSubscribed)
        {
            input.Mouse.OnMove += OnMouseMove;
            eventSubscribed = true;
            Console.WriteLine("[CameraControl] Subscribed to mouse move events");
        }

        var keyboard = input.Keyboard;
        var mouse = input.Mouse;

        // Log capture state changes
        if (mouse.IsCursorCaptured != lastCapturedState)
        {
            lastCapturedState = mouse.IsCursorCaptured;
            Console.WriteLine($"[CameraControl] Mouse captured: {lastCapturedState}");
        }

        foreach (var entity in World.Query<Transform3D, Camera, CameraController>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var controller = ref World.Get<CameraController>(entity);

            // Mouse look (only when cursor is captured)
            if (mouse.IsCursorCaptured && (!accumulatedMouseDelta.X.IsApproximatelyZero() || !accumulatedMouseDelta.Y.IsApproximatelyZero()))
            {
                // Apply accumulated mouse delta
                controller.Yaw -= accumulatedMouseDelta.X * controller.Sensitivity;
                controller.Pitch -= accumulatedMouseDelta.Y * controller.Sensitivity;

                // Clamp pitch to avoid gimbal lock
                controller.Pitch = Math.Clamp(controller.Pitch, -MathF.PI / 2f + 0.1f, MathF.PI / 2f - 0.1f);
            }

            // Calculate forward and right vectors from yaw
            var cosYaw = MathF.Cos(controller.Yaw);
            var sinYaw = MathF.Sin(controller.Yaw);

            var forward = new Vector3(sinYaw, 0, -cosYaw);
            var right = new Vector3(cosYaw, 0, sinYaw);

            // WASD movement
            var moveDir = Vector3.Zero;

            if (keyboard.IsKeyDown(Key.W) || keyboard.IsKeyDown(Key.Up))
            {
                moveDir += forward;
            }

            if (keyboard.IsKeyDown(Key.S) || keyboard.IsKeyDown(Key.Down))
            {
                moveDir -= forward;
            }

            if (keyboard.IsKeyDown(Key.A) || keyboard.IsKeyDown(Key.Left))
            {
                moveDir -= right;
            }

            if (keyboard.IsKeyDown(Key.D) || keyboard.IsKeyDown(Key.Right))
            {
                moveDir += right;
            }

            // Vertical movement with Q/E or Space/Ctrl
            if (keyboard.IsKeyDown(Key.E) || keyboard.IsKeyDown(Key.Space))
            {
                moveDir.Y += 1;
            }

            if (keyboard.IsKeyDown(Key.Q) || keyboard.IsKeyDown(Key.LeftControl))
            {
                moveDir.Y -= 1;
            }

            // Normalize and apply movement
            if (moveDir.LengthSquared() > 0.01f)
            {
                moveDir = Vector3.Normalize(moveDir);
                transform.Position += moveDir * controller.MoveSpeed * deltaTime;
            }

            // Update rotation quaternion from yaw and pitch
            transform.Rotation = Quaternion.CreateFromYawPitchRoll(controller.Yaw, controller.Pitch, 0);

            // Periodic diagnostic logging
            logTimer += deltaTime;
            if (logTimer >= LogInterval)
            {
                logTimer = 0;
                Console.WriteLine($"[Camera] Pos: ({transform.Position.X:F1}, {transform.Position.Y:F1}, {transform.Position.Z:F1}) " +
                                  $"Yaw: {controller.Yaw:F2} Pitch: {controller.Pitch:F2} " +
                                  $"MouseDelta: ({accumulatedMouseDelta.X:F1}, {accumulatedMouseDelta.Y:F1}) " +
                                  $"Captured: {mouse.IsCursorCaptured}");
            }
        }

        // Reset accumulated delta after applying
        accumulatedMouseDelta = Vector2.Zero;
    }

    private void OnMouseMove(MouseMoveEventArgs args)
    {
        // Accumulate delta from events (delta is provided by the event)
        accumulatedMouseDelta += args.Delta;

        // Log significant mouse movements
        if (args.Delta.Length() > 5f)
        {
            Console.WriteLine($"[MouseMove] Delta: ({args.Delta.X:F1}, {args.Delta.Y:F1}) Pos: ({args.Position.X:F0}, {args.Position.Y:F0})");
        }
    }
}

/// <summary>
/// System that rotates entities with the Spin component.
/// </summary>
public class SpinSystem : SystemBase
{
    /// <summary>
    /// Updates rotation of spinning entities.
    /// </summary>
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Spin>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var spin = ref World.Get<Spin>(entity);

            var rotation = Quaternion.CreateFromYawPitchRoll(
                spin.Speed.Y * deltaTime,
                spin.Speed.X * deltaTime,
                spin.Speed.Z * deltaTime);

            transform.Rotation = Quaternion.Normalize(transform.Rotation * rotation);
        }
    }
}
