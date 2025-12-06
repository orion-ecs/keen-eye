namespace KeenEyes;

/// <summary>
/// Non-generic interface for component storage within an archetype.
/// Enables heterogeneous collection of typed component arrays.
/// </summary>
public interface IComponentArray
{
    /// <summary>
    /// Gets the component type stored in this array.
    /// </summary>
    Type ComponentType { get; }

    /// <summary>
    /// Gets the number of components stored.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the capacity of the array.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Removes the component at the specified index by swapping with the last element.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    void RemoveAtSwapBack(int index);

    /// <summary>
    /// Copies a component from this array to another array.
    /// Used during entity migration between archetypes.
    /// </summary>
    /// <param name="sourceIndex">The index in this array.</param>
    /// <param name="destination">The destination array.</param>
    void CopyTo(int sourceIndex, IComponentArray destination);

    /// <summary>
    /// Clears all components from this array.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the boxed component value at the specified index.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The boxed component value.</returns>
    /// <remarks>
    /// This method boxes the component and should only be used for debugging
    /// or serialization scenarios. For performance-critical code, use the
    /// typed <see cref="ComponentArray{T}.GetRef"/> method.
    /// </remarks>
    object GetBoxed(int index);

    /// <summary>
    /// Sets the component value at the specified index from a boxed value.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <param name="value">The boxed component value.</param>
    void SetBoxed(int index, object value);

    /// <summary>
    /// Adds a component from a boxed value.
    /// </summary>
    /// <param name="value">The boxed component value to add.</param>
    /// <returns>The index where the component was added.</returns>
    int AddBoxed(object value);
}
