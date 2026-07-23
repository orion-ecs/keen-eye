using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Utilities;

/// <summary>
/// An equatable snapshot of a <see cref="Location"/> for use in incremental
/// generator pipeline models.
/// </summary>
/// <remarks>
/// <see cref="Location"/> holds a reference to its <see cref="SyntaxTree"/>, so two
/// locations from different parses of the same file never compare equal, which
/// defeats incremental caching. This record captures only the file path and spans
/// and can recreate an equivalent <see cref="Location"/> for diagnostic reporting.
/// </remarks>
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    /// <summary>Recreates a <see cref="Location"/> suitable for diagnostic reporting.</summary>
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    /// <summary>
    /// Creates a <see cref="LocationInfo"/> from a <see cref="Location"/>, or null when
    /// the location has no source tree.
    /// </summary>
    /// <param name="location">The location to capture.</param>
    public static LocationInfo? From(Location? location)
    {
        if (location?.SourceTree is null)
        {
            return null;
        }

        return new LocationInfo(
            location.SourceTree.FilePath,
            location.SourceSpan,
            location.GetLineSpan().Span);
    }
}
