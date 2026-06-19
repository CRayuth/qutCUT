using System.Text.Json;
using qutCUT.Utilities;

namespace qutCUT.Project;

public sealed class RecentProject
{
    public string FilePath  { get; set; } = string.Empty;
    public string Name      { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public string? ThumbnailPath { get; set; }
}

public sealed class ProjectRegistry
{
    private const int MaxRecent = 20;
    private readonly string _registryPath;
    private List<RecentProject> _projects = [];

    public ProjectRegistry()
    {
        var appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _registryPath = Path.Combine(appData, "qutCUT", "recent-projects.json");
        Load();
    }

    public IReadOnlyList<RecentProject> RecentProjects => _projects;

    public void RecordOpen(VideoProject project)
    {
        _projects.RemoveAll(p => p.FilePath == project.FilePath);
        _projects.Insert(0, new RecentProject
        {
            FilePath  = project.FilePath,
            Name      = Path.GetFileNameWithoutExtension(project.FilePath),
            OpenedAt  = DateTime.UtcNow
        });

        if (_projects.Count > MaxRecent)
            _projects = _projects[..MaxRecent];

        Save();
    }

    public void Remove(string filePath)
    {
        _projects.RemoveAll(p => p.FilePath == filePath);
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_registryPath)) return;
            _projects = JsonSerializer.Deserialize<List<RecentProject>>(
                File.ReadAllText(_registryPath), JsonOptions.Default) ?? [];
            // Remove entries for files that no longer exist
            _projects.RemoveAll(p => !File.Exists(p.FilePath));
        }
        catch { _projects = []; }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_registryPath)!);
        File.WriteAllText(_registryPath, JsonSerializer.Serialize(_projects, JsonOptions.Default));
    }
}
