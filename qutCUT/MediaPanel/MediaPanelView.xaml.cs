using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using qutCUT.Editor;
using qutCUT.Models;

namespace qutCUT.MediaPanel;

public sealed partial class MediaPanelView : UserControl
{
    private EditorViewModel? _viewModel;
    public EditorViewModel? ViewModel
    {
        get => _viewModel;
        set { _viewModel = value; Bindings.Update(); }
    }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set { _searchQuery = value; Bindings.Update(); }
    }

    public IEnumerable<MediaAsset> FilteredAssets =>
        _viewModel?.MediaAssets
            .Where(a => string.IsNullOrEmpty(_searchQuery)
                     || a.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
        ?? [];

    public MediaPanelView()
    {
        InitializeComponent();

        // Accept file drops
        AllowDrop = true;
        DragEnter += (_, e) =>
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                DropOverlay.Visibility = Visibility.Visible;
            }
        };
        DragLeave += (_, _) => DropOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnDragStart(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is not MediaAsset asset) return;
        e.Data.SetText($"asset:{asset.Id}");
        e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        if (!e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        var paths = items
            .OfType<Windows.Storage.StorageFile>()
            .Select(f => f.Path)
            .ToList();

        if (paths.Count > 0 && _viewModel is not null)
            await _viewModel.ImportMediaAsync(paths);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }
}
