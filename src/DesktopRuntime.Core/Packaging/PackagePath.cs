using System.Diagnostics.CodeAnalysis;

namespace DesktopRuntime.Core.Packaging;

/// <summary>
/// Validates a single entry path from an untrusted package archive.
/// <para>
/// This is the highest-risk parsing in the whole product: an archive is attacker-authored
/// data, and a path that escapes the extraction directory writes attacker-chosen bytes to
/// an attacker-chosen location. Every rule here exists because of a specific known attack,
/// and each is named in the tests.
/// </para>
/// </summary>
public static class PackagePath
{
    public const int MaxPathLength = 200;
    public const int MaxSegmentLength = 64;
    public const int MaxDepth = 8;

    /// <summary>
    /// Windows treats these as device names in any directory, with or without an
    /// extension, so an entry called <c>CON</c> or <c>NUL.txt</c> can behave in ways a
    /// regular file never would.
    /// </summary>
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Validates an archive entry path and returns it in canonical form
    /// (forward slashes, lowercase for comparison purposes).
    /// </summary>
    public static bool TryNormalize(
        string? path,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "the entry path is empty.";
            return false;
        }

        if (path.Length > MaxPathLength)
        {
            error = $"the entry path exceeds {MaxPathLength} characters.";
            return false;
        }

        foreach (char c in path)
        {
            if (char.IsControl(c))
            {
                error = "the entry path contains control characters.";
                return false;
            }
        }

        // Archives may use either separator; normalize before inspecting segments so
        // "..\\evil" is caught by the same check as "../evil".
        string candidate = path.Replace('\\', '/');

        if (candidate.StartsWith('/'))
        {
            error = "the entry path must be relative, not rooted.";
            return false;
        }

        // A drive-qualified path ("C:/x") or an alternate data stream ("f.txt:hidden")
        // both show up as a colon; neither is ever legitimate here.
        if (candidate.Contains(':'))
        {
            error = "the entry path must not contain a drive letter or alternate data stream.";
            return false;
        }

        string[] segments = candidate.Split('/', StringSplitOptions.None);

        if (segments.Length > MaxDepth)
        {
            error = $"the entry path is nested deeper than {MaxDepth} levels.";
            return false;
        }

        foreach (string segment in segments)
        {
            if (!IsValidSegment(segment, out error))
            {
                return false;
            }
        }

        normalized = string.Join('/', segments);
        error = null;
        return true;
    }

    private static bool IsValidSegment(string segment, [NotNullWhen(false)] out string? error)
    {
        if (segment.Length == 0)
        {
            // Catches "a//b" and any trailing slash, which would otherwise normalize
            // into a different path than the one declared.
            error = "the entry path contains an empty segment.";
            return false;
        }

        if (segment.Length > MaxSegmentLength)
        {
            error = $"a path segment exceeds {MaxSegmentLength} characters.";
            return false;
        }

        if (segment is "." or "..")
        {
            error = "the entry path contains a relative segment ('.' or '..').";
            return false;
        }

        // Windows silently strips trailing dots and spaces, so "evil." and "evil" become
        // the same file — a way to smuggle a second entry past a naive duplicate check.
        if (segment.EndsWith('.') || segment.EndsWith(' ') || segment.StartsWith(' '))
        {
            error = "a path segment has leading or trailing whitespace or a trailing dot.";
            return false;
        }

        string withoutExtension = segment.Split('.')[0];
        if (ReservedDeviceNames.Contains(withoutExtension))
        {
            error = $"'{withoutExtension}' is a reserved Windows device name.";
            return false;
        }

        foreach (char c in segment)
        {
            if (c is '<' or '>' or '"' or '|' or '?' or '*')
            {
                error = $"a path segment contains the invalid character '{c}'.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
