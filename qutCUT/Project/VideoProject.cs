using System.IO.Compression;
using System.Text.Json;
using qutCUT.Models;
using qutCUT.Utilities;

namespace qutCUT.Project;

// Equivalent of macOS NSDocument VideoProject.swift.
// A .qcut file is a ZIP archive containing project.json, media.json, media/, and sessions/.
public sealed class VideoProject
{
    public string FilePath { get; private set; } = string.Empty;
    public Timeline Timeline { get; set; } = new();
    public MediaManifest Manifest { get; set; } = new();
    public bool HasUnsavedChanges { get; set; }

    public string ProjectDirectory => Path.GetDirectoryName(FilePath) ?? string.Empty;
    public string MediaDirectory   => Path.Combine(WorkingDirectory, Constants.ProjectMediaFolder);
    public string SessionsDirectory => Path.Combine(WorkingDirectory, Constants.SessionsFolder);

    // Temp extraction directory for the open project
    private string WorkingDirectory => Path.Combine(
        Path.GetTempPath(), "qutCUT", Path.GetFileNameWithoutExtension(FilePath));

    public static VideoProject CreateNew(string filePath)
    {
        var project = new VideoProject
        {
            FilePath = filePath,
            Timeline = new Timeline { Fps = 30, Width = 1920, Height = 1080 },
            Manifest = MediaManifest.Empty()
        };
        project.Save();
        return project;
    }

    public static VideoProject Open(string filePath)
    {
        var project = new VideoProject { FilePath = filePath };
        project.Load();
        return project;
    }

    private void Load()
    {
        var workDir = WorkingDirectory;
        Directory.CreateDirectory(workDir);

        using var zip = ZipFile.OpenRead(FilePath);
        zip.ExtractToDirectory(workDir, overwriteFiles: true);

        var timelinePath = Path.Combine(workDir, Constants.TimelineFileName);
        var manifestPath = Path.Combine(workDir, Constants.ManifestFileName);

        if (File.Exists(timelinePath))
            Timeline = JsonSerializer.Deserialize<Timeline>(File.ReadAllText(timelinePath), JsonOptions.Default) ?? new();

        if (File.Exists(manifestPath))
            Manifest = MediaManifest.FromJson(File.ReadAllText(manifestPath));

        Log.Project.LogInformation("Opened project: {path}", FilePath);
    }

    public void Save()
    {
        var workDir = WorkingDirectory;
        Directory.CreateDirectory(workDir);
        Directory.CreateDirectory(MediaDirectory);
        Directory.CreateDirectory(SessionsDirectory);

        File.WriteAllText(Path.Combine(workDir, Constants.TimelineFileName),
            JsonSerializer.Serialize(Timeline, JsonOptions.Default));
        File.WriteAllText(Path.Combine(workDir, Constants.ManifestFileName),
            Manifest.ToJson());

        // Repack to ZIP
        var tempZip = FilePath + ".tmp";
        if (File.Exists(tempZip)) File.Delete(tempZip);
        ZipFile.CreateFromDirectory(workDir, tempZip, CompressionLevel.Optimal, false);
        File.Move(tempZip, FilePath, overwrite: true);

        HasUnsavedChanges = false;
        Log.Project.LogInformation("Saved project: {path}", FilePath);
    }

    public void SaveThumbnail(byte[] jpegBytes)
    {
        var dest = Path.Combine(WorkingDirectory, Constants.ThumbnailFileName);
        File.WriteAllBytes(dest, jpegBytes);
    }

    // Copies an external media file into the project's media folder.
    public string ImportMedia(string sourcePath)
    {
        Directory.CreateDirectory(MediaDirectory);
        var dest = Path.Combine(MediaDirectory, Path.GetFileName(sourcePath));
        if (!File.Exists(dest))
            File.Copy(sourcePath, dest);
        return dest;
    }

    public MediaResolver CreateResolver() => new(WorkingDirectory);

    public void Close()
    {
        // Clean up temp extraction directory
        try { if (Directory.Exists(WorkingDirectory)) Directory.Delete(WorkingDirectory, true); }
        catch { }
    }
}
