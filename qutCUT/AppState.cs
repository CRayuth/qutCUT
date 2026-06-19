using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using qutCUT.Account;
using qutCUT.Agent;
using qutCUT.Export;
using qutCUT.Generation;
using qutCUT.Project;
using qutCUT.Utilities;

namespace qutCUT;

public sealed partial class AppState : ObservableObject
{
    [ObservableProperty] private Editor.EditorViewModel? _activeEditor;

    public AccountService    Account    { get; }
    public ProjectRegistry   Registry   { get; }
    public GenerationService Generation { get; }
    public AgentService      Agent      { get; }
    public ExportService     Export     { get; } = new();

    private readonly DispatcherQueue _dispatcher;
    private VideoProject? _activeProject;

    private static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qutCUT");

    public AppState(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        Account     = new AccountService();
        Registry    = new ProjectRegistry();
        Agent       = new AgentService(Path.Combine(AppDataRoot, "sessions"));
        Generation  = new GenerationService(
            Path.Combine(AppDataRoot, "generated"),
            "https://api.palmier.io",
            Account.AuthToken);

        _ = Account.RestoreSessionAsync();
    }

    public void OpenProject(string filePath)
    {
        CloseProject();
        _activeProject = VideoProject.Open(filePath);
        Registry.RecordOpen(_activeProject);

        ActiveEditor = new Editor.EditorViewModel(
            _activeProject,
            Agent,
            Generation,
            Export,
            _dispatcher);

        Log.App.LogInformation("Opened project: {path}", filePath);
    }

    public void CreateProject(string filePath)
    {
        CloseProject();
        _activeProject = VideoProject.CreateNew(filePath);
        Registry.RecordOpen(_activeProject);

        ActiveEditor = new Editor.EditorViewModel(
            _activeProject,
            Agent,
            Generation,
            Export,
            _dispatcher);

        Log.App.LogInformation("Created project: {path}", filePath);
    }

    public void CloseProject()
    {
        if (ActiveEditor is not null)
        {
            ActiveEditor.Save();
            ActiveEditor.Dispose();
            ActiveEditor = null;
        }
        _activeProject?.Close();
        _activeProject = null;
    }

    public void SaveActiveProject() => ActiveEditor?.Save();
}
