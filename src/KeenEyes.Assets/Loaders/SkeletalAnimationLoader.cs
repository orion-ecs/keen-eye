using System.Numerics;

using KeenEyes.Animation.Data;

using SharpGLTF.Schema2;

using GltfAnimation = SharpGLTF.Schema2.Animation;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for skeletal animations from glTF/GLB files.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SkeletalAnimationLoader"/> extracts bone animations from glTF files for use with
/// skeletal mesh rendering. Unlike <see cref="AnimationLoader"/> (for 2D sprite animations),
/// this loader handles 3D skeletal animations with position, rotation, and scale curves.
/// </para>
/// <para>
/// A single glTF file may contain multiple animation clips. The loader extracts all
/// animations and packages them into a <see cref="SkeletalAnimationAsset"/> for sharing
/// across multiple compatible skeletons.
/// </para>
/// <para>
/// glTF animation interpolation modes:
/// </para>
/// <list type="bullet">
/// <item><description>LINEAR - Linear interpolation (default)</description></item>
/// <item><description>STEP - Constant value until next keyframe</description></item>
/// <item><description>CUBICSPLINE - Cubic spline with tangents</description></item>
/// </list>
/// </remarks>
public sealed class SkeletalAnimationLoader : IAssetLoader<SkeletalAnimationAsset>
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".gltf", ".glb"];

    /// <inheritdoc />
    public SkeletalAnimationAsset Load(Stream stream, AssetLoadContext context)
    {
        // Read the entire stream into memory (SharpGLTF needs random access)
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        // Load the glTF model
        var model = ModelRoot.ReadGLB(memoryStream);

        // Extract all animations
        var clips = ExtractAnimations(model, out var targetBoneNames);

        if (clips.Count == 0)
        {
            throw new AssetLoadException(
                context.Path,
                typeof(SkeletalAnimationAsset),
                "No animations found in glTF file");
        }

        // Get asset name from context path
        var name = Path.GetFileNameWithoutExtension(context.Path) ?? "SkeletalAnimation";

        return new SkeletalAnimationAsset(name, clips, targetBoneNames);
    }

    /// <inheritdoc />
    public async Task<SkeletalAnimationAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // glTF parsing is CPU-bound, so run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(SkeletalAnimationAsset asset) => asset.SizeBytes;

    /// <summary>
    /// Extracts all animations from the glTF model.
    /// </summary>
    private static List<AnimationClip> ExtractAnimations(ModelRoot model, out List<string> targetBoneNames)
    {
        var clips = new List<AnimationClip>();
        var boneNameSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var animation in model.LogicalAnimations)
        {
            var clip = ExtractAnimation(animation, boneNameSet);
            clips.Add(clip);
        }

        targetBoneNames = [.. boneNameSet];
        return clips;
    }

    /// <summary>
    /// Extracts a single animation clip from a glTF animation.
    /// </summary>
    private static AnimationClip ExtractAnimation(GltfAnimation animation, HashSet<string> boneNameSet)
    {
        var clip = new AnimationClip
        {
            Name = animation.Name ?? $"Animation_{animation.LogicalIndex}"
        };

        // Group channels by target node (bone)
        var channelsByNode = new Dictionary<Node, List<AnimationChannel>>();

        foreach (var channel in animation.Channels)
        {
            var targetNode = channel.TargetNode;
            if (targetNode is null)
            {
                continue;
            }

            if (!channelsByNode.TryGetValue(targetNode, out var channels))
            {
                channels = [];
                channelsByNode[targetNode] = channels;
            }

            channels.Add(channel);
        }

        // Create bone tracks for each animated node
        foreach (var (node, channels) in channelsByNode)
        {
            var boneName = node.Name ?? $"Node_{node.LogicalIndex}";
            boneNameSet.Add(boneName);

            var track = CreateBoneTrack(boneName, channels);
            clip.AddBoneTrack(track);
        }

        return clip;
    }

    /// <summary>
    /// Creates a bone track from glTF animation channels.
    /// </summary>
    private static BoneTrack CreateBoneTrack(string boneName, List<AnimationChannel> channels)
    {
        Vector3Curve? positionCurve = null;
        QuaternionCurve? rotationCurve = null;
        Vector3Curve? scaleCurve = null;

        foreach (var channel in channels)
        {
            var path = channel.TargetNodePath;

            switch (path)
            {
                case PropertyPath.translation:
                    var translationSampler = channel.GetTranslationSampler();
                    if (translationSampler is not null)
                    {
                        positionCurve = ExtractVector3Curve(translationSampler);
                    }

                    break;

                case PropertyPath.rotation:
                    var rotationSampler = channel.GetRotationSampler();
                    if (rotationSampler is not null)
                    {
                        rotationCurve = ExtractQuaternionCurve(rotationSampler);
                    }

                    break;

                case PropertyPath.scale:
                    var scaleSampler = channel.GetScaleSampler();
                    if (scaleSampler is not null)
                    {
                        scaleCurve = ExtractVector3Curve(scaleSampler);
                    }

                    break;

                case PropertyPath.weights:
                    // Morph target weights - not supported in this implementation
                    break;
            }
        }

        return new BoneTrack
        {
            BoneName = boneName,
            PositionCurve = positionCurve,
            RotationCurve = rotationCurve,
            ScaleCurve = scaleCurve
        };
    }

    /// <summary>
    /// Extracts a Vector3 curve from a glTF animation sampler.
    /// </summary>
    private static Vector3Curve ExtractVector3Curve(IAnimationSampler<Vector3> sampler)
    {
        var curve = new Vector3Curve();

        // Try to get cubic keys first (for CUBICSPLINE interpolation)
        var cubicKeys = sampler.GetCubicKeys().ToArray();
        if (cubicKeys.Length > 0)
        {
            // CubicKeys returns (time, (TangentIn, Value, TangentOut)) tuples
            curve.Interpolation = InterpolationType.CubicSpline;
            foreach (var (time, tangentData) in cubicKeys)
            {
                var (inTangent, value, outTangent) = tangentData;
                curve.AddCubicKeyframe(time, value, inTangent, outTangent);
            }

            return curve;
        }

        // Fall back to linear/step keys
        var linearKeys = sampler.GetLinearKeys().ToArray();
        if (linearKeys.Length > 0)
        {
            curve.Interpolation = InterpolationType.Linear;
            foreach (var (time, value) in linearKeys)
            {
                curve.AddKeyframe(time, value);
            }
        }

        return curve;
    }

    /// <summary>
    /// Extracts a Quaternion curve from a glTF animation sampler.
    /// </summary>
    private static QuaternionCurve ExtractQuaternionCurve(IAnimationSampler<Quaternion> sampler)
    {
        var curve = new QuaternionCurve();

        // Try to get cubic keys first (for CUBICSPLINE interpolation)
        var cubicKeys = sampler.GetCubicKeys().ToArray();
        if (cubicKeys.Length > 0)
        {
            // CubicKeys returns (time, (TangentIn, Value, TangentOut)) tuples
            curve.Interpolation = InterpolationType.CubicSpline;
            foreach (var (time, tangentData) in cubicKeys)
            {
                var (inTangent, value, outTangent) = tangentData;
                curve.AddCubicKeyframe(time, value, inTangent, outTangent);
            }

            return curve;
        }

        // Fall back to linear/step keys
        var linearKeys = sampler.GetLinearKeys().ToArray();
        if (linearKeys.Length > 0)
        {
            // Use LINEAR interpolation (slerp for quaternions)
            curve.Interpolation = InterpolationType.Linear;
            foreach (var (time, value) in linearKeys)
            {
                curve.AddKeyframe(time, value);
            }
        }

        return curve;
    }
}
