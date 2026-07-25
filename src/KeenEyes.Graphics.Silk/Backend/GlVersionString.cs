using System.Globalization;

namespace KeenEyes.Graphics.Silk.Backend;

/// <summary>
/// Parses the version number out of an OpenGL <c>GL_VERSION</c> string.
/// </summary>
/// <remarks>
/// <para>
/// <c>GL_MAJOR_VERSION</c> and <c>GL_MINOR_VERSION</c> only exist on OpenGL 3.0 and newer, so the
/// version string is the only source available on exactly the old drivers a capability check
/// needs to detect. The spec guarantees it begins with <c>major.minor</c>, optionally prefixed
/// (<c>OpenGL ES 3.2 ...</c>) and always allowed a vendor suffix (<c>4.6.0 NVIDIA 551.86</c>).
/// </para>
/// </remarks>
internal static class GlVersionString
{
    /// <summary>
    /// Extracts the major and minor version from a <c>GL_VERSION</c> string.
    /// </summary>
    /// <param name="versionString">The driver's version string, which may be null or malformed.</param>
    /// <returns>
    /// The first <c>major.minor</c> pair in the string, or <c>(0, 0)</c> when the string contains
    /// none - a value no real driver reports, so it fails every minimum-version check.
    /// </returns>
    internal static (int Major, int Minor) Parse(string? versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            return (0, 0);
        }

        var text = versionString.AsSpan();

        int index = 0;
        while (index < text.Length)
        {
            if (!char.IsAsciiDigit(text[index]))
            {
                index++;
                continue;
            }

            int majorEnd = index;
            while (majorEnd < text.Length && char.IsAsciiDigit(text[majorEnd]))
            {
                majorEnd++;
            }

            // A digit run not followed by ".<digit>" cannot be a version (for example the "2" in
            // "WebGL2 ..."). Skip past it and keep looking; the first real pair wins, which is
            // the leading one the spec mandates.
            if (majorEnd >= text.Length || text[majorEnd] != '.')
            {
                index = majorEnd;
                continue;
            }

            int minorStart = majorEnd + 1;
            int minorEnd = minorStart;
            while (minorEnd < text.Length && char.IsAsciiDigit(text[minorEnd]))
            {
                minorEnd++;
            }

            if (minorEnd == minorStart)
            {
                index = minorEnd;
                continue;
            }

            return int.TryParse(text[index..majorEnd], CultureInfo.InvariantCulture, out int major)
                   && int.TryParse(text[minorStart..minorEnd], CultureInfo.InvariantCulture, out int minor)
                ? (major, minor)
                : (0, 0);
        }

        return (0, 0);
    }
}
