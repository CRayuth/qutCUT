namespace qutCUT.Utilities;

public static class MediaPathValidator
{
    // Characters that are dangerous inside a double-quoted FFmpeg argument string
    // or that FFmpeg parses as protocol prefixes / option separators.
    private static readonly char[] DangerousChars = ['"', '\'', '\n', '\r', '|', ';', '&', '<', '>'];

    /// <summary>
    /// Returns a canonicalized, safe path, or throws if the path is untrusted.
    /// Call this on every media path before interpolating it into FFmpeg args.
    /// </summary>
    public static string Validate(string path, string? allowedRoot = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Media path is empty.");

        // Reject paths that start with '-' — FFmpeg would treat them as options.
        if (path.TrimStart().StartsWith('-'))
            throw new ArgumentException($"Rejected media path starting with '-': {path}");

        // Reject shell metacharacters and quote characters.
        if (path.IndexOfAny(DangerousChars) >= 0)
            throw new ArgumentException($"Rejected media path with unsafe characters: {path}");

        // Reject FFmpeg protocol prefixes (e.g. concat:, http:, pipe:).
        // Windows drive letters are the only legitimate colon — they appear at index 1.
        var colonIdx = path.IndexOf(':');
        if (colonIdx >= 0 && colonIdx != 1)
            throw new ArgumentException($"Rejected media path with protocol prefix: {path}");

        // Canonicalize to remove . / .. traversal.
        string canonical;
        try { canonical = Path.GetFullPath(path); }
        catch (Exception ex) { throw new ArgumentException($"Cannot canonicalize path: {path}", ex); }

        // Optionally enforce that the path lives under a trusted root directory.
        if (allowedRoot is not null)
        {
            var root = Path.GetFullPath(allowedRoot);
            if (!canonical.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Media path escapes allowed root.\n  path={canonical}\n  root={root}");
        }

        return canonical;
    }

    /// <summary>
    /// Like Validate, but also confirms the file exists on disk.
    /// Use for input paths (source media); not for output paths that are about to be created.
    /// </summary>
    public static string ValidateExists(string path, string? allowedRoot = null)
    {
        var canonical = Validate(path, allowedRoot);
        if (!File.Exists(canonical))
            throw new FileNotFoundException($"Media file not found: {canonical}");
        return canonical;
    }
}
