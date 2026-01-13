using KeenEyes.Animation.Data;

namespace KeenEyes.Assets;

/// <summary>
/// A skeletal animation asset containing bone animation clips.
/// </summary>
/// <remarks>
/// <para>
/// SkeletalAnimationAsset is designed to be shared across multiple characters/models.
/// It contains one or more animation clips that target bones by name, allowing the
/// same animation to be applied to any skeleton with matching bone names.
/// </para>
/// <para>
/// This is distinct from <see cref="AnimationAsset"/> which is for sprite-based 2D
/// animations. SkeletalAnimationAsset is used for 3D bone-driven animations loaded
/// from formats like glTF.
/// </para>
/// <para>
/// Animations are separated from skeletons to enable:
/// </para>
/// <list type="bullet">
/// <item><description>Animation sharing between models with compatible skeletons</description></item>
/// <item><description>Loading animations separately from models</description></item>
/// <item><description>Animation library management</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Load a character model (with skeleton)
/// var model = assetManager.Load&lt;ModelAsset&gt;("character.glb");
///
/// // Load animations separately (can be reused across characters)
/// var walkAnim = assetManager.Load&lt;SkeletalAnimationAsset&gt;("animations/walk.glb");
/// var runAnim = assetManager.Load&lt;SkeletalAnimationAsset&gt;("animations/run.glb");
///
/// // Check compatibility before use
/// if (walkAnim.IsCompatibleWith(model.Skeleton))
/// {
///     // Apply animation to character
/// }
/// </code>
/// </example>
public sealed class SkeletalAnimationAsset : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the name of this animation asset.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the animation clips contained in this asset.
    /// </summary>
    /// <remarks>
    /// A single glTF file may contain multiple animation clips (e.g., "idle", "walk", "run").
    /// Each clip can be played independently or blended together.
    /// </remarks>
    public IReadOnlyList<AnimationClip> Clips { get; }

    /// <summary>
    /// Gets the names of all bones targeted by animations in this asset.
    /// </summary>
    /// <remarks>
    /// This list is used for skeleton compatibility checking. An animation is compatible
    /// with a skeleton if the skeleton contains all bones in this list.
    /// </remarks>
    public IReadOnlyList<string> TargetBoneNames { get; }

    /// <summary>
    /// Gets the total duration of the longest clip in seconds.
    /// </summary>
    public float TotalDuration { get; }

    /// <summary>
    /// Gets the number of animation clips in this asset.
    /// </summary>
    public int ClipCount => Clips.Count;

    /// <summary>
    /// Gets the estimated size of this asset in bytes.
    /// </summary>
    public long SizeBytes
    {
        get
        {
            // Estimate: 64 bytes base + clips + bone names
            long size = 64;

            foreach (var clip in Clips)
            {
                // Each clip: name + duration + tracks
                size += 32 + (clip.BoneTracks.Count * 512);
            }

            foreach (var name in TargetBoneNames)
            {
                size += 16 + (name.Length * 2);
            }

            return size;
        }
    }

    /// <summary>
    /// Creates a new skeletal animation asset.
    /// </summary>
    /// <param name="name">The asset name.</param>
    /// <param name="clips">The animation clips.</param>
    /// <param name="targetBoneNames">The names of bones targeted by these animations.</param>
    /// <exception cref="ArgumentNullException">Thrown when clips or targetBoneNames is null.</exception>
    /// <exception cref="ArgumentException">Thrown when clips is empty.</exception>
    public SkeletalAnimationAsset(
        string name,
        IReadOnlyList<AnimationClip> clips,
        IReadOnlyList<string> targetBoneNames)
    {
        ArgumentNullException.ThrowIfNull(clips);
        ArgumentNullException.ThrowIfNull(targetBoneNames);

        if (clips.Count == 0)
        {
            throw new ArgumentException("At least one animation clip is required.", nameof(clips));
        }

        Name = name ?? "SkeletalAnimation";
        Clips = clips;
        TargetBoneNames = targetBoneNames;
        TotalDuration = clips.Max(c => c.Duration);
    }

    /// <summary>
    /// Finds an animation clip by name.
    /// </summary>
    /// <param name="clipName">The name of the clip to find.</param>
    /// <returns>The animation clip, or null if not found.</returns>
    public AnimationClip? FindClip(string clipName)
    {
        foreach (var clip in Clips)
        {
            if (string.Equals(clip.Name, clipName, StringComparison.Ordinal))
            {
                return clip;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to get an animation clip by name.
    /// </summary>
    /// <param name="clipName">The name of the clip to find.</param>
    /// <param name="clip">The found clip, or null if not found.</param>
    /// <returns>True if the clip was found.</returns>
    public bool TryGetClip(string clipName, out AnimationClip? clip)
    {
        clip = FindClip(clipName);
        return clip is not null;
    }

    /// <summary>
    /// Gets the animation clip at the specified index.
    /// </summary>
    /// <param name="index">The clip index.</param>
    /// <returns>The animation clip.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public AnimationClip GetClip(int index)
    {
        if (index < 0 || index >= Clips.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index,
                $"Clip index must be between 0 and {Clips.Count - 1}.");
        }

        return Clips[index];
    }

    /// <summary>
    /// Checks if this animation is compatible with a skeleton.
    /// </summary>
    /// <param name="skeleton">The skeleton to check compatibility with.</param>
    /// <returns>True if all target bones exist in the skeleton.</returns>
    /// <remarks>
    /// An animation is compatible if the skeleton contains all bones referenced
    /// by the animation. The skeleton may have additional bones that aren't animated.
    /// </remarks>
    public bool IsCompatibleWith(SkeletonAsset? skeleton)
    {
        if (skeleton is null)
        {
            return false;
        }

        return skeleton.IsCompatibleWith(TargetBoneNames);
    }

    /// <summary>
    /// Gets the names of bones that are missing from a skeleton.
    /// </summary>
    /// <param name="skeleton">The skeleton to check against.</param>
    /// <returns>List of bone names that are in this animation but not in the skeleton.</returns>
    public IReadOnlyList<string> GetMissingBones(SkeletonAsset? skeleton)
    {
        if (skeleton is null)
        {
            return TargetBoneNames;
        }

        var missing = new List<string>();

        foreach (var boneName in TargetBoneNames)
        {
            if (skeleton.FindBone(boneName) < 0)
            {
                missing.Add(boneName);
            }
        }

        return missing;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // Animation clips are managed data; GC will clean them up
    }
}
