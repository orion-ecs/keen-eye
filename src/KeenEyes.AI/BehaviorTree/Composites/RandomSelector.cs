namespace KeenEyes.AI.BehaviorTree.Composites;

/// <summary>
/// Composite node that executes children in random order until one succeeds.
/// </summary>
/// <remarks>
/// <para>
/// RandomSelector shuffles children randomly each time it starts execution,
/// then behaves like a regular <see cref="Selector"/>.
/// </para>
/// <para>
/// Use for varied AI behavior: "Pick a random valid action."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Randomly pick between different attack patterns
/// var randomSelector = new RandomSelector
/// {
///     Children = [
///         new ActionNode { Action = new MeleeAttackAction() },
///         new ActionNode { Action = new RangedAttackAction() },
///         new ActionNode { Action = new SpecialAttackAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class RandomSelector : CompositeNode
{
    private readonly List<int> shuffledIndices = [];
    private bool needsShuffle = true;

    /// <summary>
    /// Gets or sets the random seed. If null, uses system random.
    /// </summary>
    public int? Seed { get; set; }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        needsShuffle = true;
        shuffledIndices.Clear();
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Children.Count == 0)
        {
            return SetState(BTNodeState.Failure);
        }

        // Shuffle on first execution
        if (needsShuffle)
        {
            ShuffleIndices();
            needsShuffle = false;
        }

        while (currentChildIndex < shuffledIndices.Count)
        {
            var childIndex = shuffledIndices[currentChildIndex];
            var child = Children[childIndex];
            var state = child.Execute(entity, blackboard, world);

            switch (state)
            {
                case BTNodeState.Success:
                    // Found a successful child - reset for next run
                    currentChildIndex = 0;
                    needsShuffle = true;
                    return SetState(BTNodeState.Success);

                case BTNodeState.Running:
                    // Child still running - wait for completion
                    return SetState(BTNodeState.Running);

                case BTNodeState.Failure:
                    // Child failed - try next child
                    currentChildIndex++;
                    break;
            }
        }

        // All children failed - reset for next run
        currentChildIndex = 0;
        needsShuffle = true;
        return SetState(BTNodeState.Failure);
    }

    private void ShuffleIndices()
    {
        shuffledIndices.Clear();

        for (var i = 0; i < Children.Count; i++)
        {
            shuffledIndices.Add(i);
        }

        // Fisher-Yates shuffle
        var random = Seed.HasValue ? new Random(Seed.Value) : Random.Shared;

        for (var i = shuffledIndices.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffledIndices[i], shuffledIndices[j]) = (shuffledIndices[j], shuffledIndices[i]);
        }
    }
}
