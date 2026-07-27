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
    /// <summary>
    /// Gets or sets the random seed. If null, uses system random.
    /// </summary>
    public int? Seed { get; set; }

    /// <inheritdoc/>
    public override void Reset(Blackboard blackboard)
    {
        base.Reset(blackboard);
        var memory = (RandomSelectorMemory)GetMemory(blackboard);
        memory.NeedsShuffle = true;
        memory.ShuffledIndices.Clear();
    }

    /// <inheritdoc/>
    protected internal override BTNodeMemory GetMemory(Blackboard blackboard)
        => blackboard.GetMemory<RandomSelectorMemory>(this);

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Children.Count == 0)
        {
            return SetState(blackboard, BTNodeState.Failure);
        }

        var memory = (RandomSelectorMemory)GetMemory(blackboard);

        // Shuffle on first execution
        if (memory.NeedsShuffle)
        {
            ShuffleIndices(memory.ShuffledIndices);
            memory.NeedsShuffle = false;
        }

        while (memory.CurrentChildIndex < memory.ShuffledIndices.Count)
        {
            var childIndex = memory.ShuffledIndices[memory.CurrentChildIndex];
            var child = Children[childIndex];
            var state = child.Execute(entity, blackboard, world);

            switch (state)
            {
                case BTNodeState.Success:
                    // Found a successful child - reset for next run
                    memory.CurrentChildIndex = 0;
                    memory.NeedsShuffle = true;
                    return SetState(blackboard, BTNodeState.Success);

                case BTNodeState.Running:
                    // Child still running - wait for completion
                    return SetState(blackboard, BTNodeState.Running);

                case BTNodeState.Failure:
                    // Child failed - try next child
                    memory.CurrentChildIndex++;
                    break;
            }
        }

        // All children failed - reset for next run
        memory.CurrentChildIndex = 0;
        memory.NeedsShuffle = true;
        return SetState(blackboard, BTNodeState.Failure);
    }

    private void ShuffleIndices(List<int> shuffledIndices)
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

    /// <summary>
    /// Per-entity execution memory for <see cref="RandomSelector"/>.
    /// </summary>
    internal sealed class RandomSelectorMemory : CompositeMemory
    {
        public List<int> ShuffledIndices { get; } = [];
        public bool NeedsShuffle { get; set; } = true;
    }
}
