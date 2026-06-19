namespace qutCUT.Models;

public sealed class MediaResolver(string projectDirectory)
{
    public string? Resolve(MediaManifestEntry entry)
    {
        if (entry.IsExternal)
        {
            var path = entry.RawPath;
            return File.Exists(path) ? path : null;
        }

        if (entry.IsProjectRelative)
        {
            var path = Path.Combine(projectDirectory, entry.RawPath);
            return File.Exists(path) ? path : null;
        }

        return null;
    }

    public static string ExternalSource(string absolutePath) =>
        $"external:{absolutePath}";

    public static string ProjectRelativeSource(string relativePath) =>
        $"project:{relativePath}";
}
