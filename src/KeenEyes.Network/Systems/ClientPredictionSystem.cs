using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Systems;

/// <summary>
/// System that handles client-side prediction, misprediction detection, and reconciliation.
/// </summary>
/// <remarks>
/// <para>
/// This system runs on the client and manages the prediction lifecycle:
/// 1. Saves predicted states after local simulation
/// 2. Compares server-confirmed states against predictions
/// 3. Triggers reconciliation (rollback + replay) on misprediction
/// </para>
/// </remarks>
/// <param name="plugin">The network client plugin.</param>
/// <param name="interpolator">Optional interpolator for smooth corrections.</param>
/// <param name="applyInput">Function to apply an input during replay.</param>
public sealed class ClientPredictionSystem(
    NetworkClientPlugin plugin,
    INetworkInterpolator? interpolator = null,
    Action<Entity, object>? applyInput = null) : SystemBase
{
    private readonly Dictionary<Entity, PredictionBuffer> predictionBuffers = [];
    private readonly INetworkInterpolator? smoothingInterpolator = interpolator;

    /// <summary>
    /// Gets the prediction buffer for an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The prediction buffer, or null if not found.</returns>
    public PredictionBuffer? GetPredictionBuffer(Entity entity)
    {
        return predictionBuffers.TryGetValue(entity, out var buffer) ? buffer : null;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (!plugin.Config.EnablePrediction)
        {
            return;
        }

        // Save predicted states for locally owned entities
        foreach (var entity in World.Query<LocallyOwned, Predicted, PredictionState>())
        {
            SavePredictedState(entity);
        }
    }

    /// <summary>
    /// Called when server state is received to check for misprediction.
    /// </summary>
    /// <param name="entity">The entity that received server state.</param>
    /// <param name="serverTick">The tick the server state is for.</param>
    /// <param name="serverStates">The server component states.</param>
    public void OnServerStateReceived(
        Entity entity,
        uint serverTick,
        IReadOnlyDictionary<Type, object> serverStates)
    {
        if (!World.Has<PredictionState>(entity))
        {
            return;
        }

        ref var predState = ref World.Get<PredictionState>(entity);

        // Check if we have a prediction for this tick
        if (!predictionBuffers.TryGetValue(entity, out var buffer))
        {
            return;
        }

        var predictedStates = buffer.GetStatesForTick(serverTick);
        if (predictedStates is null)
        {
            // No prediction for this tick, just apply server state
            ApplyServerState(entity, serverStates);
            predState.LastConfirmedTick = serverTick;
            return;
        }

        // Compare predicted vs server state
        var mispredicted = DetectMisprediction(predictedStates, serverStates);

        if (mispredicted)
        {
            predState.MispredictionDetected = true;
            Reconcile(entity, serverTick, serverStates);
        }
        else
        {
            predState.MispredictionDetected = false;
        }

        // Update confirmed tick and clean up old predictions
        predState.LastConfirmedTick = serverTick;
        buffer.RemoveOlderThan(serverTick);
    }

    private void SavePredictedState(Entity entity)
    {
        if (!predictionBuffers.TryGetValue(entity, out var buffer))
        {
            buffer = new PredictionBuffer(plugin.Config.InputBufferSize);
            predictionBuffers[entity] = buffer;
        }

        ref var predState = ref World.Get<PredictionState>(entity);
        var tick = plugin.CurrentTick;

        // Save all replicated component states
        var serializer = plugin.Config.Serializer;
        if (serializer is null)
        {
            return;
        }

        foreach (var (type, value) in World.GetComponents(entity))
        {
            if (serializer.IsNetworkSerializable(type))
            {
                // Clone the value to avoid storing a reference
                buffer.SaveState(tick, type, CloneValue(type, value));
            }
        }

        predState.LastPredictedTick = tick;
    }

    private bool DetectMisprediction(
        IReadOnlyDictionary<Type, object> predicted,
        IReadOnlyDictionary<Type, object> server)
    {
        var threshold = plugin.Config.MispredictionThreshold;

        foreach (var (type, serverValue) in server)
        {
            if (!predicted.TryGetValue(type, out var predictedValue))
            {
                // We don't have a prediction for this component, misprediction
                return true;
            }

            if (!ValuesApproximatelyEqual(predictedValue, serverValue, threshold))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ValuesApproximatelyEqual(object a, object b, float threshold)
    {
        if (a.Equals(b))
        {
            return true;
        }

        // For floating point comparisons, use threshold
        if (threshold > 0)
        {
            // Check if both are float structs and compare fields
            // This is a simplified comparison - a full implementation would
            // use reflection or generated comparers
            if (a.GetType() == b.GetType())
            {
                // For now, return false if not exactly equal
                // A proper implementation would compare field-by-field with threshold
                return false;
            }
        }

        return false;
    }

    private void Reconcile(
        Entity entity,
        uint serverTick,
        IReadOnlyDictionary<Type, object> serverStates)
    {
        // Step 1: Apply server state (rollback)
        ApplyServerState(entity, serverStates);

        // Step 2: Replay inputs from serverTick to current tick
        if (applyInput is not null)
        {
            var inputBuffer = plugin.GetInputBuffer(entity) as IInputBuffer;
            if (inputBuffer is not null)
            {
                foreach (var input in inputBuffer.GetInputsFromBoxed(serverTick + 1))
                {
                    applyInput(entity, input);
                }
            }
        }

        // Calculate correction magnitude for smoothing
        ref var predState = ref World.Get<PredictionState>(entity);

        // If we have an interpolator, we could use it for smooth corrections
        // For now, just set to a fixed value - a proper implementation would
        // calculate the actual distance between old and new positions
        predState.LastCorrectionMagnitude = 1.0f;

        // Store interpolator availability for potential future smoothing
        predState.SmoothingAvailable = smoothingInterpolator is not null;
    }

    private void ApplyServerState(Entity entity, IReadOnlyDictionary<Type, object> serverStates)
    {
        foreach (var (type, value) in serverStates)
        {
            World.SetComponent(entity, type, value);
        }
    }

    private static object CloneValue(Type type, object value)
    {
        // For value types (structs), boxing already creates a copy
        if (type.IsValueType)
        {
            return value;
        }

        // For reference types, we'd need deep cloning
        // Network components should be value types (structs)
        return value;
    }

    /// <summary>
    /// Registers an entity for prediction tracking.
    /// </summary>
    /// <param name="entity">The entity to track.</param>
    public void RegisterEntity(Entity entity)
    {
        if (!predictionBuffers.ContainsKey(entity))
        {
            predictionBuffers[entity] = new PredictionBuffer(plugin.Config.InputBufferSize);
        }
    }

    /// <summary>
    /// Unregisters an entity from prediction tracking.
    /// </summary>
    /// <param name="entity">The entity to unregister.</param>
    public void UnregisterEntity(Entity entity)
    {
        predictionBuffers.Remove(entity);
    }
}
